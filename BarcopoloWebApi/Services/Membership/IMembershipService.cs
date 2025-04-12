using BarcopoloWebApi.DTOs.Membership;

public interface IMembershipService
{
    Task<MembershipDto> AddAsync(CreateMembershipDto dto, long currentUserId);
    Task<MembershipDto> UpdateAsync(long membershipId, UpdateMembershipDto dto, long currentUserId);
    Task<bool> RemoveAsync(long membershipId, long currentUserId);
    Task<MembershipDto> GetByIdAsync(long membershipId, long currentUserId);
    Task<IEnumerable<MembershipDto>> GetByOrganizationIdAsync(long organizationId, long currentUserId);
    Task<IEnumerable<MembershipDto>> GetAllForCurrentScopeAsync(long currentUserId);
}