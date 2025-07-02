using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Driver
{
    public class CreateDriverDto
    {
        public long? PersonId { get; set; }

        [Required, MaxLength(50)]
        public string SmartCardCode { get; set; }

        [Required, MaxLength(20)]
        public string IdentificationNumber { get; set; } // شماره شناسنامه

        [Required, MaxLength(30)]
        public string LicenseNumber { get; set; }

        [MaxLength(50)]
        public string LicenseIssuePlace { get; set; }

        [Required]
        public DateTime LicenseIssueDate { get; set; }

        [Required]
        public DateTime LicenseExpiryDate { get; set; }

        [MaxLength(30)]
        public string? InsuranceNumber { get; set; }

        public bool HasViolations { get; set; }

        // این فیلدها برای self-register الزامی‌اند
        [MaxLength(10)]
        public string? NationalCode { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }
    }
}