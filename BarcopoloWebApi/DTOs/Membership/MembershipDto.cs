namespace BarcopoloWebApi.DTOs.Membership
{
    public class MembershipDto
    {
        public long Id { get; set; }

        public long PersonId { get; set; }
        public string PersonFullName { get; set; }

        public long OrganizationId { get; set; }
        public string OrganizationName { get; set; }

        public long? BranchId { get; set; }
        public string? BranchName { get; set; }

        public string Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}