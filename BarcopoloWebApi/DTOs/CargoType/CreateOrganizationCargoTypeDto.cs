using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Organization
{
    public class CreateOrganizationCargoTypeDto
    {
        [Required(ErrorMessage = "شناسه سازمان الزامی است.")]
        public long OrganizationId { get; set; }

        [Required]
        public long CargoTypeId { get; set; }
    }
}