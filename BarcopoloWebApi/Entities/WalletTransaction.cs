using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Helper;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcopoloWebApi.Entities
{
    public class WalletTransaction
    {
        public long Id { get; set; }

        [Required]
        public long WalletId { get; set; }

        [Required]
        public TransactionType TransactionType { get; set; } // Deposit / Withdrawal / Payment

        [Required, Range(1, double.MaxValue)]
        [Column(TypeName = "decimal(18,0)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        public decimal BalanceBefore { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        public decimal BalanceAfter { get; set; }

        public long? PerformedByPersonId { get; set; }

        public string? Description { get; set; }

        public DateTime PerformedAt { get; set; } = TehranDateTime.Now;

        public virtual Wallet Wallet { get; set; }
        public virtual Person? PerformedByPerson { get; set; }
    }
}