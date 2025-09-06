using AutoMapper;
using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Cargo;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.Services.Order;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class OrderService : IOrderService
{
    private readonly DataBaseContext _context;
    private readonly IFrequentAddressService _frequentAddressService;
    private readonly ILogger<OrderService> _logger;
    private readonly IMapper _mapper;
    private readonly OrderStateMachine _stateMachine;
    private const int MaxPageSize = 100;



    public OrderService(DataBaseContext context, ILogger<OrderService> logger, IFrequentAddressService frequentAddressService, IMapper mapper, OrderStateMachine stateMachine)
    {
        _context = context;
        _logger = logger;
        _frequentAddressService = frequentAddressService;
        _mapper = mapper;
        _stateMachine = stateMachine;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, long currentUserId)
    {
        _logger.LogInformation("شروع ایجاد سفارش توسط کاربر {UserId} برای مالک {OwnerId}", currentUserId, dto.OwnerId);
        ValidateCreateOrderDto(dto);

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var owner = await _context.Persons.FindAsync(dto.OwnerId)
            ?? throw new NotFoundException("مالک سفارش یافت نشد.");

        if (dto.OwnerId != currentUserId && !currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("شما مجاز به ثبت سفارش برای دیگران نیستید.");

        if (dto.IsForOrganization && dto.OrganizationId == null)
            throw new AppException("شناسه سازمان برای سفارش سازمانی الزامی است.");

        if (!dto.IsForOrganization && (dto.OrganizationId != null || dto.BranchId != null))
            throw new AppException("در سفارش شخصی نباید OrganizationId یا BranchId مقدار داشته باشند.");

        if (dto.IsForOrganization)
        {
            var hasBranches = await _context.SubOrganizations
                .AnyAsync(b => b.OrganizationId == dto.OrganizationId);

            if (hasBranches)
            {
                if (dto.BranchId == null)
                    throw new AppException("برای سازمان دارای شعبه، انتخاب شعبه الزامی است.");

                var isBranchMember = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.PersonId == currentUserId &&
                    m.OrganizationId == dto.OrganizationId &&
                    m.BranchId == dto.BranchId);

                if (!isBranchMember && !currentUser.IsAdminOrSuperAdmin())
                    throw new ForbiddenAccessException("شما عضو این شعبه نیستید.");
            }
            else
            {
                if (dto.BranchId != null)
                    throw new AppException("این سازمان شعبه‌ای ندارد. نباید مقدار BranchId ارسال شود.");

                var isOrgMember = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.PersonId == currentUserId &&
                    m.OrganizationId == dto.OrganizationId);

                if (!isOrgMember && !currentUser.IsAdminOrSuperAdmin())
                    throw new ForbiddenAccessException("شما عضو این سازمان نیستید.");
            }
        }

        Address originAddress;

        if (dto.OriginAddressId.HasValue)
        {
            var freq = await _context.FrequentAddresses.FindAsync(dto.OriginAddressId.Value)
                ?? throw new NotFoundException("آدرس پر استفاده مبدا یافت نشد.");

            originAddress = new Address
            {
                FullAddress = freq.FullAddress,
                City = freq.City,
                Province = freq.Province,
                PostalCode = freq.PostalCode,
                Plate = freq.Plate,
                Unit = freq.Unit,
                Title = freq.Title,
                PersonId = freq.PersonId,
                OrganizationId = freq.OrganizationId,
                BranchId = freq.BranchId
            };
            _context.Addresses.Add(originAddress);
            await _context.SaveChangesAsync();

            freq.UsageCount++;
            freq.LastUsed = DateTime.UtcNow;
        }
        else if (dto.IsManualOrigin)
        {
            originAddress = new Address
            {
                FullAddress = dto.OriginFullAddress,
                City = dto.OriginCity,
                Province = dto.OriginProvince,
                PostalCode = dto.OriginPostalCode,
                Plate = dto.OriginPlate,
                Unit = dto.OriginUnit,
                Title = dto.OriginTitle,
                PersonId = dto.IsForOrganization ? null : dto.OwnerId,
                OrganizationId = dto.IsForOrganization ? dto.OrganizationId : null,
                BranchId = dto.IsForOrganization ? dto.BranchId : null
            };
            _context.Addresses.Add(originAddress);
            await _context.SaveChangesAsync();

            if (dto.SaveOriginAsFrequent)
            {
                await _frequentAddressService.InsertOrUpdateAsync(originAddress, FrequentAddressType.Origin,
                    dto.IsForOrganization ? null : dto.OwnerId,
                    dto.OrganizationId,
                    dto.BranchId);
            }
        }
        else if (dto.OriginAddressId.HasValue)
        {
            originAddress = await _context.Addresses.FindAsync(dto.OriginAddressId.Value)
                ?? throw new NotFoundException("آدرس مبدا یافت نشد.");
        }
        else
        {
            throw new AppException("اطلاعات آدرس مبدا ناقص است.");
        }

        Address destinationAddress;

        if (dto.DestinationAddressId.HasValue)
        {
            var freq = await _context.FrequentAddresses.FindAsync(dto.DestinationAddressId.Value)
                ?? throw new NotFoundException("آدرس پر استفاده مقصد یافت نشد.");

            destinationAddress = new Address
            {
                FullAddress = freq.FullAddress,
                City = freq.City,
                Province = freq.Province,
                PostalCode = freq.PostalCode,
                Plate = freq.Plate,
                Unit = freq.Unit,
                Title = freq.Title,
                PersonId = freq.PersonId,
                OrganizationId = freq.OrganizationId,
                BranchId = freq.BranchId
            };
            _context.Addresses.Add(destinationAddress);
            await _context.SaveChangesAsync();

            freq.UsageCount++;
            freq.LastUsed = DateTime.UtcNow;
        }
        else if (dto.IsManualDestination)
        {
            destinationAddress = new Address
            {
                FullAddress = dto.DestinationFullAddress,
                City = dto.DestinationCity,
                Province = dto.DestinationProvince,
                PostalCode = dto.DestinationPostalCode,
                Plate = dto.DestinationPlate,
                Unit = dto.DestinationUnit,
                Title = dto.DestinationTitle,
                PersonId = dto.IsForOrganization ? null : dto.OwnerId,
                OrganizationId = dto.IsForOrganization ? dto.OrganizationId : null,
                BranchId = dto.IsForOrganization ? dto.BranchId : null
            };
            _context.Addresses.Add(destinationAddress);
            await _context.SaveChangesAsync();

            if (dto.SaveDestinationAsFrequent)
            {
                await _frequentAddressService.InsertOrUpdateAsync(destinationAddress, FrequentAddressType.Destination,
                    dto.IsForOrganization ? null : dto.OwnerId,
                    dto.OrganizationId,
                    dto.BranchId);
            }
        }
        else if (dto.DestinationAddressId.HasValue)
        {
            destinationAddress = await _context.Addresses.FindAsync(dto.DestinationAddressId.Value)
                ?? throw new NotFoundException("آدرس مقصد یافت نشد.");
        }
        else
        {
            throw new AppException("اطلاعات آدرس مقصد ناقص است.");
        }

        var unassignedCargos = await _context.Cargos
            .Where(c => c.OwnerId == dto.OwnerId && c.OrderId == null)
            .ToListAsync();

        if (!unassignedCargos.Any())
            throw new AppException("برای ثبت سفارش، ابتدا باید حداقل یک 'بار' ثبت شود.");

        var trackingNumber = GenerateTrackingNumber();
        if (await _context.Orders.AnyAsync(o => o.TrackingNumber == trackingNumber))
            throw new AppException("کد پیگیری تکراری است.");

        var order = new Order
        {
            OwnerId = dto.OwnerId,
            OrganizationId = dto.OrganizationId,
            BranchId = dto.BranchId,
            OriginAddressId = originAddress.Id,
            DestinationAddress = destinationAddress.FullAddress,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TrackingNumber = trackingNumber,
            Fare = dto.Fare,
            Insurance = dto.Insurance,
            Vat = dto.Vat,
            DeclaredValue = dto.DeclaredValue,
            IsInsuranceRequested = dto.IsInsuranceRequested,
            SenderName = dto.SenderName,
            SenderPhone = dto.SenderPhone,
            ReceiverName = dto.ReceiverName,
            ReceiverPhone = dto.ReceiverPhone,
            OrderDescription = dto.Details,
            LoadingTime = dto.LoadingTime
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var cargo in unassignedCargos)
            cargo.OrderId = order.Id;

        _context.OrderEvents.Add(new OrderEvent
        {
            OrderId = order.Id,
            Status = OrderStatus.Pending,
            ChangedByPersonId = currentUserId,
            Remarks = "سفارش ایجاد شد."
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("سفارش با ID={OrderId} و {CargoCount} بار ایجاد شد.", order.Id, unassignedCargos.Count);

        var result = await _context.Orders
            .Include(o => o.OriginAddress)
            .Include(o => o.Cargos).ThenInclude(c => c.Images)
            .Include(o => o.Organization)
            .Include(o => o.Warehouse)
            .Include(o => o.Payments)
            .Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        return _mapper.Map<OrderDto>(result);
    }

    private async Task<Address> CreateAddressAsync(CreateOrderDto dto, bool isOrigin)
    {
        var address = new Address
        {
            FullAddress = isOrigin ? dto.OriginFullAddress : dto.DestinationFullAddress,
            City = isOrigin ? dto.OriginCity : dto.DestinationCity,
            Province = isOrigin ? dto.OriginProvince : dto.DestinationProvince,
            PostalCode = isOrigin ? dto.OriginPostalCode : dto.DestinationPostalCode,
            Plate = isOrigin ? dto.OriginPlate : dto.DestinationPlate,
            Unit = isOrigin ? dto.OriginUnit : dto.DestinationUnit,
            Title = isOrigin ? dto.OriginTitle : dto.DestinationTitle,
            PersonId = dto.IsForOrganization ? null : dto.OwnerId,
            OrganizationId = dto.IsForOrganization ? dto.OrganizationId : null,
            BranchId = dto.IsForOrganization ? dto.BranchId : null
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        return address;
    }

    public async Task<OrderDto> GetByIdAsync(long id, long currentUserId)
    {
        _logger.LogInformation("دریافت سفارش {OrderId} توسط کاربر {UserId}", id, currentUserId);

        var order = await _context.Orders
                        .Include(o => o.OriginAddress)
                        .Include(o => o.Warehouse)
                        .Include(o => o.Organization)
                        .Include(o => o.Branch)
                        .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
                        .Include(o => o.Cargos).ThenInclude(c => c.Images)
                        .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
                        .Include(o => o.Payments)
                        .Include(o => o.Events).ThenInclude(e => e.ChangedByPerson)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == id)
                    ?? throw new NotFoundException("سفارشی با این شناسه یافت نشد.");

        var user = await _context.Persons
                       .Include(p => p.Memberships)
                       .FirstOrDefaultAsync(p => p.Id == currentUserId)
                   ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, user, _context);

        return MapToOrderDto(order);
    }

    public async Task<PagedResult<OrderDto>> GetByOwnerAsync(long ownerId, long currentUserId, int page, int pageSize)
    {
        _logger.LogInformation("دریافت سفارش‌های مالک {OwnerId} توسط کاربر {UserId} - صفحه {PageNumber} اندازه {PageSize}",
            ownerId, currentUserId, page, pageSize);

        var user = await _context.Persons
                       .Include(p => p.Memberships)
                       .FirstOrDefaultAsync(p => p.Id == currentUserId)
                   ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (currentUserId != ownerId && !user.IsAdminOrSuperAdminOrMonitor())
        {
            _logger.LogWarning("دسترسی غیرمجاز کاربر {CurrentUserId} (نقش: {UserRole}) به سفارش‌های مالک {OwnerId}",
                currentUserId, user.Role, ownerId);
            throw new UnauthorizedAccessAppException("شما اجازه مشاهده سفارش‌های این کاربر را ندارید.");
        }

        var query = _context.Orders
            .Where(o => o.OwnerId == ownerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Owner)
            .Include(o => o.OriginAddress)
            .Include(o => o.Organization)
            .Include(o => o.Branch)
            .Include(o => o.Cargos).ThenInclude(c => c.Images)
            .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
            .Include(o => o.Events)
            .Include(o => o.Payments)
            .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
            .Include(o => o.Warehouse)
            .AsNoTracking();

        var orders = await query.ToListAsync();
        var totalCount = await _context.Orders.CountAsync(o => o.OwnerId == ownerId);

        return new PagedResult<OrderDto>
        {
            Items = orders.Select(MapToOrderDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    public async Task<OrderStatusDto> GetByTrackingNumberAsync(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("شماره پیگیری نمی‌تواند خالی باشد.", nameof(trackingNumber));

        _logger.LogInformation("بررسی وضعیت سفارش با شماره پیگیری {TrackingNumber}", trackingNumber);

        var order = await _context.Orders
            .Where(o => o.TrackingNumber == trackingNumber)
            .Include(o => o.Events.OrderByDescending(e => e.EventDateTime))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (order == null)
        {
            _logger.LogWarning("سفارش با شماره پیگیری {TrackingNumber} یافت نشد.", trackingNumber);
            throw new NotFoundException("سفارشی با این شماره پیگیری یافت نشد.");
        }

        var latestEvent = order.Events.FirstOrDefault();

        return new OrderStatusDto
        {
            OrderId = order.Id,
            TrackingNumber = order.TrackingNumber,
            Status = order.Status.ToString(),
            Remarks = latestEvent?.Remarks ?? "وضعیتی ثبت نشده است.",
            LastUpdated = latestEvent?.EventDateTime,
            EstimatedDeliveryTime = order.DeliveryTime 
        };
    }
    public async Task<PagedResult<OrderDto>> GetAllAsync(long currentUserId, int page, int pageSize)
    {
        _logger.LogInformation("دریافت لیست سفارش‌ها توسط کاربر {UserId} - صفحه {PageNumber} اندازه {PageSize}",
            currentUserId, page, pageSize);

        var user = await _context.Persons
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        IQueryable<Order> query;

        if (user.IsAdminOrSuperAdminOrMonitor())
        {
            query = _context.Orders.AsQueryable();
        }
        else if (user.Memberships.Any())
        {
            var orgIds = user.Memberships
                .Select(m => m.OrganizationId)
                .Distinct()
                .ToList();

            var branchIds = user.Memberships
                .Where(m => m.BranchId != null)
                .Select(m => m.BranchId!.Value)
                .Distinct()
                .ToList();

            query = _context.Orders
                .Where(o =>
                    (o.OrganizationId != null && orgIds.Contains(o.OrganizationId.Value)) ||
                    (o.BranchId != null && branchIds.Contains(o.BranchId.Value)));
        }
        else
        {
            query = _context.Orders
                .Where(o => o.OwnerId == currentUserId);
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .Include(o => o.Owner)
            .Include(o => o.OriginAddress)
            .Include(o => o.Organization)
            .Include(o => o.Branch)
            .Include(o => o.Cargos).ThenInclude(c => c.Images)
            .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
            .Include(o => o.Events)
            .Include(o => o.Payments)
            .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
            .Include(o => o.Warehouse)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        var orderDtos = orders.Select(MapToOrderDto).ToList();

        return new PagedResult<OrderDto>
        {
            Items = orderDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    public async Task<OrderDto> UpdateAsync(long orderId, UpdateOrderDto dto, long currentUserId)
    {
        _logger.LogInformation("درخواست به‌روزرسانی سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Owner)
                .Include(o => o.Organization).ThenInclude(org => org.Memberships)
                .Include(o => o.Branch).ThenInclude(b => b.Memberships)
                .Include(o => o.OriginAddress)
                .Include(o => o.Cargos).ThenInclude(c => c.Images)
                .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
                .Include(o => o.Payments)
                .Include(o => o.Events).ThenInclude(e => e.ChangedByPerson)
                .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
                .Include(o => o.Warehouse)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"سفارش با شناسه {orderId} یافت نشد.");

            var user = await _context.Persons
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد.");

            if (!await CanUserModifyOrderAsync(order, user))
                throw new ForbiddenAccessException("دسترسی به‌روزرسانی سفارش وجود ندارد.");

            if (order.Status >= OrderStatus.Assigned)
                throw new InvalidOperationException($"سفارش در وضعیت '{order.Status}' قابل ویرایش نیست.");

            var updated = ApplyOrderChanges(order, dto);
            if (!updated)
            {
                await transaction.RollbackAsync();
                _logger.LogInformation("هیچ تغییری برای سفارش {OrderId} انجام نشد.", orderId);
                return _mapper.Map<OrderDto>(order);
            }

            _context.OrderEvents.Add(new OrderEvent
            {
                OrderId = order.Id,
                Status = order.Status,
                EventDateTime = DateTime.UtcNow,
                Remarks = "اطلاعات سفارش به‌روزرسانی شد.",
                ChangedByPersonId = currentUserId
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("سفارش {OrderId} با موفقیت به‌روزرسانی شد.", orderId);

            var finalOrder = await _context.Orders
                .Include(o => o.OriginAddress)
                .Include(o => o.Cargos).ThenInclude(c => c.Images)
                .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
                .Include(o => o.Payments)
                .Include(o => o.Events).ThenInclude(e => e.ChangedByPerson)
                .Include(o => o.Warehouse)
                .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return _mapper.Map<OrderDto>(finalOrder ?? order);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در به‌روزرسانی سفارش {OrderId}", orderId);
            throw;
        }
    }
    public async Task CancelAsync(long orderId, long currentUserId, string? cancellationReason = null)
    {
        _logger.LogInformation("درخواست لغو سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Owner)
            .Include(o => o.Organization).ThenInclude(org => org.Memberships)
            .Include(o => o.Branch).ThenInclude(b => b.Memberships)
            .Include(o => o.Cargos)
            .Include(o => o.Payments)
            .Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("سفارش مورد نظر یافت نشد.");

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("سفارش قبلاً لغو شده است.");
        }

        var currentUser = await _context.Persons
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var isPrivilegedUser = currentUser.IsAdminOrSuperAdminOrMonitor();
        var isOwner = order.OwnerId == currentUserId;

        bool isOrgAdmin = currentUser.Memberships.Any(m =>
            m.OrganizationId == order.OrganizationId &&
            m.BranchId == null &&
            m.Role == SystemRole.orgadmin);

        bool isBranchAdmin = order.BranchId.HasValue &&
            currentUser.Memberships.Any(m =>
                m.OrganizationId == order.OrganizationId &&
                m.BranchId == order.BranchId &&
                m.Role == SystemRole.branchadmin);

        var hasCancelPermission = isPrivilegedUser || isOwner || isOrgAdmin || isBranchAdmin;

        if (order.Status >= OrderStatus.Assigned && !isPrivilegedUser)
        {
            _logger.LogWarning("لغو سفارش {OrderId} در وضعیت {Status} فقط برای مدیران مجاز است.", orderId, order.Status);
            throw new UnauthorizedAccessAppException("فقط مدیران مجاز به لغو سفارش پس از تخصیص هستند.");
        }

        if (!hasCancelPermission)
        {
            _logger.LogWarning("کاربر {UserId} مجاز به لغو سفارش {OrderId} نیست.", currentUserId, orderId);
            throw new UnauthorizedAccessAppException("شما مجاز به لغو این سفارش نیستید.");
        }

        order.Status = OrderStatus.Cancelled;

        _context.OrderEvents.Add(new OrderEvent
        {
            OrderId = order.Id,
            Status = OrderStatus.Cancelled,
            EventDateTime = DateTime.UtcNow,
            ChangedByPersonId = currentUserId,
            Remarks = cancellationReason ?? "لغو سفارش توسط کاربر"
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("سفارش {OrderId} با موفقیت توسط کاربر {UserId} لغو شد.", orderId, currentUserId);
    }

    public async Task ChangeStatusAsync(long orderId, ChangeOrderStatusDto dto, long currentUserId)
    {
        _logger.LogInformation("درخواست تغییر وضعیت سفارش {OrderId} به {NewStatus} توسط کاربر {UserId}", orderId, dto.NewStatus, currentUserId);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Events)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"سفارش با شناسه {orderId} یافت نشد.");

            var user = await _context.Persons
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد.");

            var isPrivileged = user.IsAdminOrSuperAdminOrMonitor();
            var isDriver = order.CollectorId == currentUserId
                                          || order.DelivererId == currentUserId
                                          || order.FinalReceiverId == currentUserId;

            var isOwner = order.OwnerId == currentUserId;

            var currentStatus = order.Status;
            var newStatus = dto.NewStatus;
            var changed = _stateMachine.TryChangeStatus(order, newStatus, isPrivileged, isDriver, isOwner);

            //if ((int)newStatus < (int)currentStatus && !isPrivileged)
            if (!changed)
            {
                //    _logger.LogWarning("کاربر {UserId} تلاش کرد وضعیت سفارش {OrderId} را از {CurrentStatus} به {NewStatus} برگرداند.", currentUserId, orderId, currentStatus, newStatus);
                //    throw new InvalidOperationException("امکان بازگشت وضعیت وجود ندارد.");
                //}

                //if (currentStatus == newStatus)
                //{
                //    _logger.LogInformation("وضعیت فعلی سفارش {OrderId} با وضعیت جدید یکسان است ({Status}).", orderId, currentStatus);
                _logger.LogInformation("وضعیت فعلی سفارش {OrderId} با وضعیت جدید یکسان است ({Status}).", orderId, newStatus);
                return;
            }

            //if (!isPrivileged)
            if (newStatus == OrderStatus.Delivered && order.DeliveryTime != null)
            {
            //    if (isDriver)
            //    {
            //        if (!IsDriverStatusTransitionAllowed(currentStatus, newStatus))
            //            throw new UnauthorizedAccessAppException("شما مجاز به این تغییر وضعیت نیستید.");
            //    }
            //    else if (isOwner)
            //    {
            //        if (newStatus != OrderStatus.Delivered)
            //            throw new UnauthorizedAccessAppException("شما فقط مجاز به تغییر وضعیت به 'Delivered' هستید.");
            //    }
            //    else
            //    {
            //        throw new UnauthorizedAccessAppException("شما مجاز به تغییر وضعیت این سفارش نیستید.");
            //    }
            //}

            //if (newStatus == OrderStatus.Cancelled && (int)currentStatus >= (int)OrderStatus.Assigned && !isPrivileged)
            //{
            //    throw new UnauthorizedAccessAppException("شما مجاز به لغو سفارش در این وضعیت نیستید.");
            //}

            //order.Status = newStatus;

            //if (newStatus == OrderStatus.Delivered && order.DeliveryTime == null)
            //{
            //    order.DeliveryTime = DateTime.UtcNow;
                _logger.LogInformation("زمان تحویل برای سفارش {OrderId} ثبت شد.", orderId);
            }

            _context.OrderEvents.Add(new OrderEvent
            {
                OrderId = order.Id,
                Status = newStatus,
                EventDateTime = DateTime.UtcNow,
                ChangedByPersonId = currentUserId,
                Remarks = dto.Remarks ?? $"وضعیت به '{newStatus}' تغییر یافت."
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("وضعیت سفارش {OrderId} با موفقیت از {OldStatus} به {NewStatus} تغییر یافت.", order.Id, currentStatus, newStatus);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در تغییر وضعیت سفارش {OrderId} به {NewStatus}", orderId, dto.NewStatus);
            throw;
        }
    }

    public async Task AssignOrderPersonnelAsync(long orderId, AssignOrderPersonnelDto dto, long currentUserId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            throw new NotFoundException("Order not found");
        var user = await _context.Persons
                              .Include(p => p.Memberships)
                              .FirstOrDefaultAsync(p => p.Id == currentUserId)
                          ?? throw new NotFoundException("کاربر جاری یافت نشد.");
        if (!user.IsAdminOrSuperAdminOrMonitor())
            throw new UnauthorizedAccessAppException("Only admin/superadmin/monitoring can assign order roles.");

        bool anyChanges = false;

        if (dto.CollectorId.HasValue && dto.CollectorId != order.CollectorId)
        {
            order.CollectorId = dto.CollectorId;
            anyChanges = true;
        }

        if (dto.DelivererId.HasValue && dto.DelivererId != order.DelivererId)
        {
            order.DelivererId = dto.DelivererId;
            anyChanges = true;
        }

        if (dto.FinalReceiverId.HasValue && dto.FinalReceiverId != order.FinalReceiverId)
        {
            order.FinalReceiverId = dto.FinalReceiverId;
            anyChanges = true;
        }

        if (!anyChanges)
            return;

        var orderEvent = new OrderEvent
        {
            OrderId = order.Id,
            Status = order.Status,
            Remarks = dto.Remarks ?? "نقش‌های سفارش تغییر یافت.",
            EventDateTime = DateTime.UtcNow,
            ChangedByPersonId = currentUserId
        };

        _context.OrderEvents.Add(orderEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("نقش‌های سفارش {OrderId} توسط کاربر {UserId} تغییر یافت. توضیحات: {Remarks}",
            order.Id, currentUserId, dto.Remarks ?? "بدون توضیحات");
    }


    private void ValidateCreateOrderDto(CreateOrderDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.SenderName)) errors.Add("نام فرستنده الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.SenderPhone)) errors.Add("شماره فرستنده الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.ReceiverName)) errors.Add("نام گیرنده الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.ReceiverPhone)) errors.Add("شماره گیرنده الزامی است.");


        if (dto.IsForOrganization)
        {
            if (!dto.OrganizationId.HasValue)
                errors.Add("برای سفارش سازمانی، شناسه سازمان الزامی است.");
            else
            {
            }

            if (dto.IsManualOrigin)
            {
                if (string.IsNullOrWhiteSpace(dto.OriginFullAddress) || string.IsNullOrWhiteSpace(dto.OriginCity) || string.IsNullOrWhiteSpace(dto.OriginProvince))
                    errors.Add("برای مبدا دستی سازمانی، آدرس کامل، شهر و استان الزامی است.");
            }
            else
            {
                if (!dto.OrganizationId.HasValue && !dto.BranchId.HasValue)
                    errors.Add("مبدا سازمانی نامشخص است (نه دستی، نه سازمان/شعبه معتبر).");
            }
            if (!dto.IsManualOrigin && dto.SaveOriginAsFrequent)
                errors.Add("ذخیره آدرس مبدا پیش‌فرض سازمانی/شعبه به عنوان پرتکرار مجاز نیست.");

        }
        else
        {

            if (dto.IsManualOrigin)
            {
                if (string.IsNullOrWhiteSpace(dto.OriginFullAddress) || string.IsNullOrWhiteSpace(dto.OriginCity) || string.IsNullOrWhiteSpace(dto.OriginProvince))
                    errors.Add("برای مبدا دستی شخصی، آدرس کامل، شهر و استان الزامی است.");
            }
            else
            {
                if (!dto.OriginAddressId.HasValue)
                    errors.Add("برای مبدا شخصی غیردستی، شناسه آدرس مبدا الزامی است.");
            }
        }

        if (errors.Any())
        {
            throw new BadRequestException("خطا در اطلاعات ورودی: " + string.Join(" | ", errors));
        }
    }
    private async Task<(string Address, string Title)> GetOrganizationOriginAddressStringAsync(long? organizationId, long? branchId)
    {
        if (branchId.HasValue)
        {
            var branch = await _context.SubOrganizations
                .FirstOrDefaultAsync(b => b.Id == branchId.Value && (!organizationId.HasValue || b.OrganizationId == organizationId.Value));

            if (branch == null) throw new NotFoundException($"شعبه با شناسه {branchId.Value} یافت نشد یا به سازمان مشخص شده تعلق ندارد.");
            if (string.IsNullOrWhiteSpace(branch.OriginAddress)) throw new BadRequestException($"آدرس مبدا پیش‌فرض برای شعبه '{branch.Name}' ثبت نشده است.");

            return (branch.OriginAddress, $"آدرس شعبه {branch.Name}");
        }
        else if (organizationId.HasValue)
        {
            var org = await _context.Organizations.FindAsync(organizationId.Value)
                      ?? throw new NotFoundException($"سازمان با شناسه {organizationId.Value} یافت نشد.");

            if (string.IsNullOrWhiteSpace(org.OriginAddress)) throw new BadRequestException($"آدرس مبدا پیش‌فرض برای سازمان '{org.Name}' ثبت نشده است.");

            return (org.OriginAddress, $"آدرس سازمان {org.Name}");
        }
        else
        {
            throw new BadRequestException("شناسه سازمان یا شعبه برای دریافت آدرس مبدا سازمانی الزامی است.");
        }
    }
    private async Task<bool> CanUserModifyOrderAsync(Order order, Person currentUser)
    {
        if (currentUser.IsAdminOrSuperAdminOrMonitor())
            return true;

        if (order.OwnerId == currentUser.Id)
            return true;

        if (order.OrganizationId.HasValue)
        {
            var orgId = order.OrganizationId.Value;
            var branchId = order.BranchId;

            foreach (var membership in currentUser.Memberships)
            {
                if (membership.OrganizationId != orgId)
                    continue;

                if (membership.Role == SystemRole.orgadmin && membership.BranchId == null)
                    return true;

                if (membership.Role == SystemRole.branchadmin && membership.BranchId == branchId)
                    return true;
            }
        }

        _logger.LogDebug("User {UserId} is not authorized to modify order {OrderId}", currentUser.Id, order.Id);
        return false;
    }
    private async Task<bool> CanUserCancelOrderAsync(Order order, Person currentUser)
    {
        if (currentUser.IsAdminOrSuperAdminOrMonitor())
            return true;

        if (order.OwnerId == currentUser.Id)
            return true;

        if (order.OrganizationId.HasValue && currentUser.Memberships.Any())
        {
            var relevantMemberships = currentUser.Memberships
                .Where(m => m.OrganizationId == order.OrganizationId.Value)
                .ToList();

            if (!relevantMemberships.Any()) return false; 

            if (relevantMemberships.Any(m => m.Role == SystemRole.orgadmin && m.BranchId is null))
                return true;

            if (order.BranchId.HasValue) 
            {
                if (relevantMemberships.Any(m => m.BranchId == order.BranchId.Value && m.Role == SystemRole.branchadmin))
                {
                    return true;
                }
            }
        }

        _logger.LogDebug("Cancellation denied for user {UserId} on order {OrderId}.", currentUser.Id, order.Id);
        return false;
    }

    private string GenerateTrackingNumber()
        => $"TRK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";


    private async Task<Address> DetermineOriginAddressAsync(CreateOrderDto dto)
    {
        Address address;

        if (dto.IsForOrganization)
        {
            if (dto.IsManualOrigin)
            {
                if (string.IsNullOrWhiteSpace(dto.OriginFullAddress) || string.IsNullOrWhiteSpace(dto.OriginCity) || string.IsNullOrWhiteSpace(dto.OriginProvince))
                    throw new BadRequestException("برای مبدا دستی سازمانی، آدرس کامل، شهر و استان الزامی است.");

                address = new Address
                {
                    PersonId = dto.OwnerId,
                    FullAddress = dto.OriginFullAddress,
                    Title = dto.OriginTitle ?? "آدرس مبدا دستی سازمانی",
                    City = dto.OriginCity,
                    Province = dto.OriginProvince,
                    PostalCode = dto.OriginPostalCode,
                    Plate = dto.OriginPlate,
                    Unit = dto.OriginUnit
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
                return address;
            }
            else
            {
                var result = await GetOrganizationOriginAddressStringAsync(dto.OrganizationId, dto.BranchId);
                address = new Address
                {
                    PersonId = dto.OwnerId,
                    FullAddress = result.Address,
                    Title = result.Title,
                    City = "",
                    Province = ""
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
                return address;
            }
        }
        else
        {
            if (dto.IsManualOrigin)
            {
                if (string.IsNullOrWhiteSpace(dto.OriginFullAddress) || string.IsNullOrWhiteSpace(dto.OriginCity) || string.IsNullOrWhiteSpace(dto.OriginProvince))
                    throw new BadRequestException("برای مبدا دستی شخصی، آدرس کامل، شهر و استان الزامی است.");

                address = new Address
                {
                    PersonId = dto.OwnerId,
                    FullAddress = dto.OriginFullAddress,
                    Title = dto.OriginTitle ?? "آدرس مبدا دستی",
                    City = dto.OriginCity,
                    Province = dto.OriginProvince,
                    PostalCode = dto.OriginPostalCode,
                    Plate = dto.OriginPlate,
                    Unit = dto.OriginUnit
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                if (dto.SaveOriginAsFrequent)
                {
                    await _frequentAddressService.InsertOrUpdateAsync(
                        address,
                        FrequentAddressType.Origin,
                        personId: dto.OwnerId,
                        organizationId: null,
                        branchId: null
                    );
                }

                return address;
            }
            else if (dto.OriginAddressId.HasValue)
            {
                address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == dto.OriginAddressId.Value && a.PersonId == dto.OwnerId)
                    ?? throw new NotFoundException($"آدرس مبدا با شناسه {dto.OriginAddressId.Value} برای این کاربر یافت نشد.");

                return address;
            }
            else
            {
                throw new BadRequestException("برای سفارش شخصی، باید شناسه آدرس مبدا یا اطلاعات مبدا دستی ارائه شود.");
            }
        }
    }

    private string BuildDestinationAddressString(
        CreateOrderDto dto,
        out string city,
        out string province,
        out string? postalCode,
        out string? plate,
        out string? unit,
        out string? title)
    {
        if (string.IsNullOrWhiteSpace(dto.DestinationFullAddress) ||
            string.IsNullOrWhiteSpace(dto.DestinationCity) ||
            string.IsNullOrWhiteSpace(dto.DestinationProvince))
        {
            throw new BadRequestException("آدرس کامل، شهر و استان مقصد الزامی است.");
        }

        city = dto.DestinationCity;
        province = dto.DestinationProvince;
        postalCode = dto.DestinationPostalCode;
        plate = dto.DestinationPlate;
        unit = dto.DestinationUnit;
        title = dto.DestinationTitle ?? "آدرس مقصد";

        return dto.DestinationFullAddress;
    }


    private OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            TrackingNumber = order.TrackingNumber,
            Status = order.Status.ToString(),
            OriginAddress = order.OriginAddress?.FullAddress ?? "—",
            DestinationAddress = order.DestinationAddress,
            SenderName = order.SenderName,
            SenderPhone = order.SenderPhone,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            Fare = order.Fare,
            Insurance = order.Insurance,
            Vat = order.Vat,
            Description = order.OrderDescription,
            LoadingTime = order.LoadingTime,
            DeliveryTime = order.DeliveryTime,
            WarehouseName = order.Warehouse?.WarehouseName,
            OrganizationName = order.Organization?.Name,
            AssignedVehiclePlates = order.OrderVehicles.Select(ov => ov.Vehicle?.PlateNumber ?? "نامشخص").ToList(),
            Cargos = order.Cargos.Select(c => new CargoDto
            {
                Id = c.Id,
                Title = c.Title,
                Contents = c.Contents,
                Weight = c.Weight,
                Length = c.Length,
                Width = c.Width,
                Height = c.Height,
                PackagingType = c.PackagingType,
                PackageCount = c.PackageCount,
                Description = c.Description,
                CargoTypeName = c.CargoType?.Name ?? "",
                ImageUrls = c.Images?.Select(i => i.ImageUrl).ToList() ?? new()
            }).ToList(),
            Payments = order.Payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                TransactionId = p.TransactionId
            }).ToList(),
            Events = order.Events.Select(e => new OrderEventDto
            {
                Id = e.Id,
                Status = e.Status.ToString(),
                EventDateTime = e.EventDateTime,
                Remarks = e.Remarks,
                ChangedByFullName = e.ChangedByPerson?.GetFullName() ?? "سیستم"
            }).ToList()
        };
    }

    private bool ApplyOrderChanges( Order order, UpdateOrderDto dto)
    {
        bool changed = false;

        void TryUpdate<T>(T? newValue, Action<T> setter) where T : class
        {
            if (newValue != null)
            {
                setter(newValue);
                changed = true;
            }
        }

        void TryUpdateStruct<T>(T? newValue, Action<T> setter) where T : struct
        {
            if (newValue.HasValue)
            {
                setter(newValue.Value);
                changed = true;
            }
        }

        TryUpdate(dto.SenderName, val => order.SenderName = val);
        TryUpdate(dto.SenderPhone, val => order.SenderPhone = val);
        TryUpdate(dto.ReceiverName, val => order.ReceiverName = val);
        TryUpdate(dto.ReceiverPhone, val => order.ReceiverPhone = val);
        TryUpdate(dto.Details, val => order.OrderDescription = val);
        TryUpdateStruct(dto.LoadingTime, val => order.LoadingTime = val);
        TryUpdateStruct(dto.DeclaredValue, val => order.DeclaredValue = val);
        TryUpdateStruct(dto.IsInsuranceRequested, val => order.IsInsuranceRequested = val);

        return changed;
    }

    //private bool IsDriverStatusTransitionAllowed(OrderStatus current, OrderStatus next)
    //{
    //    return (current == OrderStatus.Assigned && next == OrderStatus.Loading)
    //           || (current == OrderStatus.Loading && next == OrderStatus.InProgress)
    //           || (current == OrderStatus.InProgress && next == OrderStatus.Unloading);
    //}

}
