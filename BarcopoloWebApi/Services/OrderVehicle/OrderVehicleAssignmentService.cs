using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Vehicle;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BarcopoloWebApi.Services
{
    public class OrderVehicleAssignmentService : IOrderVehicleAssignmentService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<OrderVehicleAssignmentService> _logger;

        public OrderVehicleAssignmentService(DataBaseContext context, ILogger<OrderVehicleAssignmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AssignAsync(long orderId, long vehicleId, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !IsAdmin(user.Role.ToString().ToLower()))
            {
                _logger.LogWarning("User {UserId} is not authorized to assign vehicles.", currentUserId);
                throw new ForbiddenAccessException("شما اجازه اختصاص وسیله نقلیه را ندارید.");
            }

            _logger.LogInformation("Assigning vehicle {VehicleId} to order {OrderId}", vehicleId, orderId);

            var order = await _context.Orders.FindAsync(orderId) ?? throw new NotFoundException("سفارش یافت نشد");
            var vehicle = await _context.Vehicles.FindAsync(vehicleId) ?? throw new NotFoundException("وسیله نقلیه یافت نشد");

            var exists = await _context.OrderVehicles.AnyAsync(x => x.OrderId == orderId && x.VehicleId == vehicleId);
            if (exists)
            {
                _logger.LogWarning("Vehicle {VehicleId} already assigned to order {OrderId}", vehicleId, orderId);
                return;
            }

            var assignment = new OrderVehicle { OrderId = orderId, VehicleId = vehicleId };
            _context.OrderVehicles.Add(assignment);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Vehicle {VehicleId} assigned to order {OrderId} successfully", vehicleId, orderId);
        }

        public async Task<bool> RemoveAsync(long orderId, long vehicleId, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !IsAdmin(user.Role.ToString().ToLower()))
            {
                _logger.LogWarning("User {UserId} is not authorized to remove vehicle assignments.", currentUserId);
                throw new ForbiddenAccessException("شما اجازه حذف وسیله نقلیه را ندارید.");
            }

            _logger.LogInformation("Removing vehicle {VehicleId} from order {OrderId}", vehicleId, orderId);

            var assignment = await _context.OrderVehicles
                .FirstOrDefaultAsync(x => x.OrderId == orderId && x.VehicleId == vehicleId);

            if (assignment == null)
            {
                _logger.LogWarning("Assignment not found for order {OrderId} and vehicle {VehicleId}", orderId, vehicleId);
                return false;
            }

            _context.OrderVehicles.Remove(assignment);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Vehicle {VehicleId} removed from order {OrderId}", vehicleId, orderId);
            return true;
        }

        public async Task<IEnumerable<VehicleDto>> GetByOrderIdAsync(long orderId, long currentUserId)
        {
            _logger.LogInformation("User {UserId} retrieving vehicles for order {OrderId}", currentUserId, orderId);

            var vehicles = await _context.OrderVehicles
                .Where(x => x.OrderId == orderId)
                .Include(x => x.Vehicle)
                .ThenInclude(v => v.Driver)
                .Select(x => new VehicleDto
                {
                    Id = x.Vehicle.Id,
                    PlateNumber = x.Vehicle.PlateNumber,
                    Model = x.Vehicle.Model,
                    Color = x.Vehicle.Color,
                    SmartCardCode = x.Vehicle.SmartCardCode,
                    Axles = x.Vehicle.Axles,
                    Engine = x.Vehicle.Engine,
                    Chassis = x.Vehicle.Chassis,
                    IsBroken = x.Vehicle.IsBroken,
                    IsVan = x.Vehicle.IsVan,
                    VanCommission = x.Vehicle.VanCommission,
                    DriverId = x.Vehicle.DriverId,
                    DriverFullName = x.Vehicle.Driver != null
                        ? x.Vehicle.Driver.Person.GetFullName()
                        : null
                })
                .ToListAsync();

            return vehicles;
        }

        private bool IsAdmin(string? role) =>
            role?.ToLower() is "admin" or "superadmin";
    }
}
