using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Driver
{
    public class CreateDriverDto
    {
        [Required]
        public long PersonId { get; set; }

        [Required]
        [MaxLength(20)]
        public string SmartCardCode { get; set; }

        [MaxLength(20)]
        public string IdentificationNumber { get; set; }

        [Required]
        [MaxLength(20)]
        public string LicenseNumber { get; set; }

        [MaxLength(50)]
        public string LicenseIssuePlace { get; set; }

        [Required]
        public DateTime LicenseIssueDate { get; set; }

        [Required]
        public DateTime LicenseExpiryDate { get; set; }

        [MaxLength(30)]
        public string InsuranceNumber { get; set; }

        public bool HasViolations { get; set; }
    }
}