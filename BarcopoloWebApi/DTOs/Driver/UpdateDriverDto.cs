using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Driver
{
    public class UpdateDriverDto
    {
        [MaxLength(50)]
        public string? SmartCardCode { get; set; }

        [MaxLength(20)]
        public string? IdentificationNumber { get; set; }

        [MaxLength(30)]
        public string? LicenseNumber { get; set; }

        [MaxLength(50)]
        public string? LicenseIssuePlace { get; set; }

        public DateTime? LicenseIssueDate { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }

        [MaxLength(30)]
        public string? InsuranceNumber { get; set; }

        public bool? HasViolations { get; set; }

        // فقط admin یا superadmin حق تغییر این‌ها را دارند
        [MaxLength(10)]
        public string? NationalCode { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }
}