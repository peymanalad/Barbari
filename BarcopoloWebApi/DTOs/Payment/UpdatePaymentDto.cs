using System.ComponentModel.DataAnnotations;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.DTOs.Payment
{
    public class UpdatePaymentDto
    {
        [MaxLength(50)]
        public PaymentMethodType? PaymentType { get; set; }

        [Range(1000, 1_000_000_000, ErrorMessage = "مبلغ باید معتبر باشد.")]
        public decimal? Amount { get; set; }

        [MaxLength(100)]
        public string? TransactionId { get; set; }
    }
}