using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Membership
{
    public class CreateMembershipDto
    {
        [Required(ErrorMessage = "شناسه شخص الزامی است.")]
        public long PersonId { get; set; }

        [Required(ErrorMessage = "شناسه سازمان الزامی است.")]
        public long OrganizationId { get; set; }

        [Required(ErrorMessage = "نقش در سازمان الزامی است.")]
        [MaxLength(50)]
        public string Role { get; set; }

        public long? BranchId { get; set; }
    }
}