using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Entities
{
    public class Driver
    {
        public long Id { get; set; }

        [Required]
        public long PersonId { get; set; }

        [Required, MaxLength(50)]
        public string SmartCardCode { get; set; }

        [Required, MaxLength(20)]
        public string IdentificationNumber { get; set; } // شماره شناسنامه

        [Required, MaxLength(30)]
        public string LicenseNumber { get; set; }

        [MaxLength(50)]
        public string LicenseIssuePlace { get; set; }

        public DateTime LicenseIssueDate { get; set; }

        public DateTime LicenseExpiryDate { get; set; }

        [MaxLength(30)]
        public string InsuranceNumber { get; set; }

        public bool HasViolations { get; set; }


        [JsonIgnore]
        public virtual Person Person { get; set; }

        public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();


        public bool IsLicenseValid()
        {
            return LicenseExpiryDate > DateTime.UtcNow;
        }

        public bool HasActiveInsurance()
        {
            return !string.IsNullOrEmpty(InsuranceNumber);
        }
    }
}