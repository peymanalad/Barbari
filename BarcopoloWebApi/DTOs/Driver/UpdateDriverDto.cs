using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Driver
{
    public class UpdateDriverDto
    {
        [MaxLength(20)]
        public string? SmartCardCode { get; set; }

        [MaxLength(20)]
        public string? IdentificationNumber { get; set; }

        [MaxLength(20)]
        public string? LicenseNumber { get; set; }

        [MaxLength(50)]
        public string? LicenseIssuePlace { get; set; }

        public DateTime? LicenseIssueDate { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }

        [MaxLength(30)]
        public string? InsuranceNumber { get; set; }

        public bool? HasViolations { get; set; }
    }
}