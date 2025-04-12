using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Membership;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<MembershipService> _logger;

        public MembershipService(DataBaseContext context, ILogger<MembershipService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MembershipDto> AddAsync(CreateMembershipDto dto, long currentUserId)
        {
            _logger.LogInformation("User {UserId} is attempting to add person {PersonId} to organization {OrgId} with role {Role}",
                currentUserId, dto.PersonId, dto.OrganizationId, dto.Role);

            var currentUser = await _context.Persons.FindAsync(currentUserId)
                ?? throw new ForbiddenAccessException("کاربر جاری یافت نشد");

            var targetPerson = await _context.Persons.FindAsync(dto.PersonId)
                ?? throw new NotFoundException("کاربری با این شناسه یافت نشد");

            if (dto.PersonId == currentUserId)
                throw new ForbiddenAccessException("شما نمی‌توانید خودتان را اضافه کنید");

            var existing = await _context.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.OrganizationId == dto.OrganizationId && m.PersonId == dto.PersonId);

            if (existing != null)
                throw new AppException("این کاربر قبلاً عضو این سازمان شده است");

            var normalizedRole = dto.Role.Trim().ToLower();
            if (!Enum.TryParse<SystemRole>(normalizedRole, true, out var targetRoleEnum))
                throw new AppException("نقش وارد شده نامعتبر است");

            var isSuperadmin = currentUser.IsSuperAdmin();
            var isAdmin = currentUser.IsAdmin();
            var isOrgAdmin = await IsOrgAdminAsync(currentUserId, dto.OrganizationId);
            var isBranchAdmin = dto.BranchId.HasValue && await IsBranchAdminAsync(currentUserId, dto.BranchId.Value);

            if (!isSuperadmin && !isAdmin && !isOrgAdmin && !isBranchAdmin)
                throw new ForbiddenAccessException("دسترسی به افزودن عضو ندارید");

            if (isBranchAdmin)
            {
                if (normalizedRole is not ("user" or "branchadmin"))
                    throw new AppException("branchadmin فقط می‌تواند نقش user یا branchadmin ایجاد کند");

                if (!dto.BranchId.HasValue)
                    throw new AppException("branchId الزامی است");

                var branch = await _context.SubOrganizations.FindAsync(dto.BranchId.Value);
                if (branch == null || branch.OrganizationId != dto.OrganizationId)
                    throw new AppException("برنچ وارد شده با سازمان مطابقت ندارد");
            }


            var userMembership = await _context.OrganizationMemberships
                .FirstOrDefaultAsync(m =>
                    m.PersonId == currentUserId &&
                    m.OrganizationId == dto.OrganizationId); 

            var userOrgId = userMembership?.OrganizationId;
            var userBranchId = userMembership?.BranchId;

            if (isOrgAdmin && userOrgId != dto.OrganizationId)
                throw new AppException("orgadmin فقط به سازمان خودش می‌تواند عضو اضافه کند");

            if (isBranchAdmin && userBranchId != dto.BranchId)
                throw new AppException("branchadmin فقط به برنچ خودش می‌تواند عضو اضافه کند");


            var membership = new OrganizationMembership
            {
                PersonId = dto.PersonId,
                OrganizationId = dto.OrganizationId,
                BranchId = dto.BranchId,
                Role = targetRoleEnum,
                JoinedAt = DateTime.UtcNow
            };

            await _context.OrganizationMemberships.AddAsync(membership);

            var addressToInherit = dto.BranchId.HasValue
                ? await _context.SubOrganizations
                    .Where(b => b.Id == dto.BranchId)
                    .Include(b => b.OriginAddress)
                    .Select(b => b.OriginAddress)
                    .FirstOrDefaultAsync()
                : await _context.Organizations
                    .Where(o => o.Id == dto.OrganizationId)
                    .Include(o => o.OriginAddress)
                    .Select(o => o.OriginAddress)
                    .FirstOrDefaultAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("عضویت برای شخص {PersonId} با نقش {Role} در سازمان {OrgId} ایجاد شد", dto.PersonId, dto.Role, dto.OrganizationId);

            return new MembershipDto
            {
                Id = membership.Id,
                PersonId = dto.PersonId,
                OrganizationId = dto.OrganizationId,
                BranchId = dto.BranchId,
                Role = membership.Role.ToString(),
                JoinedAt = membership.JoinedAt,
                OrganizationName = (await _context.Organizations.FindAsync(dto.OrganizationId))?.Name ?? "",
                BranchName = dto.BranchId.HasValue
                    ? (await _context.SubOrganizations.FindAsync(dto.BranchId.Value))?.Name
                    : null,
                PersonFullName = targetPerson.GetFullName()
            };
        }

        public async Task<MembershipDto> UpdateAsync(long membershipId, UpdateMembershipDto dto, long currentUserId)
        {
            _logger.LogInformation("در حال بروزرسانی عضویت با Id {MembershipId} توسط کاربر {UserId}", membershipId, currentUserId);

            var membership = await _context.OrganizationMemberships
                .Include(m => m.Person)
                .Include(m => m.Organization)
                .Include(m => m.Branch)
                .FirstOrDefaultAsync(m => m.Id == membershipId);

            if (membership == null)
                throw new NotFoundException("عضویت مورد نظر یافت نشد");

            if (membership.PersonId == currentUserId)
                throw new ForbiddenAccessException("کاربر نمی‌تواند خودش را ویرایش کند");

            var isSuperAdmin = await IsSuperAdminAsync(currentUserId);
            var isAdmin = await IsAdminAsync(currentUserId);
            var isOrgAdmin = await IsOrgAdminAsync(membership.OrganizationId, currentUserId);
            var isBranchAdmin = membership.BranchId.HasValue && await IsBranchAdminAsync(membership.BranchId.Value, currentUserId);

            if (!(isSuperAdmin || isAdmin || isOrgAdmin || isBranchAdmin))
                throw new ForbiddenAccessException("دسترسی غیرمجاز");

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var normalizedRole = dto.Role.Trim().ToLower();

                if (!Enum.TryParse<SystemRole>(normalizedRole, true, out var roleEnum))
                    throw new AppException("نقش وارد شده معتبر نیست");

                if (isBranchAdmin && (roleEnum == SystemRole.admin || roleEnum == SystemRole.orgadmin))
                    throw new AppException("branchadmin فقط می‌تواند نقش user یا branchadmin اختصاص دهد");

                membership.Role = roleEnum;
            }

            if (dto.BranchId.HasValue)
            {
                if (isBranchAdmin)
                    throw new ForbiddenAccessException("branchadmin نمی‌تواند شعبه را تغییر دهد");

                var branch = await _context.SubOrganizations.FirstOrDefaultAsync(b => b.Id == dto.BranchId.Value);
                if (branch == null)
                    throw new NotFoundException("شعبه مورد نظر یافت نشد");

                if (branch.OrganizationId != membership.OrganizationId)
                    throw new AppException("شعبه متعلق به سازمان عضویت نیست");

                membership.BranchId = branch.Id;
                membership.Branch = branch;
            }
            else if (dto.BranchId == null && !isBranchAdmin)
            {
                membership.BranchId = null;
                membership.Branch = null;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("عضویت با شناسه {MembershipId} با موفقیت بروزرسانی شد", membershipId);

            return new MembershipDto
            {
                Id = membership.Id,
                PersonId = membership.PersonId,
                PersonFullName = membership.Person.GetFullName(),
                OrganizationId = membership.OrganizationId,
                OrganizationName = membership.Organization.Name,
                BranchId = membership.BranchId,
                BranchName = membership.Branch?.Name,
                Role = membership.Role.ToString(),
                JoinedAt = membership.JoinedAt
            };
        }

        public async Task<bool> RemoveAsync(long membershipId, long currentUserId)
        {
            _logger.LogInformation("در حال حذف عضویت با شناسه {MembershipId} توسط کاربر {UserId}", membershipId, currentUserId);

            var membership = await _context.OrganizationMemberships
                .Include(m => m.Organization)
                .Include(m => m.Branch)
                .FirstOrDefaultAsync(m => m.Id == membershipId);

            if (membership == null)
                throw new NotFoundException("عضویت مورد نظر یافت نشد");

            if (membership.PersonId == currentUserId)
                throw new ForbiddenAccessException("کاربر نمی‌تواند خودش را حذف کند");

            var isSuperAdmin = await IsSuperAdminAsync(currentUserId);
            var isAdmin = await IsAdminAsync(currentUserId);
            var isOrgAdmin = await IsOrgAdminAsync(membership.OrganizationId, currentUserId);
            var isBranchAdmin = membership.BranchId.HasValue && await IsBranchAdminAsync(membership.BranchId.Value, currentUserId);

            if (!(isSuperAdmin || isAdmin || isOrgAdmin || isBranchAdmin))
                throw new ForbiddenAccessException("دسترسی غیرمجاز");

            if (isBranchAdmin)
            {
                if (!membership.BranchId.HasValue)
                    throw new ForbiddenAccessException("branchadmin فقط می‌تواند اعضای زیرمجموعه‌ی خودش را حذف کند");

                var currentMembership = await _context.OrganizationMemberships
                    .FirstOrDefaultAsync(m => m.PersonId == currentUserId && m.BranchId.HasValue);

                if (currentMembership == null || currentMembership.BranchId != membership.BranchId)
                    throw new ForbiddenAccessException("branchadmin فقط می‌تواند اعضای زیرمجموعه‌ی خودش را حذف کند");

                if (membership.Role == SystemRole.orgadmin || membership.Role == SystemRole.admin)
                    throw new ForbiddenAccessException("branchadmin نمی‌تواند عضوی با نقش orgadmin یا admin حذف کند");
            }

            _context.OrganizationMemberships.Remove(membership);
            await _context.SaveChangesAsync();

            _logger.LogInformation("عضویت با شناسه {MembershipId} با موفقیت حذف شد", membershipId);
            return true;
        }

        public async Task<MembershipDto> GetByIdAsync(long membershipId, long currentUserId)
        {
            var membership = await _context.OrganizationMemberships
                .Include(m => m.Person)
                .Include(m => m.Organization)
                .Include(m => m.Branch)
                .FirstOrDefaultAsync(m => m.Id == membershipId);

            if (membership == null)
                throw new NotFoundException("عضویت مورد نظر یافت نشد.");

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null)
                throw new NotFoundException("کاربر جاری یافت نشد.");

            if (currentUser.IsSuperAdmin() || currentUser.IsAdmin())
                return MapToDto(membership);

            if (membership.PersonId == currentUserId)
                return MapToDto(membership);

            if (await IsOrgAdminAsync(membership.OrganizationId, currentUserId))
                return MapToDto(membership);

            if (membership.BranchId.HasValue && await IsBranchAdminAsync(membership.BranchId.Value, currentUserId))
                return MapToDto(membership);
            
            throw new ForbiddenAccessException("شما اجازه مشاهده این عضویت را ندارید.");
        }

        public async Task<IEnumerable<MembershipDto>> GetAllForCurrentScopeAsync(long currentUserId)
        {
            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null)
                throw new NotFoundException("کاربر یافت نشد.");

            IQueryable<OrganizationMembership> query;

            if (currentUser.IsSuperAdmin() || currentUser.IsAdmin())
            {
                query = _context.OrganizationMemberships
                    .Include(m => m.Person)
                    .Include(m => m.Organization)
                    .Include(m => m.Branch);
            }
            else
            {
                var membership = await _context.OrganizationMemberships
                    .Include(m => m.Organization)
                    .FirstOrDefaultAsync(m => m.PersonId == currentUserId);

                if (membership == null)
                    throw new ForbiddenAccessException("شما در هیچ سازمانی عضو نیستید.");

                if (membership.Role == SystemRole.orgadmin)
                {
                    query = _context.OrganizationMemberships
                        .Include(m => m.Person)
                        .Include(m => m.Organization)
                        .Include(m => m.Branch)
                        .Where(m => m.OrganizationId == membership.OrganizationId);
                }
                else if (membership.Role == SystemRole.branchadmin)
                {
                    query = _context.OrganizationMemberships
                        .Include(m => m.Person)
                        .Include(m => m.Organization)
                        .Include(m => m.Branch)
                        .Where(m => m.BranchId == membership.BranchId);
                }
                else
                {
                    throw new ForbiddenAccessException("شما اجازه مشاهده اعضای سازمان را ندارید.");
                }
            }

            var memberships = await query.ToListAsync();
            return memberships.Select(MapToDto).ToList();
        }


        public async Task<IEnumerable<MembershipDto>> GetByOrganizationIdAsync(long organizationId, long currentUserId)
        {
            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null)
                throw new NotFoundException("کاربر یافت نشد.");

            if (currentUser.IsSuperAdmin() || currentUser.IsAdmin())
            {
            }
            else
            {
                var isOrgAdmin = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == organizationId &&
                    m.PersonId == currentUserId &&
                    m.Role == SystemRole.orgadmin);

                if (!isOrgAdmin)
                    throw new ForbiddenAccessException("شما اجازه مشاهده اعضای این سازمان را ندارید.");
            }

            var memberships = await _context.OrganizationMemberships
                .Include(m => m.Person)
                .Include(m => m.Organization)
                .Include(m => m.Branch)
                .Where(m => m.OrganizationId == organizationId)
                .ToListAsync();

            return memberships.Select(MapToDto).ToList();
        }



        private MembershipDto MapToDto(OrganizationMembership m)
        {
            return new MembershipDto
            {
                Id = m.Id,
                PersonId = m.PersonId,
                PersonFullName = m.Person?.GetFullName() ?? "",
                OrganizationId = m.OrganizationId,
                OrganizationName = m.Organization?.Name ?? "",
                BranchId = m.BranchId,
                BranchName = m.Branch?.Name,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            };
        }

        private async Task<bool> IsSuperAdminAsync(long userId)
        {
            return await _context.Persons
                .AnyAsync(p => p.Id == userId && p.Role == SystemRole.superadmin);
        }

        private async Task<bool> IsAdminAsync(long userId)
        {
            return await _context.Persons
                .AnyAsync(p => p.Id == userId && p.Role == SystemRole.admin);
        }

        private async Task<bool> IsBranchAdminAsync(long branchId, long userId)
        {
            return await _context.OrganizationMemberships
                .AnyAsync(m => m.BranchId == branchId && m.PersonId == userId && m.Role == SystemRole.branchadmin);
        }

        private async Task<bool> IsOrgAdminAsync(long organizationId, long userId)
        {
            return await _context.OrganizationMemberships
                .AnyAsync(m =>
                    m.OrganizationId == organizationId &&
                    m.PersonId == userId &&
                    m.Role == SystemRole.orgadmin);
        }

    }
}
