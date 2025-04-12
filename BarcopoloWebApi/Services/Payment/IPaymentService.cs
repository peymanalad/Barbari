using BarcopoloWebApi.DTOs.Payment;

namespace BarcopoloWebApi.Services
{
    public interface IPaymentService
    {
        Task<PaymentDto> CreateAsync(CreatePaymentDto dto, long currentUserId);
        Task<PaymentDto> UpdateAsync(long id, UpdatePaymentDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);
        Task<PaymentDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<PaymentDto>> GetByOrderIdAsync(long orderId, long currentUserId);
        Task<decimal> GetRemainingAmountAsync(long orderId, long currentUserId);
    }
}