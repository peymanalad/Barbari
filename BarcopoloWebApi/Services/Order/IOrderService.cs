using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.Services.Order
{
    public interface IOrderService
    {
        Task<OrderDto> CreateAsync(CreateOrderDto dto, long currentUserId);
        Task<OrderDto> UpdateAsync(long id, UpdateOrderDto dto, long currentUserId);
        Task<bool> CancelAsync(long id, long currentUserId);

        Task<OrderDto> GetByIdAsync(long id, long currentUserId);
        Task<OrderStatusDto> GetByTrackingNumberAsync(string trackingNumber);
        Task<IEnumerable<OrderDto>> GetByOwnerAsync(long ownerId, long currentUserId);
        Task<OrderStatusDto> ChangeStatusAsync(long orderId, OrderStatus newStatus, string? remarks, long currentUserId);


    }
}