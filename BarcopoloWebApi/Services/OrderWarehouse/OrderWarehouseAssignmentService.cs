using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BarcopoloWebApi.Services
{
    public class OrderWarehouseAssignmentService : IOrderWarehouseAssignmentService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<OrderWarehouseAssignmentService> _logger;

        public OrderWarehouseAssignmentService(DataBaseContext context, ILogger<OrderWarehouseAssignmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AssignAsync(long orderId, long warehouseId, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !IsAdmin(user.Role.ToString().ToLower()))
            {
                _logger.LogWarning("User {UserId} is not authorized to assign warehouses.", currentUserId);
                throw new ForbiddenAccessException("شما اجازه اختصاص انبار را ندارید.");
            }

            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("سفارش یافت نشد");

            var warehouse = await _context.Warehouses.FindAsync(warehouseId)
                ?? throw new NotFoundException("انبار یافت نشد");

            order.WarehouseId = warehouseId;

            _logger.LogInformation("Warehouse {WarehouseId} assigned to Order {OrderId} by user {UserId}", warehouseId, orderId, currentUserId);

            await _context.SaveChangesAsync();
        }

        public async Task<long?> GetAssignedWarehouseIdAsync(long orderId, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !IsAdmin(user.Role.ToString().ToLower()))
            {
                _logger.LogWarning("User {UserId} is not authorized to view assigned warehouse.", currentUserId);
                throw new ForbiddenAccessException("شما اجازه مشاهده انبار را ندارید.");
            }

            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("سفارش یافت نشد");

            return order.WarehouseId;
        }

        public async Task<bool> RemoveAsync(long orderId, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !IsAdmin(user.Role.ToString().ToLower()))
            {
                _logger.LogWarning("User {UserId} is not authorized to remove warehouse from order.", currentUserId);
                throw new ForbiddenAccessException("شما اجازه حذف انبار را ندارید.");
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                return false;
            }

            if (!order.WarehouseId.HasValue)
            {
                _logger.LogInformation("Order {OrderId} has no assigned warehouse", orderId);
                return false;
            }

            order.WarehouseId = null;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Warehouse removed from order {OrderId} by user {UserId}", orderId, currentUserId);

            return true;
        }

        private bool IsAdmin(string? role)
        {
            return role?.ToLower() is "admin" or "superadmin";
        }
    }
}
