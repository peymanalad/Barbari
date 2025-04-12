using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید 11 رقمی بوده و با 09 شروع شود.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "رمز عبور جدید الزامی است.")]
        [MinLength(6, ErrorMessage = "رمز عبور باید حداقل 6 کاراکتر باشد.")]
        public string NewPassword { get; set; }
    }
}
