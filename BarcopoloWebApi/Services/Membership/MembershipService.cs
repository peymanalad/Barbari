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

            if (dto.PersonId == currentUserId)
                throw new ForbiddenAccessException("شما نمی‌توانید خودتان را اضافه کنید");

            var currentUser = await _context.Persons.FindAsync(currentUserId)
                ?? throw new ForbiddenAccessException("کاربر جاری یافت نشد");

            var targetPerson = await _context.Persons.FindAsync(dto.PersonId)
                ?? throw new NotFoundException("کاربر مقصد یافت نشد");

            // بررسی وجود عضویت قبلی
            var existing = await _context.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.OrganizationId == dto.OrganizationId && m.PersonId == dto.PersonId);
            if (existing != null)
                throw new AppException("این کاربر قبلاً عضو این سازمان شده است");

            // اعتبارسنجی نقش هدف
            var normalizedRole = dto.Role.Trim().ToLower();
            if (!Enum.TryParse<SystemRole>(normalizedRole, true, out var targetRoleEnum))
                throw new AppException("نقش وارد شده نامعتبر است");

            if (targetRoleEnum == SystemRole.superadmin)
                throw new ForbiddenAccessException("اجازه تعریف کاربر با نقش superadmin وجود ندارد");

            var isSuperadmin = currentUser.IsSuperAdmin();
            var isAdmin = currentUser.IsAdmin();

            // اگر admin هست:
            if (isAdmin)
            {
                if (targetRoleEnum == SystemRole.superadmin)
                    throw new ForbiddenAccessException("admin نمی‌تواند superadmin تعریف کند");
            }

            // اگر orgadmin هست:
            if (!isSuperadmin && !isAdmin)
            {
                var userMembership = await _context.OrganizationMemberships
                    .FirstOrDefaultAsync(m => m.PersonId == currentUserId);

                if (userMembership == null)
                    throw new ForbiddenAccessException("شما در هیچ سازمانی عضو نیستید");

                var isOrgAdmin = userMembership.OrganizationId == dto.OrganizationId && userMembership.Role == SystemRole.orgadmin;
                var isBranchAdmin = userMembership.BranchId == dto.BranchId && userMembership.Role == SystemRole.branchadmin;

                if (!isOrgAdmin && !isBranchAdmin)
                    throw new ForbiddenAccessException("شما اجازه افزودن عضو را ندارید");

                if (isOrgAdmin)
                {
                    if (userMembership.OrganizationId != dto.OrganizationId)
                        throw new ForbiddenAccessException("orgadmin فقط می‌تواند به سازمان خودش عضو اضافه کند");

                    if (targetRoleEnum is SystemRole.admin or SystemRole.superadmin)
                        throw new ForbiddenAccessException("orgadmin نمی‌تواند نقش admin یا superadmin ایجاد کند");
                }

                if (isBranchAdmin)
                {
                    if (userMembership.OrganizationId != dto.OrganizationId)
                        throw new ForbiddenAccessException("branchadmin فقط به سازمان خودش دسترسی دارد");

                    if (userMembership.BranchId != dto.BranchId)
                        throw new ForbiddenAccessException("branchadmin فقط به شعبه خودش دسترسی دارد");

                    if (targetRoleEnum is SystemRole.admin or SystemRole.superadmin or SystemRole.orgadmin)
                        throw new ForbiddenAccessException("branchadmin فقط می‌تواند نقش user یا branchadmin ایجاد کند");
                }
            }

            // اعتبارسنجی branch اگر وجود دارد
            if (dto.BranchId.HasValue)
            {
                var branch = await _context.SubOrganizations.FindAsync(dto.BranchId.Value);
                if (branch == null || branch.OrganizationId != dto.OrganizationId)
                    throw new AppException("شعبه وارد شده با سازمان مطابقت ندارد");
            }

            // ایجاد عضویت
            var membership = new OrganizationMembership
            {
                PersonId = dto.PersonId,
                OrganizationId = dto.OrganizationId,
                BranchId = dto.BranchId,
                Role = targetRoleEnum,
                JoinedAt = DateTime.UtcNow
            };

            await _context.OrganizationMemberships.AddAsync(membership);
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

            var currentUser = await _context.Persons.FindAsync(currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد");

            var isSuperAdmin = currentUser.IsSuperAdmin();
            var isAdmin = currentUser.IsAdmin();

            var currentMembership = await _context.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.PersonId == currentUserId);

            var isOrgAdmin = currentMembership != null &&
                currentMembership.OrganizationId == membership.OrganizationId &&
                currentMembership.Role == SystemRole.orgadmin;

            var isBranchAdmin = currentMembership != null &&
                currentMembership.BranchId == membership.BranchId &&
                currentMembership.Role == SystemRole.branchadmin;

            if (!(isSuperAdmin || isAdmin || isOrgAdmin || isBranchAdmin))
                throw new ForbiddenAccessException("شما اجازه ویرایش این عضویت را ندارید");

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var normalizedRole = dto.Role.Trim().ToLower();

                if (!Enum.TryParse<SystemRole>(normalizedRole, true, out var roleEnum))
                    throw new AppException("نقش وارد شده معتبر نیست");

                if (roleEnum == SystemRole.superadmin)
                    throw new ForbiddenAccessException("تغییر به نقش superadmin مجاز نیست");

                if (isOrgAdmin && roleEnum is SystemRole.admin or SystemRole.superadmin)
                    throw new ForbiddenAccessException("orgadmin نمی‌تواند نقش admin یا superadmin بدهد");

                if (isBranchAdmin && roleEnum is SystemRole.admin or SystemRole.orgadmin or SystemRole.superadmin)
                    throw new ForbiddenAccessException("branchadmin فقط می‌تواند نقش user یا branchadmin بدهد");

                membership.Role = roleEnum;
            }

            if (dto.BranchId.HasValue)
            {
                if (isBranchAdmin)
                    throw new ForbiddenAccessException("branchadmin اجازه تغییر شعبه را ندارد");

                var branch = await _context.SubOrganizations.FirstOrDefaultAsync(b => b.Id == dto.BranchId.Value);
                if (branch == null)
                    throw new NotFoundException("شعبه مورد نظر یافت نشد");

                if (branch.OrganizationId != membership.OrganizationId)
                    throw new AppException("شعبه انتخاب‌شده متعلق به سازمان این عضویت نیست");

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

            return MapToDto(membership);
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

            var currentUser = await _context.Persons.FindAsync(currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد");

            var isSuperAdmin = currentUser.IsSuperAdmin();
            var isAdmin = currentUser.IsAdmin();

            var currentMembership = await _context.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.PersonId == currentUserId);

            var isOrgAdmin = currentMembership != null &&
                currentMembership.OrganizationId == membership.OrganizationId &&
                currentMembership.Role == SystemRole.orgadmin;

            var isBranchAdmin = currentMembership != null &&
                currentMembership.BranchId == membership.BranchId &&
                currentMembership.Role == SystemRole.branchadmin;

            if (!(isSuperAdmin || isAdmin || isOrgAdmin || isBranchAdmin))
                throw new ForbiddenAccessException("شما اجازه حذف این عضویت را ندارید");

            if (membership.Role == SystemRole.superadmin)
                throw new ForbiddenAccessException("حذف نقش superadmin مجاز نیست");

            if (isOrgAdmin && membership.Role is SystemRole.admin or SystemRole.superadmin)
                throw new ForbiddenAccessException("orgadmin نمی‌تواند نقش admin یا superadmin را حذف کند");

            if (isBranchAdmin)
            {
                if (membership.BranchId != currentMembership.BranchId)
                    throw new ForbiddenAccessException("branchadmin فقط به شعبه خودش دسترسی دارد");

                if (membership.Role is SystemRole.admin or SystemRole.orgadmin or SystemRole.superadmin)
                    throw new ForbiddenAccessException("branchadmin نمی‌تواند عضو با نقش‌های بالاتر را حذف کند");
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

            if (membership.PersonId == currentUserId)
                return MapToDto(membership);

            var currentUser = await _context.Persons.FindAsync(currentUserId)
                              ?? throw new NotFoundException("کاربر جاری یافت نشد.");

            if (currentUser.IsSuperAdmin() || currentUser.IsAdmin())
                return MapToDto(membership);

            var currentMembership = await _context.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.PersonId == currentUserId);

            if (currentMembership != null)
            {
                var isOrgAdmin = currentMembership.Role == SystemRole.orgadmin &&
                                 currentMembership.OrganizationId == membership.OrganizationId;

                var isBranchAdmin = currentMembership.Role == SystemRole.branchadmin &&
                                    membership.BranchId.HasValue &&
                                    currentMembership.BranchId == membership.BranchId;

                if (isOrgAdmin || isBranchAdmin)
                    return MapToDto(membership);
            }

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
                // مشاهده کل عضویت‌ها
                query = _context.OrganizationMemberships
                    .Include(m => m.Person)
                    .Include(m => m.Organization)
                    .Include(m => m.Branch);
            }
            else
            {
                var currentMembership = await _context.OrganizationMemberships
                    .FirstOrDefaultAsync(m => m.PersonId == currentUserId);

                if (currentMembership == null)
                    throw new ForbiddenAccessException("شما در هیچ سازمان یا شعبه‌ای عضو نیستید.");

                if (currentMembership.Role == SystemRole.orgadmin)
                {
                    query = _context.OrganizationMemberships
                        .Include(m => m.Person)
                        .Include(m => m.Organization)
                        .Include(m => m.Branch)
                        .Where(m => m.OrganizationId == currentMembership.OrganizationId);
                }
                else if (currentMembership.Role == SystemRole.branchadmin)
                {
                    if (!currentMembership.BranchId.HasValue)
                        throw new ForbiddenAccessException("branchadmin باید دارای شعبه باشد");

                    query = _context.OrganizationMemberships
                        .Include(m => m.Person)
                        .Include(m => m.Organization)
                        .Include(m => m.Branch)
                        .Where(m => m.BranchId == currentMembership.BranchId);
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
            var currentUser = await _context.Persons.FindAsync(currentUserId)
                ?? throw new NotFoundException("کاربر جاری یافت نشد.");

            if (currentUser.IsSuperAdmin() || currentUser.IsAdmin())
            {
                // دسترسی کامل: تمام اعضای سازمان + برنچ‌ها
                var all = await _context.OrganizationMemberships
                    .Include(m => m.Person)
                    .Include(m => m.Organization)
                    .Include(m => m.Branch)
                    .Where(m => m.OrganizationId == organizationId)
                    .ToListAsync();

                return all.Select(MapToDto).ToList();
            }

            var currentMembership = await _context.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.PersonId == currentUserId);

            if (currentMembership == null)
                throw new ForbiddenAccessException("شما در هیچ سازمان یا شعبه‌ای عضو نیستید.");

            if (currentMembership.Role == SystemRole.orgadmin && currentMembership.OrganizationId == organizationId)
            {
                var orgMembers = await _context.OrganizationMemberships
                    .Include(m => m.Person)
                    .Include(m => m.Organization)
                    .Include(m => m.Branch)
                    .Where(m => m.OrganizationId == organizationId)
                    .ToListAsync();

                return orgMembers.Select(MapToDto).ToList();
            }

            if (currentMembership.Role == SystemRole.branchadmin)
            {
                if (currentMembership.OrganizationId != organizationId)
                    throw new ForbiddenAccessException("شما فقط به اعضای شعبه خودتان در سازمان خودتان دسترسی دارید");

                var branchId = currentMembership.BranchId;
                if (branchId == null)
                    throw new ForbiddenAccessException("branchadmin باید شعبه مشخصی داشته باشد");

                var branchMembers = await _context.OrganizationMemberships
                    .Include(m => m.Person)
                    .Include(m => m.Organization)
                    .Include(m => m.Branch)
                    .Where(m => m.OrganizationId == organizationId && m.BranchId == branchId)
                    .ToListAsync();

                return branchMembers.Select(MapToDto).ToList();
            }


            throw new ForbiddenAccessException("شما اجازه مشاهده اعضای این سازمان را ندارید.");
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
