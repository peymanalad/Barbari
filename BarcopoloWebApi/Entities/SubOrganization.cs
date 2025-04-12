using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Entities
{
    public class SubOrganization
    {
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public long OrganizationId { get; set; }

        [Required, MaxLength(1000)]
        public string OriginAddress { get; set; }
        
        public long? BranchWalletId { get; set; }


        [JsonIgnore]
        public virtual Organization Organization { get; set; }

        [JsonIgnore]
        public virtual ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public virtual Wallet BranchWallet { get; set; }


        public bool HasActiveOrders() => Orders.Any();

        public bool BelongsTo(long organizationId) => OrganizationId == organizationId;
    }
}