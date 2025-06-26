using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.Entities
{
    public class Person
    {
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(10)]
        public string? NationalCode { get; set; }

        [Required]
        public SystemRole Role { get; set; } = SystemRole.user;

        public long? PersonalWalletId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, JsonIgnore]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        public bool IsActive { get; set; } = true;


        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

        [JsonIgnore]
        public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();

        public virtual ICollection<Order> OwnedOrders { get; set; } = new List<Order>();

        public virtual ICollection<Cargo> OwnedCargos { get; set; } = new List<Cargo>();

        public virtual ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();

        public virtual ICollection<OrderEvent> ChangedOrderEvents { get; set; } = new List<OrderEvent>();
        public virtual Driver Driver { get; set; }

        [ForeignKey("PersonalWalletId")]
        public virtual Wallet? PersonalWallet { get; set; }


        public string GetFullName() => $"{FirstName} {LastName}";

        public bool CanPlaceOrder() => IsActive && Role == SystemRole.user;

        public bool IsAdmin() => Role == SystemRole.admin;
        public bool IsSuperAdmin() => Role == SystemRole.superadmin;
        public bool IsAdminOrSuperAdmin()
        {
            return Role == SystemRole.admin || Role == SystemRole.superadmin;
        }
        public bool IsAdminOrSuperAdminOrMonitor()
        {
            return Role == SystemRole.admin || Role == SystemRole.superadmin || Role == SystemRole.monitor;
        }
    }
}