using BarcopoloWebApi.Enums;
using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Wallet
{
    public class DepositRequestDto
    {
        [Required]
        public WalletOwnerType TargetOwnerType { get; set; }

        [Required]
        public long TargetOwnerId { get; set; } 

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "مبلغ واریز باید بزرگتر از صفر باشد.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; } 

        // ممکنه فیلدهای بیشتری برای رفرنس پرداخت درگاه لازم باشه
        // public string? GatewayTransactionId { get; set; }
    }
}