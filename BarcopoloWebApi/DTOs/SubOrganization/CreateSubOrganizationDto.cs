using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.SubOrganization
{
    public class CreateSubOrganizationDto
    {
        [Required(ErrorMessage = "نام شعبه الزامی است.")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "شناسه سازمان مادر الزامی است.")]
        public long OrganizationId { get; set; }

        [Required(ErrorMessage = "شناسه آدرس مبدأ شعبه الزامی است.")]
        public long OriginAddressId { get; set; }
    }
}