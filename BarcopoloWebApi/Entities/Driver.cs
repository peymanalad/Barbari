using BarcopoloWebApi.Helper;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Entities
{
    public class Driver
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [ForeignKey(nameof(Person))]
        public long PersonId { get; set; }

        [Required, MaxLength(50)]
        public string SmartCardCode { get; set; }

        [Required, MaxLength(20)]
        public string IdentificationNumber { get; set; } // شماره شناسنامه

        [Required, MaxLength(30)]
        public string LicenseNumber { get; set; }

        [MaxLength(50)]
        public string? LicenseIssuePlace { get; set; }

        [Required]
        public DateTime LicenseIssueDate { get; set; }

        [Required]
        public DateTime LicenseExpiryDate { get; set; }

        [MaxLength(30)]
        public string? InsuranceNumber { get; set; }

        public bool HasViolations { get; set; }

        [JsonIgnore]
        public virtual Person Person { get; set; }

        public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public bool IsLicenseValid() => LicenseExpiryDate > TehranDateTime.Now;
        public bool HasActiveInsurance() => !string.IsNullOrWhiteSpace(InsuranceNumber);
    }
}