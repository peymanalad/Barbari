using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarcopoloWebApi.DTOs.Wallet;

namespace BarcopoloWebApi.Services.WalletManagement
{
    public interface IWalletService
    {
        Task<WalletDto> GetWalletDetailsAsync(long currentUserId, bool organizationMode);
        Task ChargeWalletAsync(ChargeWalletDto dto, long currentUserId);
        Task<bool> HasSufficientBalanceAsync(long walletId, decimal amount, long currentUserId);
        Task PayWithWalletAsync(long walletId, decimal amount, long orderId, long currentUserId);
        Task<WalletAccessLevel> GetWalletAccessLevelAsync(long walletId, long currentUserId);

        Task<IEnumerable<WalletTransactionDto>> GetTransactionsAsync(long walletId,TransactionFilterDto filter,long currentUserId);


        Task CreateWalletForPersonAsync(long personId);
        Task CreateWalletForOrganizationAsync(long organizationId);
        Task CreateWalletForBranchAsync(long branchId);

    }
}