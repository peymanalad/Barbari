using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Vehicle;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarcopoloWebApi.Services
{
    public class OrderVehicleAssignmentService : IOrderVehicleAssignmentService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<OrderVehicleAssignmentService> _logger;
        private readonly IMapper _mapper;

        public OrderVehicleAssignmentService(
            DataBaseContext context,
            ILogger<OrderVehicleAssignmentService> logger,
            IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task AssignAsync(long orderId, long vehicleId, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId)
                ?? throw new NotFoundException("کاربر یافت نشد.");

            if (!user.IsAdminOrSuperAdminOrMonitor())
            {
                _logger.LogWarning("کاربر {UserId} مجاز به اختصاص وسیله نقلیه نیست.", currentUserId);
                throw new ForbiddenAccessException("دسترسی غیرمجاز.");
            }

            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("سفارش یافت نشد.");
            var vehicle = await _context.Vehicles.FindAsync(vehicleId)
                ?? throw new NotFoundException("وسیله نقلیه یافت نشد.");

            var exists = await _context.OrderVehicles
                .AnyAsync(x => x.OrderId == orderId && x.VehicleId == vehicleId);

            if (exists)
            {
                _logger.LogWarning("وسیله نقلیه {VehicleId} قبلاً به سفارش {OrderId} اختصاص داده شده است.", vehicleId, orderId);
                return;
            }

            _context.OrderVehicles.Add(new OrderVehicle
            {
                OrderId = orderId,
                VehicleId = vehicleId
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("وسیله نقلیه {VehicleId} با موفقیت به سفارش {OrderId} اختصاص داده شد.", vehicleId, orderId);
        }

        public async Task<bool> RemoveAsync(long orderId, long vehicleId, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId)
                ?? throw new NotFoundException("کاربر یافت نشد.");

            if (!user.IsAdminOrSuperAdminOrMonitor())
            {
                _logger.LogWarning("کاربر {UserId} مجاز به حذف وسیله نقلیه نیست.", currentUserId);
                throw new ForbiddenAccessException("دسترسی غیرمجاز.");
            }

            var assignment = await _context.OrderVehicles
                .FirstOrDefaultAsync(x => x.OrderId == orderId && x.VehicleId == vehicleId);

            if (assignment == null)
            {
                _logger.LogWarning("رابطه‌ای برای حذف یافت نشد: OrderId = {OrderId}, VehicleId = {VehicleId}", orderId, vehicleId);
                return false;
            }

            _context.OrderVehicles.Remove(assignment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("رابطه وسیله نقلیه {VehicleId} با سفارش {OrderId} با موفقیت حذف شد.", vehicleId, orderId);
            return true;
        }

        public async Task<IEnumerable<VehicleDto>> GetByOrderIdAsync(long orderId, long currentUserId)
        {
            var orderExists = await _context.Orders.AnyAsync(x => x.Id == orderId);
            if (!orderExists)
                throw new NotFoundException("سفارش یافت نشد.");

            var vehicles = await _context.OrderVehicles
                .Where(x => x.OrderId == orderId)
                .Include(x => x.Vehicle)
                .ThenInclude(v => v.Driver)
                .ThenInclude(d => d.Person)
                .Select(x => x.Vehicle)
                .ProjectTo<VehicleDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            _logger.LogInformation("تعداد {Count} وسیله نقلیه برای سفارش {OrderId} توسط کاربر {UserId} بازیابی شد.", vehicles.Count, orderId, currentUserId);

            return vehicles;
        }

    }
}
