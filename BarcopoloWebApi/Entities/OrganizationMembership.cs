using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.Entities
{
    public class OrganizationMembership
    {
        public long Id { get; set; }

        [Required]
        public long PersonId { get; set; }

        [Required]
        public long OrganizationId { get; set; }

        [Required]
        public SystemRole Role { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public long? BranchId { get; set; }


        [JsonIgnore]
        public virtual Person Person { get; set; }

        [JsonIgnore]
        public virtual Organization Organization { get; set; }

        [JsonIgnore]
        public virtual SubOrganization Branch { get; set; }


        public bool IsBranchMember() => BranchId.HasValue;

        public bool IsInRole(SystemRole role) => Role == role;
    }
}