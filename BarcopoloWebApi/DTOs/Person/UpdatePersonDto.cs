using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Person
{
    public class UpdatePersonDto
    {
        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [RegularExpression(@"^\d{10}$", ErrorMessage = "کد ملی باید 10 رقمی باشد.")]
        public string? NationalCode { get; set; }

        public bool? IsActive { get; set; }
        public string? Role { get; set; }
    }
}