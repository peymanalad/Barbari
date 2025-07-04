﻿using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.SubOrganization;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Services.SubOrganization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Services;

public class SubOrganizationService : ISubOrganizationService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<SubOrganizationService> _logger;

    public SubOrganizationService(DataBaseContext context, ILogger<SubOrganizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SubOrganizationDto> CreateAsync(CreateSubOrganizationDto dto, long currentUserId)
    {
        await EnsureHasCreatePermission(dto.OrganizationId, currentUserId);

        var organization = await _context.Organizations.FindAsync(dto.OrganizationId)
            ?? throw new Exception("سازمان یافت نشد");

        var branch = new Entities.SubOrganization
        {
            Name = dto.Name,
            OrganizationId = dto.OrganizationId,
            OriginAddress = dto.OriginAddress
        };

        _context.SubOrganizations.Add(branch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("شعبه جدید با شناسه {BranchId} برای سازمان {OrgId} ثبت شد", branch.Id, dto.OrganizationId);
        return await MapToDtoAsync(branch);
    }

    public async Task<SubOrganizationDto> GetByIdAsync(long id, long currentUserId)
    {
        await EnsureHasViewPermission(id, currentUserId);

        var branch = await _context.SubOrganizations
            .Include(b => b.Organization)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception("شعبه یافت نشد");

        return await MapToDtoAsync(branch);
    }

    public async Task<IEnumerable<SubOrganizationDto>> GetByOrganizationIdAsync(long organizationId, long currentUserId)
    {
        await EnsureIsOrgAdminOrHigher(organizationId, currentUserId);

        var branches = await _context.SubOrganizations
            .Where(b => b.OrganizationId == organizationId)
            .Include(b => b.Organization)
            .ToListAsync();

        return branches.Select(MapToDto).ToList();
    }


    public async Task<SubOrganizationDto> UpdateAsync(long id, UpdateSubOrganizationDto dto, long currentUserId)
    {
        await EnsureHasEditPermission(id, currentUserId);

        var branch = await _context.SubOrganizations.FindAsync(id)
            ?? throw new Exception("شعبه یافت نشد");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            branch.Name = dto.Name;

        if (!string.IsNullOrWhiteSpace(dto.OriginAddress))
        {
            branch.OriginAddress = dto.OriginAddress;
        }


        await _context.SaveChangesAsync();
        return await MapToDtoAsync(branch);
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        await EnsureHasDeletePermission(id, currentUserId);
        var branch = await _context.SubOrganizations
            .Include(b => b.Memberships)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
            return false;

        if (branch.Memberships.Any())
            _context.OrganizationMemberships.RemoveRange(branch.Memberships);

        _context.SubOrganizations.Remove(branch);
        await _context.SaveChangesAsync();

        return true;
    }


    private async Task<bool> IsSuperAdminAsync(long personId) =>
        await _context.Persons.AnyAsync(p => p.Id == personId && p.Role == SystemRole.superadmin);

    private async Task<bool> IsAdminAsync(long personId) =>
        await _context.Persons.AnyAsync(p => p.Id == personId && p.Role == SystemRole.admin);

    private async Task<bool> IsOrgAdminAsync(long orgId, long personId) =>
        await _context.OrganizationMemberships
            .AnyAsync(m =>
                m.OrganizationId == orgId &&
                m.PersonId == personId &&
                m.Role == SystemRole.orgadmin);

    private async Task<bool> IsBranchAdminAsync(long branchId, long personId)
    {
        return await _context.OrganizationMemberships
            .AnyAsync(m =>
                m.BranchId == branchId &&
                m.PersonId == personId &&
                m.Role == SystemRole.branchadmin);
    }

    private async Task<bool> IsBranchMemberAsync(long branchId, long personId)
    {
        return await _context.OrganizationMemberships
            .AnyAsync(m => m.BranchId == branchId && m.PersonId == personId);
    }

    private async Task<bool> IsOrgMemberAsync(long orgId, long personId)
    {
        return await _context.OrganizationMemberships
            .AnyAsync(m => m.OrganizationId == orgId && m.PersonId == personId);
    }

    private async Task<long?> GetOrgIdFromBranchAsync(long branchId)
    {
        return await _context.SubOrganizations
            .Where(s => s.Id == branchId)
            .Select(s => (long?)s.OrganizationId)
            .FirstOrDefaultAsync();
    }

    private async Task EnsureHasCreatePermission(long orgId, long userId)
    {
        if (!await IsSuperAdminAsync(userId) &&
            !await IsAdminAsync(userId) &&
            !await IsOrgAdminAsync(orgId, userId))
        {
            throw new UnauthorizedAccessException("اجازه ایجاد شعبه برای این سازمان را ندارید");
        }
    }

    private async Task EnsureHasViewPermission(long branchId, long userId)
    {
        var orgId = await GetOrgIdFromBranchAsync(branchId)
                    ?? throw new Exception("شعبه یافت نشد");

        if (await IsSuperAdminAsync(userId) || await IsAdminAsync(userId))
            return;

        var isOrgAdmin = await IsOrgAdminAsync(orgId, userId);
        var isBranchAdmin = await IsBranchAdminAsync(branchId, userId);
        var isBranchMember = await IsBranchMemberAsync(branchId, userId);

        if (!isOrgAdmin && !isBranchAdmin && !isBranchMember)
            throw new UnauthorizedAccessException("اجازه مشاهده این شعبه را ندارید");
    }


    private async Task EnsureHasEditPermission(long branchId, long userId)
    {
        var orgId = await GetOrgIdFromBranchAsync(branchId)
                    ?? throw new Exception("شعبه یافت نشد");

        if (await IsSuperAdminAsync(userId) || await IsAdminAsync(userId))
            return;

        var isOrgAdmin = await IsOrgAdminAsync(orgId, userId);
        var isBranchAdmin = await IsBranchAdminAsync(branchId, userId);

        if (!isOrgAdmin && !isBranchAdmin)
            throw new UnauthorizedAccessException("اجازه ویرایش این شعبه را ندارید");
    }


    private async Task EnsureHasDeletePermission(long branchId, long userId)
    {
        var orgId = await GetOrgIdFromBranchAsync(branchId)
                    ?? throw new Exception("شعبه یافت نشد");

        if (await IsSuperAdminAsync(userId) || await IsAdminAsync(userId))
            return;

        if (!await IsOrgAdminAsync(orgId, userId))
            throw new UnauthorizedAccessException("اجازه حذف این شعبه را ندارید");
    }

    private async Task EnsureIsOrgMemberOrAdmin(long orgId, long userId)
    {
        if (await IsSuperAdminAsync(userId) || await IsAdminAsync(userId))
            return;

        if (!await IsOrgMemberAsync(orgId, userId) && !await IsOrgAdminAsync(orgId, userId))
        {
            throw new UnauthorizedAccessException("دسترسی به سازمان ندارید");
        }
    }
    private async Task EnsureIsOrgAdminOrHigher(long orgId, long userId)
    {
        if (await IsSuperAdminAsync(userId) || await IsAdminAsync(userId))
            return;

        if (!await IsOrgAdminAsync(orgId, userId))
        {
            throw new UnauthorizedAccessException("دسترسی به لیست شعبه‌های این سازمان را ندارید");
        }
    }



    private async Task<SubOrganizationDto> MapToDtoAsync(Entities.SubOrganization entity)
    {
        var orgName = await _context.Organizations
            .Where(o => o.Id == entity.OrganizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync();

        return new SubOrganizationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            OrganizationId = entity.OrganizationId,
            OrganizationName = orgName,
            AddressSummary = entity.OriginAddress ?? "-"
        };
    }

    private SubOrganizationDto MapToDto(Entities.SubOrganization entity)
    {
        return new SubOrganizationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            OrganizationId = entity.OrganizationId,
            OrganizationName = entity.Organization?.Name ?? "-",
            AddressSummary = entity.OriginAddress ?? "-"
        };
    }
}
