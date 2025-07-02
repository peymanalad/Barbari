using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Cargo;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.Services.Order;
using Microsoft.EntityFrameworkCore;

public class OrderService : IOrderService
{
    private readonly DataBaseContext _context;
    private readonly IFrequentAddressService _frequentAddressService;
    private readonly ILogger<OrderService> _logger;
    private const int MaxPageSize = 100;



    public OrderService(DataBaseContext context, ILogger<OrderService> logger, IFrequentAddressService frequentAddressService)
    {
        _context = context;
        _logger = logger;
        _frequentAddressService = frequentAddressService;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, long currentUserId)
    {
        _logger.LogInformation("شروع فرآیند ثبت سفارش جدید برای {OwnerId} توسط کاربر {CurrentUserId}", dto.OwnerId, currentUserId);

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var owner = await _context.Persons.FindAsync(dto.OwnerId)
            ?? throw new NotFoundException($"مالک سفارش با شناسه {dto.OwnerId} یافت نشد.");

        if (dto.OwnerId != currentUserId && !currentUser.IsAdminOrSuperAdmin())
            throw new UnauthorizedAccessAppException("اجازه ثبت سفارش برای دیگران را ندارید.");

        if (dto.LoadingTime.HasValue && dto.LoadingTime < DateTime.UtcNow)
            throw new ArgumentException("زمان بارگیری نمی‌تواند در گذشته باشد.");

        ValidateCreateOrderDto(dto);

        var pendingCargos = await _context.Cargos
            .Where(c => c.OwnerId == dto.OwnerId && c.OrderId == null)
            .ToListAsync();

        if (!pendingCargos.Any())
        {
            _logger.LogWarning("تلاش برای ثبت سفارش بدون بار توسط کاربر {UserId}", currentUserId);
            throw new BadRequestException("برای ثبت سفارش، باید حداقل یک بار ثبت شده باشد.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            Address originAddressEntity = await DetermineOriginAddressAsync(dto);

            string destinationFullAddress = BuildDestinationAddressString(dto, out string destinationCity, out string destinationProvince, out string? postalCode, out string? plate, out string? unit, out string? title);

            if (dto.SaveDestinationAsFrequent)
            {
                var tempDestinationAddress = new Address
                {
                    FullAddress = destinationFullAddress,
                    City = destinationCity,
                    Province = destinationProvince,
                    PostalCode = postalCode,
                    Plate = plate,
                    Unit = unit,
                    Title = title
                };

                await _frequentAddressService.InsertOrUpdateAsync(
                    tempDestinationAddress,
                    FrequentAddressType.Destination,
                    personId: dto.IsForOrganization ? null : dto.OwnerId,
                    organizationId: dto.IsForOrganization ? dto.OrganizationId : null,
                    branchId: dto.IsForOrganization ? dto.BranchId : null
                );
            }

            var combinedDestinationString = $"{destinationFullAddress}, {destinationCity}, {destinationProvince}";
            if (!string.IsNullOrWhiteSpace(postalCode)) combinedDestinationString += $" (کدپستی: {postalCode})";
            if (!string.IsNullOrWhiteSpace(plate)) combinedDestinationString += $" (پلاک: {plate})";
            if (!string.IsNullOrWhiteSpace(unit)) combinedDestinationString += $" (واحد: {unit})";

            var order = new Order
            {
                OwnerId = dto.OwnerId,
                OriginAddressId = originAddressEntity.Id,
                DestinationAddress = combinedDestinationString,
                SenderName = dto.SenderName,
                SenderPhone = dto.SenderPhone,
                ReceiverName = dto.ReceiverName,
                ReceiverPhone = dto.ReceiverPhone,
                OrderDescription = dto.Details,
                LoadingTime = dto.LoadingTime,
                Status = OrderStatus.Pending,
                TrackingNumber = GenerateTrackingNumber(),
                CreatedAt = DateTime.UtcNow,
                OrganizationId = dto.IsForOrganization ? dto.OrganizationId : null,
                BranchId = dto.IsForOrganization ? dto.BranchId : null,
                DeclaredValue = dto.DeclaredValue,
                IsInsuranceRequested = dto.IsInsuranceRequested,
                Fare = dto.Fare,
                Insurance = dto.Insurance,
                Vat = dto.Vat
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var cargo in pendingCargos)
            {
                cargo.OrderId = order.Id;
            }
            await _context.SaveChangesAsync();

            _context.OrderEvents.Add(new OrderEvent
            {
                OrderId = order.Id,
                Status = OrderStatus.Pending,
                EventDateTime = DateTime.UtcNow,
                Remarks = "سفارش ثبت شد",
                ChangedByPersonId = currentUserId
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("سفارش جدید با شناسه {OrderId} و شماره پیگیری {TrackingNumber} با موفقیت ثبت شد", order.Id, order.TrackingNumber);

            var createdOrderWithIncludes = await _context.Orders
                .Include(o => o.Owner)
                .Include(o => o.OriginAddress)
                .Include(o => o.Organization)
                .Include(o => o.Branch)
                .Include(o => o.Cargos).ThenInclude(c => c.Images)
                .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
                .Include(o => o.Events).ThenInclude(e => e.ChangedByPerson)
                .Include(o => o.Payments)
                .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
                .Include(o => o.Warehouse)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            if (createdOrderWithIncludes == null)
            {
                _logger.LogError("Order created (ID: {OrderId}) but could not be retrieved immediately after creation.", order.Id);
                throw new ApplicationException("Order created but could not be retrieved.");
            }

            return MapToOrderDto(createdOrderWithIncludes);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در ثبت سفارش جدید برای {OwnerId}", dto.OwnerId);
            throw;
        }
    }
    public async Task<OrderDto> GetByIdAsync(long id, long currentUserId)
    {
        _logger.LogInformation("دریافت سفارش {OrderId} توسط کاربر {UserId}", id, currentUserId);

        var order = await _context.Orders
            .Include(o => o.OriginAddress)
            .Include(o => o.Warehouse)
            .Include(o => o.Organization)
            .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
            .Include(o => o.Cargos).ThenInclude(c => c.Images)
            .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
            .Include(o => o.Payments)
            .Include(o => o.Events).ThenInclude(e => e.ChangedByPerson)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException("سفارشی با این شناسه یافت نشد.");

        var user = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, user, _context);
        return MapToOrderDto(order);
    }

    public async Task<PagedResult<OrderDto>> GetByOwnerAsync(long ownerId, long currentUserId, int pageNumber, int pageSize)
    {
        _logger.LogInformation("دریافت سفارش‌های مالک {OwnerId} توسط کاربر {UserId} - صفحه {PageNumber} اندازه {PageSize}",
            ownerId, currentUserId, pageNumber, pageSize);

        var user = await _context.Persons.FindAsync(currentUserId)
                   ?? throw new NotFoundException("کاربر جاری یافت نشد.");
        if (currentUserId != ownerId && !user.IsAdminOrSuperAdminOrMonitor())
        {
            _logger.LogWarning("Attempt by non-admin/monitor user {CurrentUserId} (Role: {UserRole}) to access orders of owner {OwnerId}. Access denied.",
                currentUserId, user.Role, ownerId);
            throw new UnauthorizedAccessAppException("شما دسترسی لازم برای مشاهده سفارش‌هاي اين كاربر را ندارید.");
        }

        var query = _context.Orders.Where(o => o.OwnerId == ownerId);
        var totalCount = await query.CountAsync();
        query = query.OrderByDescending(o => o.CreatedAt);
        query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        query = query
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
        var orderDtos = orders.Select(MapToOrderDto).ToList();

        var pagedResult = new PagedResult<OrderDto>
        {
            Items = orderDtos,
            TotalCount = totalCount,
            Page = pageNumber, 
            PageSize = pageSize
        };

        return pagedResult;
    }
    public async Task<OrderStatusDto> GetByTrackingNumberAsync(string trackingNumber)
    {
        _logger.LogInformation("در حال بررسی وضعیت سفارش با شماره پیگیری {TrackingNumber}", trackingNumber);

        var order = await _context.Orders
            .Include(o => o.Events)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.TrackingNumber == trackingNumber);

        if (order == null)
            throw new NotFoundException("سفارشی با این شماره پیگیری یافت نشد.");

        var latestEvent = order.Events
            .OrderByDescending(e => e.EventDateTime)
            .FirstOrDefault();

        return new OrderStatusDto
        {
            OrderId = order.Id,
            TrackingNumber = order.TrackingNumber,
            Status = order.Status.ToString(),
            Remarks = latestEvent?.Remarks ?? "وضعیت ثبت شده‌ای موجود نیست.",
            LastUpdated = latestEvent?.EventDateTime
        };
    }
    public async Task<PagedResult<OrderDto>> GetAllAsync(long currentUserId, int pageNumber, int pageSize)
    {

        _logger.LogInformation("دریافت لیست تمام سفارش‌ها توسط کاربر {UserId} - صفحه {PageNumber} اندازه {PageSize}",
            currentUserId, pageNumber, pageSize);
        var user = await _context.Persons.FindAsync(currentUserId)
                   ?? throw new NotFoundException("کاربر جاری یافت نشد.");
        if (!user.IsAdminOrSuperAdminOrMonitor())
        {
            _logger.LogWarning("کاربر غیرمجاز {UserId} با نقش {UserRole} تلاش کرد به لیست تمام سفارش‌ها دسترسی پیدا کند.",
                currentUserId, user.Role);
            throw new UnauthorizedAccessAppException("شما دسترسی لازم برای مشاهده تمام سفارش‌ها را ندارید.");
        }
        var query = _context.Orders.AsQueryable();
        var totalCount = await query.CountAsync();
        query = query.OrderByDescending(o => o.CreatedAt);
        query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        query = query
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
        var orderDtos = orders.Select(MapToOrderDto).ToList();


        var pagedResult = new PagedResult<OrderDto>
        {
            Items = orderDtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return pagedResult;
    }
    public async Task<OrderDto> UpdateAsync(long orderId, UpdateOrderDto dto, long currentUserId)
    {
        _logger.LogInformation("درخواست به‌روزرسانی سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        using var transaction = await _context.Database.BeginTransactionAsync();
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

            var currentUser = await _context.Persons
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد.");

            if (!await CanUserModifyOrderAsync(order, currentUser))
            {
                throw new UnauthorizedAccessAppException($"شما دسترسی لازم برای به‌روزرسانی سفارش {orderId} را ندارید.");
            }

            if (order.Status >= OrderStatus.Assigned)
            {
                throw new InvalidOperationException($"سفارش در وضعیت '{order.Status}' قابل ویرایش نیست. فقط سفارشات در وضعیت Pending قابل ویرایش هستند.");
            }

            bool hasChanges = false; 


            if (dto.SenderName != null)
            {
                order.SenderName = dto.SenderName;
                hasChanges = true;
            }
            if (dto.SenderPhone != null)
            {
                order.SenderPhone = dto.SenderPhone;
                hasChanges = true;
            }
            if (dto.ReceiverName != null)
            {
                order.ReceiverName = dto.ReceiverName;
                hasChanges = true;
            }
            if (dto.ReceiverPhone != null)
            {
                order.ReceiverPhone = dto.ReceiverPhone;
                hasChanges = true;
            }
            if (dto.Details != null) 
            {
                order.OrderDescription = dto.Details;
                hasChanges = true;
            }

            if (dto.LoadingTime.HasValue)
            {
                order.LoadingTime = dto.LoadingTime.Value;
                hasChanges = true;
            }

            if (dto.DeclaredValue.HasValue)
            {
                order.DeclaredValue = dto.DeclaredValue.Value;
                hasChanges = true;
            }

            if (dto.IsInsuranceRequested.HasValue)
            {
                order.IsInsuranceRequested = dto.IsInsuranceRequested.Value;
                hasChanges = true;
            }


            if (hasChanges)
            {
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
            }
            else
            {
                await transaction.RollbackAsync();
                _logger.LogInformation("هیچ تغییری برای به‌روزرسانی سفارش {OrderId} ارائه نشده بود.", orderId);
            }



            var finalOrderState = await _context.Orders
                .Include(o => o.Owner)
                .Include(o => o.OriginAddress)
                .Include(o => o.Organization)
                .Include(o => o.Branch)
                .Include(o => o.Cargos).ThenInclude(c => c.Images)
                .Include(o => o.Cargos).ThenInclude(c => c.CargoType)
                .Include(o => o.Events).ThenInclude(e => e.ChangedByPerson)
                .Include(o => o.Payments)
                .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
                .Include(o => o.Warehouse)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

        
            if (finalOrderState == null)
            {
                _logger.LogError("Failed to re-fetch order {OrderId} after update.", orderId);

                return MapToOrderDto(order); 
            }

            return MapToOrderDto(finalOrderState);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در به‌روزرسانی سفارش {OrderId}", orderId);
            throw; // Re-throw the exception
        }
    }
    public async Task CancelAsync(long orderId, long currentUserId, string? cancellationReason = null)
    {
        _logger.LogInformation("درخواست لغو سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Fetch Order with necessary includes for authorization
            // Include relations needed for access check
            var order = await _context.Orders
                .Include(o => o.Owner) // Needed for owner check
                .Include(o => o.Organization).ThenInclude(org => org.Memberships) // Needed for role check
                .Include(o => o.Branch).ThenInclude(b => b.Memberships) // Needed for role check
                .Include(o => o.Events) // Needed for adding new event
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"سفارش با شناسه {orderId} یافت نشد.");

            var currentUser = await _context.Persons
                .Include(p => p.Memberships) // Include memberships for access check
                .FirstOrDefaultAsync(p => p.Id == currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد.");


            // --- Authorization Check ---
            if (!await CanUserCancelOrderAsync(order, currentUser))
            {
                throw new UnauthorizedAccessAppException($"شما دسترسی لازم برای لغو سفارش {orderId} را ندارید.");
            }

            // --- Business Rule Check ---
            // Check if the order status allows cancellation (only before Assigned)
            if (order.Status >= OrderStatus.Assigned) // Cannot cancel if Assigned or later
            {
                throw new InvalidOperationException($"سفارش در وضعیت '{order.Status}' قابل لغو نیست. فقط سفارشات در وضعیت Pending قابل لغو هستند.");
            }

            // Check if already cancelled (as in original code)
            if (order.Status == OrderStatus.Cancelled)
            {
                _logger.LogWarning("سفارش {OrderId} قبلاً لغو شده است. درخواست لغو توسط {UserId} نادیده گرفته شد.", orderId, currentUserId);
                await transaction.RollbackAsync(); // No changes needed, rollback transaction
                return; // Or return false as in original? Let's return void to indicate success/no error.
            }

            // --- Update Order Status ---
            // Assuming Order.SetStatus exists and handles internal logic/validation if any
            // order.SetStatus(OrderStatus.Cancelled);
            // If SetStatus doesn't exist or has no validation, set directly:
            order.Status = OrderStatus.Cancelled;


            // --- Create Order Event ---
            string remarks = string.IsNullOrWhiteSpace(cancellationReason)
                ? "سفارش لغو شد." // Simplified message
                : $"سفارش لغو شد. دلیل: {cancellationReason}";

            _context.OrderEvents.Add(new OrderEvent
            {
                OrderId = order.Id,
                Status = OrderStatus.Cancelled,
                EventDateTime = DateTime.UtcNow,
                Remarks = remarks,
                ChangedByPersonId = currentUserId
            });

            await _context.SaveChangesAsync(); // Save changes (Order status + Event)
            await transaction.CommitAsync();

            _logger.LogInformation("سفارش {OrderId} با موفقیت لغو شد.", orderId);

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در لغو سفارش {OrderId}", orderId);
            throw;
        }

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

            var currentUser = await _context.Persons.FindAsync(currentUserId)
                 ?? throw new NotFoundException("کاربر جاری یافت نشد.");

            if (!currentUser.IsAdminOrSuperAdminOrMonitor())
            {
                _logger.LogWarning("کاربر غیرمجاز {UserId} تلاش کرد وضعیت سفارش {OrderId} را تغییر دهد.", currentUserId, orderId);
                throw new UnauthorizedAccessAppException("شما دسترسی لازم برای تغییر وضعیت سفارش را ندارید.");
            }


            if ((int)dto.NewStatus < (int)order.Status)
                throw new InvalidOperationException($"امکان بازگشت وضعیت سفارش از '{order.Status}' به '{dto.NewStatus}' وجود ندارد.");

            if (dto.NewStatus == order.Status)
            {
                _logger.LogInformation("وضعیت سفارش {OrderId} هم اکنون {Status} است. تغییری اعمال نشد.", orderId, order.Status);
                await transaction.RollbackAsync(); 
                return;
            }

            order.Status = dto.NewStatus;

            if (dto.NewStatus == OrderStatus.Delivered && order.DeliveryTime == null)
            {
                order.DeliveryTime = DateTime.UtcNow;
                _logger.LogInformation("زمان تحویل برای سفارش {OrderId} ثبت شد.", orderId);
            }

            _context.OrderEvents.Add(new OrderEvent
            {
                OrderId = order.Id,
                Status = dto.NewStatus,
                EventDateTime = DateTime.UtcNow,
                Remarks = dto.Remarks ?? $"وضعیت به '{dto.NewStatus}' تغییر یافت.", 
                ChangedByPersonId = currentUserId
            });

            await _context.SaveChangesAsync(); 
            await transaction.CommitAsync();

            _logger.LogInformation("وضعیت سفارش {OrderId} با موفقیت به {NewStatus} تغییر یافت.", order.Id, dto.NewStatus);

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در تغییر وضعیت سفارش {OrderId} به {NewStatus}", orderId, dto.NewStatus);
            throw;
        }
    }

    private void ValidateCreateOrderDto(CreateOrderDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.SenderName)) errors.Add("نام فرستنده الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.SenderPhone)) errors.Add("شماره فرستنده الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.ReceiverName)) errors.Add("نام گیرنده الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.ReceiverPhone)) errors.Add("شماره گیرنده الزامی است.");

        if (dto.IsManualDestination)
        { 
            if (string.IsNullOrWhiteSpace(dto.DestinationFullAddress)) errors.Add("آدرس کامل مقصد الزامی است.");
            if (string.IsNullOrWhiteSpace(dto.DestinationCity)) errors.Add("شهر مقصد الزامی است.");
            if (string.IsNullOrWhiteSpace(dto.DestinationProvince)) errors.Add("استان مقصد الزامی است.");
        }
        else
        {

            if (string.IsNullOrWhiteSpace(dto.DestinationFullAddress)) errors.Add("آدرس کامل مقصد الزامی است.");
            if (string.IsNullOrWhiteSpace(dto.DestinationCity)) errors.Add("شهر مقصد الزامی است.");
            if (string.IsNullOrWhiteSpace(dto.DestinationProvince)) errors.Add("استان مقصد الزامی است.");
        }


        if (dto.IsForOrganization)
        {
            if (!dto.OrganizationId.HasValue)
                errors.Add("برای سفارش سازمانی، شناسه سازمان الزامی است.");
            else
            {
                // Further check if BranchId belongs to OrgId could be done here or later
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
        // Prioritize BranchId if provided
        if (branchId.HasValue)
        {
            var branch = await _context.SubOrganizations
                // Ensure branch belongs to the specified OrgId if also provided
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
            // This case should be caught by ValidateCreateOrderDto earlier
            throw new BadRequestException("شناسه سازمان یا شعبه برای دریافت آدرس مبدا سازمانی الزامی است.");
        }
    }
    private async Task<bool> CanUserModifyOrderAsync(Order order, Person currentUser)
    {
        // For now, update permissions are the same as cancel permissions.
        // This logic can be made more granular if needed (e.g., allow Branch User to update but not cancel).
        return await CanUserCancelOrderAsync(order, currentUser);
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
}
