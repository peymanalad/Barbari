using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Membership
{
    public class CreateMembershipDto
    {
        //[Required(ErrorMessage = "شناسه شخص الزامی است.")]
        //public long? PersonId { get; set; }

        [Required(ErrorMessage = "شناسه سازمان الزامی است.")]
        public long OrganizationId { get; set; }

        [Required(ErrorMessage = "نقش در سازمان الزامی است.")]
        [MaxLength(50)]
        public string Role { get; set; }

        public long? BranchId { get; set; }
        
        [MaxLength(50)]
        public string? FirstName { get; set; }
        
        [MaxLength(50)]
        public string? LastName { get; set; }

        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید 11 رقمی بوده و با 09 شروع شود.")]
        public string? PhoneNumber { get; set; }
        
        [RegularExpression(@"^\d{10}$", ErrorMessage = "کد ملی باید 10 رقمی باشد.")]
        public string? NationalCode { get; set; }
    }
}