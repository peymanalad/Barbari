using System.ComponentModel.DataAnnotations;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.DTOs.Payment
{
    public class CreatePaymentDto
    {
        [Required(ErrorMessage = "شناسه سفارش الزامی است.")]
        public long OrderId { get; set; }

        [Required(ErrorMessage = "نوع پرداخت الزامی است.")]
        public PaymentMethodType PaymentType { get; set; }

        [Required(ErrorMessage = "مبلغ پرداخت الزامی است.")]
        [Range(1000, 1_000_000_000, ErrorMessage = "مبلغ باید بیشتر از ۱۰۰۰ تومان باشد.")]
        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "شناسه تراکنش الزامی است.")]
        [MaxLength(100)]
        public string TransactionId { get; set; }
    }
}