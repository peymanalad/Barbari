using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.SubOrganization
{
    public class UpdateSubOrganizationDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        public string? OriginAddress { get; set; }
    }
}