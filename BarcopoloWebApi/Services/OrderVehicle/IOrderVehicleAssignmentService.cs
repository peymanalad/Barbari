using BarcopoloWebApi.DTOs.Vehicle;

namespace BarcopoloWebApi.Services
{
    public interface IOrderVehicleAssignmentService
    {
        Task AssignAsync(long orderId, long vehicleId, long currentUserId);
        Task<bool> RemoveAsync(long orderId, long vehicleId, long currentUserId);
        Task<IEnumerable<VehicleDto>> GetByOrderIdAsync(long orderId, long currentUserId);
    }
}