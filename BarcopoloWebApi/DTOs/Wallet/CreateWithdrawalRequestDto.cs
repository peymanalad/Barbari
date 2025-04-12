using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Withdrawal
{
    public class CreateWithdrawalRequestDto
    {
        [Required]
        [Range(1000, 10_000_000)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string DestinationBankAccount { get; set; }

        [Required]
        public long WalletId { get; set; }
    }
}