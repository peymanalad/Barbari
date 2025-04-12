using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.DTOs.Payment
{
    public class PaymentDto
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public PaymentMethodType PaymentType { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string TransactionId { get; set; }

        public string PaymentSummary => $"{PaymentDate:yyyy/MM/dd} | {PaymentType} | {Amount:C0}";
    }
}