using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Membership
{
    public class UpdateMembershipDto
    {
        [MaxLength(50)]
        public string? Role { get; set; }

        public long? BranchId { get; set; }
    }
}