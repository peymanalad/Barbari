using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.Services.Order
{
    public interface IOrderService
    {
        Task<OrderDto> CreateAsync(CreateOrderDto dto, long currentUserId);
        Task<OrderDto> UpdateAsync(long id, UpdateOrderDto dto, long currentUserId);
        Task CancelAsync(long id, long currentUserId, string? cancellationReason = null);

        Task<OrderDto> GetByIdAsync(long id, long currentUserId);
        Task<OrderStatusDto> GetByTrackingNumberAsync(string trackingNumber);
        Task<PagedResult<OrderDto>> GetByOwnerAsync(long ownerId, long currentUserId, int pageNumber, int pageSize);
        Task<PagedResult<OrderDto>> GetAllAsync(long currentUserId, int page, int pageSize);
        Task ChangeStatusAsync(long orderId, ChangeOrderStatusDto dto, long currentUserId);
        Task AssignOrderPersonnelAsync(long orderId, AssignOrderPersonnelDto dto, long currentUserId);


    }
}