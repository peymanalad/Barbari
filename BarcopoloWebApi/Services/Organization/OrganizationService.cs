using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Organization;
using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.Services.Organization
{
    public class OrganizationService : IOrganizationService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(DataBaseContext context, ILogger<OrganizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OrganizationDto> CreateAsync(CreateOrganizationDto dto, long currentUserId)
        {
            await EnsureIsAdminOrSuperAdminAsync(currentUserId);

            var organization = new Entities.Organization
            {
                Name = dto.Name,
                OriginAddress = dto.OriginAddress
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            _logger.LogInformation("سازمان با شناسه {OrgId} توسط کاربر {UserId} ایجاد شد.", organization.Id, currentUserId);

            return await MapToDtoAsync(organization.Id);
        }




        public async Task<OrganizationDto> GetByIdAsync(long id, long currentUserId)
        {
            var org = await _context.Organizations
                .Include(o => o.AllowedCargoTypes)
                .Include(o => o.Branches)
                .Include(o => o.Memberships)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (org == null)
                throw new Exception("سازمان یافت نشد.");

            if (!await IsSuperAdmin(currentUserId) && !org.HasMember(currentUserId))
                throw new Exception("عدم دسترسی");

            return MapToDto(org);
        }

        public async Task<IEnumerable<OrganizationDto>> GetAllAsync(long currentUserId)
        {
            await EnsureIsSuperAdmin(currentUserId);

            var organizations = await _context.Organizations
                .Include(o => o.AllowedCargoTypes)
                .Include(o => o.Branches)
                .ToListAsync();

            return organizations.Select(MapToDto).ToList();
        }

        public async Task<OrganizationDto> UpdateAsync(long id, UpdateOrganizationDto dto, long currentUserId)
        {
            var org = await _context.Organizations
                .Include(o => o.Memberships)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (org == null)
                throw new Exception("سازمان یافت نشد.");

            if (!await IsSuperAdmin(currentUserId) && !await IsOrgAdmin(id, currentUserId))
                throw new UnauthorizedAccessException("شما مجاز به ویرایش این سازمان نیستید.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                org.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.OriginAddress))
                org.OriginAddress = dto.OriginAddress;

            await _context.SaveChangesAsync();

            _logger.LogInformation("سازمان {OrgId} توسط کاربر {UserId} بروزرسانی شد", id, currentUserId);

            return await MapToDtoAsync(id);
        }


        public async Task<bool> DeleteAsync(long id, long currentUserId)
        {
            await EnsureIsSuperAdmin(currentUserId);

            var org = await _context.Organizations
                .Include(o => o.Memberships)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (org == null)
                return false;

            if (org.Memberships.Any())
                _context.OrganizationMemberships.RemoveRange(org.Memberships);

            _context.Organizations.Remove(org);
            await _context.SaveChangesAsync();

            _logger.LogInformation("سازمان {OrgId} توسط ادمین {UserId} حذف شد", id, currentUserId);
            return true;
        }

        #region Private Helpers

        private async Task EnsureIsSuperAdmin(long userId)
        {
            if (!await IsSuperAdmin(userId))
                throw new Exception("شما مجاز به انجام این عملیات نیستید.");
        }

        private async Task EnsureIsAdminOrSuperAdminAsync(long currentUserId)
        {
            var person = await _context.Persons
                             .AsNoTracking()
                             .FirstOrDefaultAsync(p => p.Id == currentUserId)
                         ?? throw new Exception("کاربر جاری یافت نشد.");

            if (person.Role != Enums.SystemRole.admin && person.Role != Enums.SystemRole.superadmin)
            {
                _logger.LogWarning("دسترسی غیرمجاز: کاربر {UserId} با نقش {Role} تلاش کرد به عملیات ادمین دسترسی پیدا کند.", currentUserId, person.Role);
                throw new UnauthorizedAccessException("فقط کاربران با نقش ادمین یا سوپرادمین مجاز به انجام این عملیات هستند.");
            }
        }


        private async Task<bool> IsSuperAdmin(long userId)
        {
            return await _context.Persons
                .AnyAsync(p => p.Id == userId && p.Role.ToString().ToLower() == "superadmin");
        }

        private async Task<bool> IsOrgAdmin(long orgId, long userId)
        {
            return await _context.OrganizationMemberships
                .AnyAsync(m => m.OrganizationId == orgId && m.PersonId == userId && m.Role == SystemRole.admin);
        }

        private async Task<OrganizationDto> MapToDtoAsync(long organizationId)
        {
            var org = await _context.Organizations
                .Include(o => o.AllowedCargoTypes)
                .Include(o => o.Branches)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            return MapToDto(org);
        }

        private OrganizationDto MapToDto(Entities.Organization org)
        {
            return new OrganizationDto
            {
                Id = org.Id,
                Name = org.Name,
                AddressSummary = org.OriginAddress ?? "-",
                BranchCount = org.Branches.Count,
                AllowedCargoTypes = org.AllowedCargoTypes.Select(c => c.CargoType?.Name ?? "نامشخص").ToList()
            };
        }

        #endregion
    }
}
