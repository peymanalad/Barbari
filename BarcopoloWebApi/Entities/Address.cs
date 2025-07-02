using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Entities
{
    public class Address
    {
        public long Id { get; set; }

        public long? PersonId { get; set; }
        public long? OrganizationId { get; set; }
        public long? BranchId { get; set; }

        [Required, MaxLength(100)]
        public string City { get; set; }

        [Required, MaxLength(100)]
        public string Province { get; set; }

        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(20)]
        public string PostalCode { get; set; }

        [MaxLength(10)]
        public string Plate { get; set; }

        [MaxLength(10)]
        public string Unit { get; set; }

        [Required, MaxLength(1000)]
        public string FullAddress { get; set; }

        [MaxLength(1000)]
        public string? AdditionalInfo { get; set; }

        [JsonIgnore]
        public virtual Person? Person { get; set; }

        [JsonIgnore]
        public virtual Organization? Organization { get; set; }

        [JsonIgnore]
        public virtual SubOrganization? Branch { get; set; }

        public string GetBrief()
        {
            return $"{Title} - {City}, {Province}";
        }

        public bool IsComplete()
        {
            return !string.IsNullOrWhiteSpace(FullAddress) && !string.IsNullOrWhiteSpace(City);
        }
    }
}