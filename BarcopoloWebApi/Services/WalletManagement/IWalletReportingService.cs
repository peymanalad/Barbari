using BarcopoloWebApi.DTOs.Wallet;

public interface IWalletReportingService
{
    Task<WalletStatisticsDto> GetWalletStatisticsAsync(long walletId, long currentUserId);
}