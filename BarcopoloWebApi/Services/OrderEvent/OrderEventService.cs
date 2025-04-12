using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Services.OrderEvent;
using Microsoft.EntityFrameworkCore;

public class OrderEventService : IOrderEventService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<OrderEventService> _logger;

    public OrderEventService(DataBaseContext context, ILogger<OrderEventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderEventDto> CreateAsync(CreateOrderEventDto dto, long currentUserId)
    {
        _logger.LogInformation("ایجاد رویداد جدید برای سفارش {OrderId} توسط کاربر {UserId}", dto.OrderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Organization)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId)
            ?? throw new NotFoundException("سفارش مورد نظر یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        if (!Enum.TryParse<OrderStatus>(dto.Status, ignoreCase: true, out var newStatus))
            throw new ArgumentException("وضعیت وارد شده معتبر نیست.");

        var orderEvent = new OrderEvent
        {
            OrderId = dto.OrderId,
            Status = newStatus,
            Remarks = dto.Remarks?.Trim(),
            ChangedByPersonId = currentUserId,
            EventDateTime = DateTime.UtcNow
        };

        _context.OrderEvents.Add(orderEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("رویداد جدید با شناسه {EventId} برای سفارش {OrderId} ثبت شد", orderEvent.Id, dto.OrderId);

        return new OrderEventDto
        {
            Id = orderEvent.Id,
            OrderId = orderEvent.OrderId,
            Status = orderEvent.Status.ToString(),
            EventDateTime = orderEvent.EventDateTime,
            Remarks = orderEvent.Remarks,
            ChangedByFullName = currentUser.GetFullName()
        };
    }

    public async Task<IEnumerable<OrderEventDto>> GetByOrderIdAsync(long orderId, long currentUserId)
    {
        _logger.LogInformation("در حال دریافت رویدادهای سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Organization)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        var events = await _context.OrderEvents
            .Where(e => e.OrderId == orderId)
            .Include(e => e.ChangedByPerson)
            .OrderByDescending(e => e.EventDateTime)
            .ToListAsync();

        return events.Select(e => new OrderEventDto
        {
            Id = e.Id,
            OrderId = e.OrderId,
            Status = e.Status.ToString(),
            EventDateTime = e.EventDateTime,
            Remarks = e.Remarks,
            ChangedByFullName = e.ChangedByPerson?.GetFullName() ?? "سیستم"
        }).ToList();
    }
}
