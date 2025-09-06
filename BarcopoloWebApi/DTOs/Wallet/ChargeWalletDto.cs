using BarcopoloWebApi.Helper;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Wallet
{
    public class ChargeWalletDto
    {
        [Required(ErrorMessage = "شناسه کیف پول الزامی است.")]
        public long WalletId { get; set; }

        [Required(ErrorMessage = "مبلغ الزامی است.")]
        [Range(1000, 1_000_000_000, ErrorMessage = "مبلغ باید بین ۱۰۰۰ تا ۱ میلیارد ریال باشد.")]
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal Amount { get; set; }

        [MaxLength(255, ErrorMessage = "توضیحات نمی‌تواند بیشتر از ۲۵۵ کاراکتر باشد.")]
        public string? Description { get; set; }
    }
}