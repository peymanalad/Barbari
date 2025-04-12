using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Organization
{
    public class UpdateOrganizationDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        public long? OriginAddressId { get; set; }
    }
}