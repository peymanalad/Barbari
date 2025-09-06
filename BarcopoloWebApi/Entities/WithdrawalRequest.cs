using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Helper;

namespace BarcopoloWebApi.Entities
{
    public class WithdrawalRequest
    {
        public long Id { get; set; }

        [Required]
        public long SourceWalletId { get; set; }

        [Required]
        [Range(1000, double.MaxValue)]
        [Column(TypeName = "decimal(18,0)")]
        public decimal Amount { get; set; }

        [Required]
        public long RequesterPersonId { get; set; }

        public long? ReviewedByAdminId { get; set; }
        public string DestinationBankAccount { get; set; }

        [Required]
        public WithdrawalRequestStatus Status { get; set; } = WithdrawalRequestStatus.Pending;

        public DateTime RequestedAt { get; set; } = TehranDateTime.Now;
        public DateTime? ReviewedAt { get; set; }

        // Navigation properties
        public virtual Wallet SourceWallet { get; set; }
        public virtual Person RequesterPerson { get; set; }
        public virtual Person ReviewedByAdmin { get; set; } // برای Include درست!
    }
}