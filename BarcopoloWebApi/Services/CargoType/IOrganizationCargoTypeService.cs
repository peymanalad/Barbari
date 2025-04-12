using BarcopoloWebApi.DTOs.Organization;

public interface IOrganizationCargoTypeService
{
    Task<IEnumerable<OrganizationCargoTypeDto>> GetAllAsync(long organizationId, long currentUserId);
    Task<OrganizationCargoTypeDto> AddAsync(long organizationId, CreateOrganizationCargoTypeDto dto, long currentUserId);
    Task<bool> DeleteAsync(long id, long currentUserId);
    Task<bool> IsCargoTypeAllowedAsync(long organizationId, long cargoTypeId);
}