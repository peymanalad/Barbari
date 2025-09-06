using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Helper;

namespace BarcopoloWebApi.Entities
{
    public class Payment
    {
        public long Id { get; set; }

        [Required]
        public long OrderId { get; set; }

        [Required]
        public PaymentMethodType PaymentMethod { get; set; }

        [Required, Range(1, double.MaxValue)]
        [Column(TypeName = "decimal(18,0)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = TehranDateTime.Now;

        [Required, MaxLength(100)]
        public string TransactionId { get; set; }


        public virtual Order Order { get; set; }


        public bool IsValidAmount() => Amount > 0;

        public string GetPaymentSummary()
        {
            // return $"{PaymentDate.ToShortDateString()} | {PaymentType} | {Amount:C}"; // قدیمی
            return $"{PaymentDate.ToShortDateString()} | {PaymentMethod} | {Amount.ToRial()}";
        }
    }
}