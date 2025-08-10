using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Withdrawal;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Services.WalletManagement
{
    public class WithdrawalRequestService : IWithdrawalRequestService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<WithdrawalRequestService> _logger;

        public WithdrawalRequestService(DataBaseContext context, ILogger<WithdrawalRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WithdrawalRequestDto> CreateAsync(CreateWithdrawalRequestDto dto, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId)
                       ?? throw new NotFoundException("کاربر یافت نشد");

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == dto.WalletId)
                         ?? throw new NotFoundException("کیف پول یافت نشد");

            if (!await HasPermissionToCreateRequest(currentUserId, wallet))
                throw new ForbiddenAccessException("شما مجاز به ایجاد درخواست برداشت برای این کیف پول نیستید.");

            if (wallet.Balance < dto.Amount)
                throw new InvalidOperationException("موجودی کافی نیست.");

            var request = new WithdrawalRequest
            {
                SourceWalletId = dto.WalletId,
                Amount = dto.Amount,
                RequesterPersonId = currentUserId,
                Status = WithdrawalRequestStatus.Pending,
                DestinationBankAccount = dto.DestinationBankAccount,
                RequestedAt = DateTime.UtcNow
            };

            _context.WithdrawalRequests.Add(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("درخواست برداشت ثبت شد. شناسه: {RequestId}", request.Id);

            return new WithdrawalRequestDto
            {
                Id = request.Id,
                Amount = request.Amount,
                Status = request.Status.ToString(),
                RequestedAt = request.RequestedAt
            };
        }

        public async Task<WithdrawalRequestDto> ReviewAsync(long requestId, WithdrawalReviewDto dto, long currentUserId)
        {
            var admin = await _context.Persons.FindAsync(currentUserId)
                        ?? throw new NotFoundException("کاربر یافت نشد");

            if (!admin.IsAdminOrSuperAdmin())
                throw new ForbiddenAccessException("فقط ادمین یا سوپرادمین می‌تواند درخواست را تأیید یا رد کند.");

            var request = await _context.WithdrawalRequests
                .Include(r => r.SourceWallet)
                .FirstOrDefaultAsync(r => r.Id == requestId)
                ?? throw new NotFoundException("درخواست برداشت یافت نشد");

            if (request.Status != WithdrawalRequestStatus.Pending)
                throw new InvalidOperationException("درخواست قبلاً بررسی شده است.");

            request.Status = dto.Approved ? WithdrawalRequestStatus.Approved : WithdrawalRequestStatus.Rejected;
            request.ReviewedByAdminId = currentUserId;
            request.ReviewedAt = DateTime.UtcNow;

            if (dto.Approved)
            {
                if (request.SourceWallet.Balance < request.Amount)
                    throw new InvalidOperationException("موجودی کافی برای برداشت وجود ندارد.");

                var before = request.SourceWallet.Balance;
                request.SourceWallet.Balance -= request.Amount;

                var tx = new WalletTransaction
                {
                    WalletId = request.SourceWallet.Id,
                    Amount = -request.Amount,
                    TransactionType = TransactionType.Withdrawal,
                    Description = "برداشت از کیف پول",
                    PerformedByPersonId = currentUserId,
                    PerformedAt = DateTime.UtcNow,
                    BalanceBefore = before,
                    BalanceAfter = request.SourceWallet.Balance
                };

                _context.WalletTransactions.Add(tx);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("درخواست برداشت با موفقیت بررسی شد. وضعیت: {Status}", request.Status);

            return new WithdrawalRequestDto
            {
                Id = request.Id,
                Amount = request.Amount,
                Status = request.Status.ToString(),
                ReviewedAt = request.ReviewedAt,
                ReviewedBy = $"{admin.FirstName} {admin.LastName}"
            };
        }

        public async Task<IEnumerable<WithdrawalRequestDto>> GetRequestsAsync(long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId)
                       ?? throw new NotFoundException("کاربر یافت نشد");

            if (user.IsAdminOrSuperAdmin())
            {
                return await _context.WithdrawalRequests
                    .Include(r => r.RequesterPerson)
                    .Include(r => r.SourceWallet)
                    .Select(r => new WithdrawalRequestDto
                    {
                        Id = r.Id,
                        Amount = r.Amount,
                        Status = r.Status.ToString(),
                        RequestedAt = r.RequestedAt,
                        ReviewedAt = r.ReviewedAt,
                        ReviewedBy = r.ReviewedByAdmin != null ? $"{r.ReviewedByAdmin.FirstName} {r.ReviewedByAdmin.LastName}" : null
                    })
                    .ToListAsync();
            }

            var accessibleWallets = await GetAccessibleWalletIdsForUser(currentUserId);

            return await _context.WithdrawalRequests
                .Where(r => accessibleWallets.Contains(r.SourceWalletId))
                .Include(r => r.RequesterPerson)
                .Select(r => new WithdrawalRequestDto
                {
                    Id = r.Id,
                    Amount = r.Amount,
                    Status = r.Status.ToString(),
                    RequestedAt = r.RequestedAt,
                    ReviewedAt = r.ReviewedAt,
                    ReviewedBy = r.ReviewedByAdmin != null ? $"{r.ReviewedByAdmin.FirstName} {r.ReviewedByAdmin.LastName}" : null
                })
                .ToListAsync();
        }

        private async Task<bool> HasPermissionToCreateRequest(long userId, Wallet wallet)
        {
            var user = await _context.Persons.FindAsync(userId);
            if (user == null) return false;

            if (wallet.OwnerType == WalletOwnerType.Person)
                return wallet.OwnerId == user.Id;

            if (wallet.OwnerType == WalletOwnerType.Organization)
            {
                return await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == wallet.OwnerId && m.PersonId == userId && m.Role == SystemRole.orgadmin);
            }

            if (wallet.OwnerType == WalletOwnerType.Branch)
            {
                return await _context.OrganizationMemberships.AnyAsync(m =>
                    m.BranchId == wallet.OwnerId && m.PersonId == userId &&
                    (m.Role == SystemRole.branchadmin || m.Role == SystemRole.orgadmin));
            }

            return false;
        }

        private async Task<List<long>> GetAccessibleWalletIdsForUser(long userId)
        {
            var walletIds = new List<long>();

            var person = await _context.Persons.FindAsync(userId);
            if (person == null) return walletIds;

            if (person.PersonalWalletId.HasValue)
                walletIds.Add(person.PersonalWalletId.Value);

            var memberships = await _context.OrganizationMemberships
                .Where(m => m.PersonId == userId)
                .Include(m => m.Organization)
                .Include(m => m.Branch)
                .ToListAsync();

            foreach (var m in memberships)
            {
                if (m.Organization?.OrganizationWalletId != null)
                    walletIds.Add(m.Organization.OrganizationWalletId.Value);
                if (m.Branch?.BranchWalletId != null)
                    walletIds.Add(m.Branch.BranchWalletId.Value);
            }

            return walletIds;
        }
    }
}
