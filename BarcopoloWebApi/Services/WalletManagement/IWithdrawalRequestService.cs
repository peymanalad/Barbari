using BarcopoloWebApi.DTOs.Withdrawal;

namespace BarcopoloWebApi.Services.WalletManagement

{
    public interface IWithdrawalRequestService
    {
        Task<WithdrawalRequestDto> CreateAsync(CreateWithdrawalRequestDto dto, long currentUserId);
        Task<WithdrawalRequestDto> ReviewAsync(long requestId, WithdrawalReviewDto dto, long currentUserId);
        Task<IEnumerable<WithdrawalRequestDto>> GetRequestsAsync(long currentUserId);
    }
}