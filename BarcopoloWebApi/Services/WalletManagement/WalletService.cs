using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.DTOs.Wallet;

namespace BarcopoloWebApi.Services.WalletManagement
{
    public class WalletService : IWalletService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<WalletService> _logger;

        public WalletService(DataBaseContext context, ILogger<WalletService> logger)
        {
            _context = context;
            _logger = logger;
        }


        public async Task<WalletDto> GetWalletDetailsAsync(long currentUserId, bool organizationMode)
        {
            var user = await _context.Persons
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == currentUserId)
                ?? throw new NotFoundException("کاربر یافت نشد.");
            Wallet wallet;
            string walletOwnerType = "Person";
            long walletOwnerId = user.Id;

            if (organizationMode)
            {
                var membership = user.Memberships.FirstOrDefault()
                    ?? throw new ForbiddenAccessException("عضو هیچ سازمانی نیستید.");

                if (membership.BranchId.HasValue)
                {
                    var branch = await _context.SubOrganizations
                        .Include(b => b.BranchWallet)
                        .FirstOrDefaultAsync(b => b.Id == membership.BranchId.Value)
                        ?? throw new NotFoundException("شعبه یافت نشد.");

                    wallet = branch.BranchWallet
                        ?? throw new NotFoundException("کیف پول شعبه یافت نشد.");

                    walletOwnerType = "SubOrganization";
                    walletOwnerId = branch.Id;
                }
                else
                {
                    var organization = await _context.Organizations
                        .Include(o => o.OrganizationWallet)
                        .FirstOrDefaultAsync(o => o.Id == membership.OrganizationId)
                        ?? throw new NotFoundException("سازمان یافت نشد.");

                    wallet = organization.OrganizationWallet
                        ?? throw new NotFoundException("کیف پول سازمان یافت نشد.");

                    walletOwnerType = "Organization";
                    walletOwnerId = organization.Id;
                }
            }
            else
            {
                wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.OwnerType == WalletOwnerType.Person && w.OwnerId == user.Id)
                    ?? throw new NotFoundException("کیف پول شخصی یافت نشد.");
            }

            return new WalletDto
            {
                WalletId = wallet.Id,
                Balance = wallet.Balance,
                OwnerType = walletOwnerType,
                OwnerId = walletOwnerId
            };
        }

        public async Task ChargeWalletAsync(ChargeWalletDto dto, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId)
                       ?? throw new NotFoundException("کاربر یافت نشد.");

            var wallet = await _context.Wallets
                .Include(w => w.OwnerOrganization)
                .Include(w => w.OwnerBranch)
                .Include(w => w.OwnerPerson)
                .FirstOrDefaultAsync(w => w.Id == dto.WalletId)
                ?? throw new NotFoundException("کیف پول یافت نشد.");

            if (!await CanChargeWallet(wallet, user))
                throw new ForbiddenAccessException("شما مجاز به شارژ این کیف پول نیستید.");

            if (dto.Amount <= 0)
                throw new InvalidOperationException("مبلغ شارژ باید بیشتر از صفر باشد.");

            var before = wallet.Balance;
            wallet.Balance += dto.Amount;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = dto.Amount,
                TransactionType = TransactionType.Deposit,
                Description = dto.Description ?? "شارژ کیف پول",
                PerformedByPersonId = currentUserId,
                PerformedAt = DateTime.UtcNow,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("کیف پول {WalletId} با مبلغ {Amount} توسط کاربر {UserId} شارژ شد.", wallet.Id, dto.Amount, currentUserId);
        }
        public async Task<bool> HasSufficientBalanceAsync(long walletId, decimal amount, long currentUserId)
        {
            var wallet = await _context.Wallets
                             .Include(w => w.OwnerOrganization)
                             .Include(w => w.OwnerBranch)
                             .Include(w => w.OwnerPerson)
                             .FirstOrDefaultAsync(w => w.Id == walletId)
                         ?? throw new NotFoundException("کیف پول یافت نشد.");

            if (!await HasAccessToWallet(wallet, currentUserId))
                throw new ForbiddenAccessException("شما مجاز به بررسی این کیف پول نیستید.");

            return wallet.Balance >= amount;
        }

        public async Task PayWithWalletAsync(long walletId, decimal amount, long orderId, long currentUserId)
        {
            var wallet = await _context.Wallets
                .Include(w => w.OwnerOrganization)
                .Include(w => w.OwnerBranch)
                .Include(w => w.OwnerPerson)
                .FirstOrDefaultAsync(w => w.Id == walletId)
                ?? throw new NotFoundException("کیف پول یافت نشد.");

            if (!await HasAccessToWallet(wallet, currentUserId))
                throw new ForbiddenAccessException("شما مجاز به استفاده از این کیف پول نیستید.");

            if (wallet.Balance < amount)
                throw new InvalidOperationException("موجودی کیف پول کافی نیست.");

            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("سفارش یافت نشد.");

            if (wallet.OwnerType == WalletOwnerType.Organization)
            {
                if (order.OrganizationId != wallet.OwnerId)
                    throw new ForbiddenAccessException("این سفارش متعلق به این سازمان نیست.");
            }
            else if (wallet.OwnerType == WalletOwnerType.Branch)
            {
                if (order.BranchId != wallet.OwnerId)
                    throw new ForbiddenAccessException("این سفارش متعلق به این شعبه نیست.");
            }
            else if (wallet.OwnerType == WalletOwnerType.Person)
            {
                if (order.OwnerId != wallet.OwnerId)
                    throw new ForbiddenAccessException("این سفارش متعلق به این کاربر نیست.");
            }

            var balanceBefore = wallet.Balance;
            wallet.Balance -= amount;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = -amount,
                TransactionType = TransactionType.Payment,
                Description = $"پرداخت برای سفارش شماره {orderId}",
                PerformedByPersonId = currentUserId,
                PerformedAt = DateTime.UtcNow,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("پرداخت با کیف پول با موفقیت انجام شد. WalletId={WalletId}, OrderId={OrderId}, Amount={Amount}",
                wallet.Id, order.Id, amount);
        }

        public async Task<WalletAccessLevel> GetWalletAccessLevelAsync(long walletId, long currentUserId)
        {
            var user = await _context.Persons
                           .Include(p => p.Memberships)
                           .FirstOrDefaultAsync(p => p.Id == currentUserId)
                       ?? throw new NotFoundException("کاربر یافت نشد.");

            var wallet = await _context.Wallets
                             .FirstOrDefaultAsync(w => w.Id == walletId)
                         ?? throw new NotFoundException("کیف پول یافت نشد.");

            if (wallet.OwnerType == WalletOwnerType.Person)
            {
                return wallet.OwnerId == currentUserId
                    ? WalletAccessLevel.Manager
                    : WalletAccessLevel.None;
            }

            // کیف پول سازمانی
            if (wallet.OwnerType == WalletOwnerType.Organization)
            {
                var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId == wallet.OwnerId);
                if (membership == null) return WalletAccessLevel.None;

                return membership.Role switch
                {
                    SystemRole.orgadmin => WalletAccessLevel.Manager,
                    SystemRole.branchadmin or SystemRole.user => WalletAccessLevel.Payer,
                    _ => WalletAccessLevel.None
                };
            }

            if (wallet.OwnerType == WalletOwnerType.Branch)
            {
                var membership = user.Memberships.FirstOrDefault(m => m.BranchId == wallet.OwnerId);
                if (membership == null) return WalletAccessLevel.None;

                return membership.Role switch
                {
                    SystemRole.branchadmin => WalletAccessLevel.Manager,
                    SystemRole.user => WalletAccessLevel.Payer,
                    SystemRole.orgadmin => WalletAccessLevel.Manager, 
                    _ => WalletAccessLevel.None
                };
            }

            return WalletAccessLevel.None;
        }


        public async Task<IEnumerable<WalletTransactionDto>> GetTransactionsAsync(long walletId, TransactionFilterDto filter, long currentUserId)
        {
            var wallet = await _context.Wallets
                             .Include(w => w.OwnerOrganization)
                             .Include(w => w.OwnerBranch)
                             .Include(w => w.OwnerPerson)
                             .FirstOrDefaultAsync(w => w.Id == walletId)
                         ?? throw new NotFoundException("کیف پول یافت نشد");

            if (!await HasAccessToWallet(wallet, currentUserId))
                throw new ForbiddenAccessException("شما مجاز به مشاهده این کیف پول نیستید.");

            var query = _context.WalletTransactions
                .Include(t => t.PerformedByPerson)
                .Where(t => t.WalletId == walletId)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(t => t.PerformedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(t => t.PerformedAt <= filter.EndDate.Value);

            if (filter.TransactionType.HasValue)
                query = query.Where(t => t.TransactionType == filter.TransactionType.Value);

            if (filter.MinAmount.HasValue)
                query = query.Where(t => t.Amount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(t => t.Amount <= filter.MaxAmount.Value);

            query = query.OrderByDescending(t => t.PerformedAt);

            var result = await query
                .Include(t => t.PerformedByPerson)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    WalletId = t.WalletId,
                    TransactionType = t.TransactionType.ToString(),
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    Description = t.Description,
                    PerformedAt = t.PerformedAt,
                    PerformedByPersonId = t.PerformedByPersonId,
                    PerformedByFullName = t.PerformedByPerson != null
                        ? $"{t.PerformedByPerson.FirstName} {t.PerformedByPerson.LastName}"
                        : null
                })
                .ToListAsync();


            return result;
        }


        public async Task CreateWalletForPersonAsync(long personId)
        {
            var person = await _context.Persons.FindAsync(personId) ?? throw new NotFoundException("کاربر یافت نشد.");

            if (person.PersonalWalletId.HasValue)
                return;

            var wallet = new Wallet
            {
                OwnerType = WalletOwnerType.Person,
                OwnerId = personId,
                Balance = 0
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            person.PersonalWalletId = wallet.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("کیف پول شخصی برای کاربر {UserId} ایجاد شد", personId);
        }

        public async Task CreateWalletForOrganizationAsync(long organizationId)
        {
            var organization = await _context.Organizations.FindAsync(organizationId)
                ?? throw new NotFoundException("سازمان یافت نشد.");

            if (organization.OrganizationWalletId.HasValue)
                return;

            var wallet = new Wallet
            {
                OwnerType = WalletOwnerType.Organization,
                OwnerId = organizationId,
                Balance = 0
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            organization.OrganizationWalletId = wallet.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("کیف پول سازمانی برای سازمان {OrgId} ایجاد شد", organizationId);
        }

        public async Task CreateWalletForBranchAsync(long branchId)
        {
            var branch = await _context.SubOrganizations.FindAsync(branchId)
                ?? throw new NotFoundException("شعبه یافت نشد.");

            if (branch.BranchWalletId.HasValue)
                return;

            var wallet = new Wallet
            {
                OwnerType = WalletOwnerType.Branch,
                OwnerId = branchId,
                Balance = 0
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            branch.BranchWalletId = wallet.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("کیف پول برای شعبه {BranchId} ایجاد شد", branchId);
        }


        private async Task<bool> CanChargeWallet(Wallet wallet, Entities.Person user)
        {
            if (wallet.OwnerType == WalletOwnerType.Person)
                return wallet.OwnerId == user.Id;

            if (wallet.OwnerType == WalletOwnerType.Organization)
            {
                return await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == wallet.OwnerId &&
                    m.PersonId == user.Id &&
                    m.Role == SystemRole.orgadmin);
            }

            if (wallet.OwnerType == WalletOwnerType.Branch)
            {
                return await _context.OrganizationMemberships.AnyAsync(m =>
                    m.BranchId == wallet.OwnerId &&
                    m.PersonId == user.Id &&
                    (m.Role == SystemRole.branchadmin || m.Role == SystemRole.orgadmin));
            }

            return false;
        }
        private async Task<bool> HasAccessToWallet(Wallet wallet, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null) return false;

            if (wallet.OwnerType == WalletOwnerType.Person)
                return wallet.OwnerId == currentUserId;

            if (wallet.OwnerType == WalletOwnerType.Organization)
            {
                return await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == wallet.OwnerId && m.PersonId == currentUserId);
            }

            if (wallet.OwnerType == WalletOwnerType.Branch)
            {
                return await _context.OrganizationMemberships.AnyAsync(m =>
                    m.BranchId == wallet.OwnerId && m.PersonId == currentUserId);
            }

            return false;
        }


    }
}