using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Wallet;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BarcopoloWebApi.Services.WalletManagement
{
    public class WalletReportingService : IWalletReportingService
    {
        private readonly DataBaseContext _context;

        public WalletReportingService(DataBaseContext context)
        {
            _context = context;
        }

        public async Task<WalletStatisticsDto> GetWalletStatisticsAsync(long walletId, long currentUserId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.Id == walletId)
                ?? throw new NotFoundException("کیف پول یافت نشد.");

            if (!await HasAccessToWallet(wallet, currentUserId))
                throw new ForbiddenAccessException("شما مجاز به مشاهده این کیف پول نیستید.");

            var transactions = await _context.WalletTransactions
                .Where(t => t.WalletId == walletId)
                .ToListAsync();

            return new WalletStatisticsDto
            {
                WalletId = wallet.Id,
                Balance = wallet.Balance,
                TotalTransactionCount = transactions.Count,
                TotalDeposits = transactions
                    .Where(t => t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                TotalWithdrawals = transactions
                    .Where(t => t.TransactionType == TransactionType.Withdrawal)
                    .Sum(t => Math.Abs(t.Amount)),
                TotalPayments = transactions
                    .Where(t => t.TransactionType == TransactionType.Payment)
                    .Sum(t => Math.Abs(t.Amount)),
                FirstTransactionDate = transactions
                    .OrderBy(t => t.PerformedAt)
                    .FirstOrDefault()?.PerformedAt,
                LastTransactionDate = transactions
                    .OrderByDescending(t => t.PerformedAt)
                    .FirstOrDefault()?.PerformedAt
            };
        }

        private async Task<bool> HasAccessToWallet(Wallet wallet, long userId)
        {
            var user = await _context.Persons
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == userId);

            if (user == null) return false;

            if (wallet.OwnerType == WalletOwnerType.Person)
                return wallet.OwnerId == userId;

            if (wallet.OwnerType == WalletOwnerType.Organization)
                return user.Memberships.Any(m => m.OrganizationId == wallet.OwnerId);

            if (wallet.OwnerType == WalletOwnerType.Branch)
                return user.Memberships.Any(m => m.BranchId == wallet.OwnerId);

            return false;
        }
    }
}
