namespace BarcopoloWebApi.Services
{
    public interface IOrderWarehouseAssignmentService
    {
        Task AssignAsync(long orderId, long warehouseId, long currentUserId);
        Task<long?> GetAssignedWarehouseIdAsync(long orderId, long currentUserId);
        Task<bool> RemoveAsync(long orderId, long currentUserId);
    }
}