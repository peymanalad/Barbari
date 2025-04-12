using BarcopoloWebApi.Enums;
using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.Entities
{
    public class WalletTransaction
    {
        public long Id { get; set; }

        [Required]
        public long WalletId { get; set; }

        [Required]
        public TransactionType TransactionType { get; set; } // Deposit / Withdrawal / Payment

        [Required]
        [Range(1, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public decimal BalanceBefore { get; set; }

        [Required]
        public decimal BalanceAfter { get; set; }

        public long? PerformedByPersonId { get; set; }

        public string? Description { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;


        public virtual Wallet Wallet { get; set; }
        public virtual Person? PerformedByPerson { get; set; }
    }
}