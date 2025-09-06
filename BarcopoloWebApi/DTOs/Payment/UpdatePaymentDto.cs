using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Helper;

namespace BarcopoloWebApi.DTOs.Payment
{
    public class UpdatePaymentDto
    {
        [MaxLength(50)]
        public PaymentMethodType? PaymentType { get; set; }

        [Range(1000, 1_000_000_000, ErrorMessage = "مبلغ باید معتبر باشد.")]
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal? Amount { get; set; }

        [MaxLength(100)]
        public string? TransactionId { get; set; }
    }
}