using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Cargo;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Services.Order;
using Microsoft.EntityFrameworkCore;

public class OrderService : IOrderService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<OrderService> _logger;



    public OrderService(DataBaseContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, long currentUserId)
    {
        _logger.LogInformation("در حال ثبت سفارش جدید برای کاربر {OwnerId} توسط {UserId}", dto.OwnerId, currentUserId);

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (dto.OwnerId != currentUserId && !currentUser.IsAdmin())
            throw new UnauthorizedAccessAppException("اجازه ثبت سفارش برای دیگران را ندارید.");

        if (dto.Fare < 0 || dto.Insurance < 0 || dto.Vat < 0)
            throw new ArgumentException("مقادیر هزینه نمی‌توانند منفی باشند.");

        if (dto.LoadingTime.HasValue && dto.LoadingTime < DateTime.UtcNow)
            throw new ArgumentException("زمان بارگیری نمی‌تواند در گذشته باشد.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var newOrder = new Order
            {
                OwnerId = dto.OwnerId,
                OriginAddressId = dto.OriginAddressId,
                DestinationAddressId = dto.DestinationAddressId,
                SenderName = dto.SenderName,
                SenderPhone = dto.SenderPhone,
                ReceiverName = dto.ReceiverName,
                ReceiverPhone = dto.ReceiverPhone,
                OrderDescription = dto.Details,
                Fare = dto.Fare,
                Insurance = dto.Insurance,
                Vat = dto.Vat,
                LoadingTime = dto.LoadingTime,
                Status = OrderStatus.Pending,
                TrackingNumber = GenerateTrackingNumber(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            _context.OrderEvents.Add(new OrderEvent
            {
                OrderId = newOrder.Id,
                Status = OrderStatus.Pending,
                EventDateTime = DateTime.UtcNow,
                Remarks = "سفارش توسط کاربر ثبت شد",
                ChangedByPersonId = currentUserId
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToOrderDto(newOrder);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در ثبت سفارش جدید");
            throw;
        }
    }

    public async Task<OrderDto> GetByIdAsync(long id, long currentUserId)
    {
        _logger.LogInformation("دریافت سفارش {OrderId} توسط کاربر {UserId}", id, currentUserId);

        var order = await _context.Orders
            .Include(o => o.OriginAddress)
            .Include(o => o.DestinationAddress)
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

    public async Task<IEnumerable<OrderDto>> GetByOwnerAsync(long ownerId, long currentUserId)
    {
        _logger.LogInformation("دریافت سفارش‌های کاربر {OwnerId} توسط {UserId}", ownerId, currentUserId);

        var currentUser = await _context.Persons.Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var isAdmin = currentUser.IsAdmin() || currentUser.IsSuperAdmin();

        if (ownerId != currentUserId && !isAdmin)
        {
            var owner = await _context.Persons.Include(p => p.Memberships)
                .AsNoTracking().FirstOrDefaultAsync(p => p.Id == ownerId)
                ?? throw new NotFoundException("کاربر مالک سفارش یافت نشد.");

            var sharedOrg = owner.Memberships.Any(m => currentUser.Memberships.Any(cm => cm.OrganizationId == m.OrganizationId));
            var sharedBranch = owner.Memberships.Any(m => m.BranchId.HasValue &&
                currentUser.Memberships.Any(cm => cm.BranchId == m.BranchId));

            if (!sharedOrg && !sharedBranch)
                throw new UnauthorizedAccessAppException("شما مجاز به مشاهده سفارش‌های این کاربر نیستید.");
        }

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OwnerId == ownerId)
            .Include(o => o.OriginAddress)
            .Include(o => o.DestinationAddress)
            .Include(o => o.Warehouse)
            .Include(o => o.Organization)
            .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
            .ToListAsync();

        return orders.Select(MapToOrderDto).ToList();
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


    public async Task<OrderDto> UpdateAsync(long id, UpdateOrderDto dto, long currentUserId)
    {
        _logger.LogInformation("بروزرسانی سفارش {OrderId} توسط {UserId}", id, currentUserId);

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var order = await _context.Orders
            .Include(o => o.OriginAddress)
            .Include(o => o.DestinationAddress)
            .Include(o => o.Warehouse)
            .Include(o => o.Organization)
            .Include(o => o.OrderVehicles).ThenInclude(ov => ov.Vehicle)
            .Include(o => o.Cargos)
            .Include(o => o.Payments)
            .Include(o => o.Events).ThenInclude(e => e.ChangedByPerson)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        if (order.Status >= OrderStatus.Assigned && !currentUser.IsAdminOrSuperAdmin())
            throw new UnauthorizedAccessAppException("سفارش در این مرحله قابل ویرایش نیست.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            bool isOwner = order.OwnerId == currentUserId;

            if (isOwner || currentUser.IsAdminOrSuperAdmin())
            {
                order.OriginAddressId = dto.OriginAddressId ?? order.OriginAddressId;
                order.DestinationAddressId = dto.DestinationAddressId ?? order.DestinationAddressId;
                order.SenderName = dto.SenderName ?? order.SenderName;
                order.SenderPhone = dto.SenderPhone ?? order.SenderPhone;
                order.ReceiverName = dto.ReceiverName ?? order.ReceiverName;
                order.ReceiverPhone = dto.ReceiverPhone ?? order.ReceiverPhone;
                order.OrderDescription = dto.OrderDescription ?? order.OrderDescription;
            }

            if (currentUser.IsAdminOrSuperAdmin())
            {
                order.Fare = dto.Fare ?? order.Fare;
                order.Insurance = dto.Insurance ?? order.Insurance;
                order.Vat = dto.Vat ?? order.Vat;
                order.LoadingTime = dto.LoadingTime ?? order.LoadingTime;
                order.DeliveryTime = dto.DeliveryTime ?? order.DeliveryTime;
                order.TrackingNumber = dto.TrackingNumber ?? order.TrackingNumber;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToOrderDto(order);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در بروزرسانی سفارش");
            throw;
        }
    }

    public async Task<bool> CancelAsync(long orderId, long currentUserId)
    {
        _logger.LogInformation("در حال لغو سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var order = await _context.Orders.Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        if (order.Status == OrderStatus.Cancelled)
            return true;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            order.SetStatus(OrderStatus.Cancelled);

            _context.OrderEvents.Add(new OrderEvent
            {
                OrderId = order.Id,
                Status = OrderStatus.Cancelled,
                EventDateTime = DateTime.UtcNow,
                Remarks = "سفارش توسط کاربر لغو شد",
                ChangedByPersonId = currentUserId
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در لغو سفارش");
            throw;
        }
    }

    public async Task<OrderStatusDto> ChangeStatusAsync(long orderId, OrderStatus newStatus, string? remarks, long currentUserId)
    {
        _logger.LogInformation("تغییر وضعیت سفارش {OrderId} به {NewStatus} توسط {UserId}", orderId, newStatus, currentUserId);

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var order = await _context.Orders.Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        if ((int)newStatus < (int)order.Status)
            throw new ArgumentException("امکان بازگشت وضعیت سفارش وجود ندارد.");

        if ((int)newStatus >= (int)OrderStatus.Assigned && !currentUser.IsAdminOrSuperAdmin())
            throw new UnauthorizedAccessAppException("تنها مدیران می‌توانند وضعیت را به این مرحله تغییر دهند.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            order.Status = newStatus;

            var statusEvent = new OrderEvent
            {
                OrderId = order.Id,
                Status = newStatus,
                EventDateTime = DateTime.UtcNow,
                Remarks = remarks ?? $"وضعیت به {newStatus} تغییر یافت",
                ChangedByPersonId = currentUserId
            };

            _context.OrderEvents.Add(statusEvent);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new OrderStatusDto
            {
                OrderId = order.Id,
                TrackingNumber = order.TrackingNumber,
                Status = newStatus.ToString(),
                Remarks = statusEvent.Remarks,
                LastUpdated = statusEvent.EventDateTime
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در تغییر وضعیت سفارش");
            throw;
        }
    }


    private string GenerateTrackingNumber()
        => $"TRK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";

    private OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            TrackingNumber = order.TrackingNumber,
            Status = order.Status.ToString(),
            OriginAddress = order.OriginAddress?.FullAddress ?? "—",
            DestinationAddress = order.DestinationAddress?.FullAddress ?? "—",
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
                Value = c.Value,
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
