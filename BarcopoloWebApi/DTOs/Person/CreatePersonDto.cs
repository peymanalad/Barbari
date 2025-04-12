using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Person
{
    public class CreatePersonDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید 11 رقمی بوده و با 09 شروع شود.")]
        public string PhoneNumber { get; set; }

        [RegularExpression(@"^\d{10}$", ErrorMessage = "کد ملی باید 10 رقمی باشد.")]
        public string? NationalCode { get; set; }

        [MinLength(6, ErrorMessage = "رمز عبور باید حداقل 6 کاراکتر باشد.")]
        public string? Password { get; set; }
    }
}