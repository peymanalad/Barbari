using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Organization
{
    public class CreateOrganizationDto
    {
        [Required(ErrorMessage = "نام سازمان الزامی است.")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "شناسه آدرس مبدأ الزامی است.")]
        public long OriginAddressId { get; set; }
    }
}