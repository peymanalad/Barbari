using BarcopoloWebApi.DTOs.OrderEvent;

namespace BarcopoloWebApi.Services.OrderEvent
{
    public interface IOrderEventService
    {
        Task<OrderEventDto> CreateAsync(CreateOrderEventDto dto, long currentUserId);

        Task<IEnumerable<OrderEventDto>> GetByOrderIdAsync(long orderId, long currentUserId);
    }
}