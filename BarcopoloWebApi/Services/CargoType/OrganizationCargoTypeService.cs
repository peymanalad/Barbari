using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Organization;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

public class OrganizationCargoTypeService : IOrganizationCargoTypeService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<OrganizationCargoTypeService> _logger;

    public OrganizationCargoTypeService(DataBaseContext context, ILogger<OrganizationCargoTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<OrganizationCargoTypeDto>> GetAllAsync(long organizationId, long currentUserId)
    {
        if (!await IsOrgMember(organizationId, currentUserId))
            throw new AppException("دسترسی غیرمجاز");

        return await _context.OrganizationCargoTypes
            .Where(x => x.OrganizationId == organizationId)
            .Include(x => x.CargoType)
            .Select(x => new OrganizationCargoTypeDto
            {
                Id = x.Id,
                CargoTypeId = x.CargoTypeId,
                CargoTypeName = x.CargoType.Name
            })
            .ToListAsync();
    }

    public async Task<OrganizationCargoTypeDto> AddAsync(long organizationId, CreateOrganizationCargoTypeDto dto, long currentUserId)
    {
        if (!await IsOrgAdmin(organizationId, currentUserId))
            throw new AppException("تنها مدیر سازمان می‌تواند نوع بار اضافه کند");

        var exists = await _context.OrganizationCargoTypes.AnyAsync(x =>
            x.OrganizationId == organizationId && x.CargoTypeId == dto.CargoTypeId);

        if (exists)
            throw new AppException("این نوع بار قبلا اضافه شده است");

        var entity = new OrganizationCargoType
        {
            OrganizationId = organizationId,
            CargoTypeId = dto.CargoTypeId
        };

        _context.OrganizationCargoTypes.Add(entity);
        await _context.SaveChangesAsync();

        var name = await _context.CargoTypes
            .Where(c => c.Id == dto.CargoTypeId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

        return new OrganizationCargoTypeDto
        {
            Id = entity.Id,
            CargoTypeId = dto.CargoTypeId,
            CargoTypeName = name
        };
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        var entity = await _context.OrganizationCargoTypes
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return false;

        if (!await IsOrgAdmin(entity.OrganizationId, currentUserId))
            throw new AppException("فقط مدیر سازمان می‌تواند حذف کند");

        _context.OrganizationCargoTypes.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsCargoTypeAllowedAsync(long organizationId, long cargoTypeId)
    {
        return await _context.OrganizationCargoTypes.AnyAsync(x =>
            x.OrganizationId == organizationId && x.CargoTypeId == cargoTypeId);
    }

    private async Task<bool> IsOrgAdmin(long orgId, long userId)
    {
        return await _context.OrganizationMemberships.AnyAsync(m =>
            m.OrganizationId == orgId && m.PersonId == userId && m.Role == SystemRole.orgadmin);
    }

    private async Task<bool> IsOrgMember(long orgId, long userId)
    {
        return await _context.OrganizationMemberships.AnyAsync(m =>
            m.OrganizationId == orgId && m.PersonId == userId);
    }
}
