using BarcopoloWebApi.DTOs;
using BarcopoloWebApi.DTOs.SubOrganization;

namespace BarcopoloWebApi.Services.SubOrganization
{
    public interface ISubOrganizationService
    {
        Task<SubOrganizationDto> CreateAsync(CreateSubOrganizationDto dto, long currentUserId);
        Task<SubOrganizationDto> UpdateAsync(long id, UpdateSubOrganizationDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);

        Task<SubOrganizationDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<SubOrganizationDto>> GetByOrganizationIdAsync(long organizationId, long currentUserId);
    }
}