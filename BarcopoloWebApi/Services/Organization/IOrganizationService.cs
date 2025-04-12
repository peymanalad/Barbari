using BarcopoloWebApi.DTOs;
using BarcopoloWebApi.DTOs.Organization;

namespace BarcopoloWebApi.Services.Organization
{
    public interface IOrganizationService
    {
        Task<OrganizationDto> CreateAsync(CreateOrganizationDto dto, long currentUserId);
        Task<OrganizationDto> UpdateAsync(long id, UpdateOrganizationDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);

        Task<IEnumerable<OrganizationDto>> GetAllAsync(long currentUserId);
        Task<OrganizationDto> GetByIdAsync(long id, long currentUserId);
    }
}