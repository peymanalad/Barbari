using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Entities
{
    public class Organization
    {
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public long OriginAddressId { get; set; }
        public long? OrganizationWalletId { get; set; }


        public virtual Address OriginAddress { get; set; }

        public virtual ICollection<SubOrganization> Branches { get; set; } = new List<SubOrganization>();

        [JsonIgnore]
        public virtual ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();

        [JsonIgnore]
        public virtual ICollection<OrganizationCargoType> AllowedCargoTypes { get; set; } = new List<OrganizationCargoType>();

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual Wallet OrganizationWallet { get; set; }


        public bool HasMember(long personId)
        {
            return Memberships.Any(m => m.PersonId == personId);
        }

        public bool IsCargoTypeAllowed(long cargoTypeId)
        {
            return AllowedCargoTypes.Any(c => c.CargoTypeId == cargoTypeId);
        }

        public void AddBranch(SubOrganization branch)
        {
            if (Branches.Any(b => b.Name == branch.Name))
                throw new InvalidOperationException("A branch with the same name already exists.");

            Branches.Add(branch);
        }
    }
}