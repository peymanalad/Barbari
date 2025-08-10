using AutoMapper;
using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BarcopoloWebApi.Services.OrderEvent
{
    public class OrderEventService : IOrderEventService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<OrderEventService> _logger;
        private readonly IMapper _mapper;

        public OrderEventService(DataBaseContext context, ILogger<OrderEventService> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<OrderEventDto> CreateAsync(CreateOrderEventDto dto, long currentUserId)
        {
            _logger.LogInformation("در حال ایجاد رویداد برای سفارش {OrderId} توسط کاربر {UserId}", dto.OrderId, currentUserId);

            var order = await _context.Orders
                .Include(o => o.Organization)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId)
                ?? throw new NotFoundException("سفارش مورد نظر یافت نشد.");

            var currentUser = await _context.Persons.FindAsync(currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد.");

            await OrderAccessGuard.EnsureUserCanAccessOrderEventAsync(order, currentUser, _context);

            if (!Enum.TryParse<OrderStatus>(dto.Status, ignoreCase: true, out var newStatus))
                throw new BadRequestException("وضعیت وارد شده معتبر نیست.");

            var currentStatus = await _context.OrderEvents
                .Where(e => e.OrderId == dto.OrderId)
                .OrderByDescending(e => e.EventDateTime)
                .Select(e => (OrderStatus?)e.Status)
                .FirstOrDefaultAsync() ?? order.Status;

            if (currentUser.IsAdminOrSuperAdminOrMonitor())
            {
                // Admin roles can change status freely
                _logger.LogInformation("وضعیت سفارش {OrderId} توسط مدیر {UserId} از {OldStatus} به {NewStatus} تغییر کرد.",
                    dto.OrderId, currentUserId, currentStatus, newStatus);
            }
            else if (currentUser.Role == SystemRole.user)
            {
                // Check if this user is the assigned driver
                var assignedDriverPersonId = await _context.OrderVehicles
                    .Where(ov => ov.OrderId == dto.OrderId)
                    .Select(ov => ov.Vehicle.Driver.PersonId)
                    .FirstOrDefaultAsync();

                if (assignedDriverPersonId == 0 || assignedDriverPersonId != currentUserId)
                    throw new ForbiddenAccessException("شما راننده اختصاص داده‌شده به این سفارش نیستید.");


                var allowedTransitions = new Dictionary<OrderStatus, OrderStatus>
        {
            { OrderStatus.Assigned, OrderStatus.Loading },
            { OrderStatus.Loading, OrderStatus.InProgress },
            { OrderStatus.InProgress, OrderStatus.Unloading },
            { OrderStatus.Unloading, OrderStatus.Delivered }
        };

                if (!allowedTransitions.TryGetValue(currentStatus, out var allowedNextStatus) || newStatus != allowedNextStatus)
                {
                    throw new ForbiddenAccessException($"شما فقط می‌توانید وضعیت را از '{currentStatus}' به '{allowedNextStatus}' تغییر دهید.");
                }
            }
            else
            {
                throw new ForbiddenAccessException("شما مجاز به تغییر وضعیت سفارش نیستید.");
            }

            var orderEvent = new Entities.OrderEvent
            {
                OrderId = dto.OrderId,
                Status = newStatus,
                Remarks = dto.Remarks?.Trim(),
                ChangedByPersonId = currentUserId,
                EventDateTime = DateTime.UtcNow
            };

            _context.OrderEvents.Add(orderEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("رویداد {EventId} برای سفارش {OrderId} با وضعیت {Status} ثبت شد",
                orderEvent.Id, order.Id, orderEvent.Status);

            var resultDto = _mapper.Map<OrderEventDto>(orderEvent);
            resultDto.ChangedByFullName = currentUser.GetFullName();

            return resultDto;
        }

        public async Task<IEnumerable<OrderEventDto>> GetByOrderIdAsync(long orderId, long currentUserId)
        {
            _logger.LogInformation("دریافت رویدادهای سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

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

            var dtoList = _mapper.Map<List<OrderEventDto>>(events);

            foreach (var dto in dtoList)
            {
                var person = events.First(e => e.Id == dto.Id).ChangedByPerson;
                dto.ChangedByFullName = person?.GetFullName() ?? "سیستم";
            }

            return dtoList;
        }
    }
}
