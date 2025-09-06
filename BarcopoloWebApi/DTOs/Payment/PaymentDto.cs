using System.Text.Json.Serialization;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Helper;

namespace BarcopoloWebApi.DTOs.Payment
{
    public class PaymentDto
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public PaymentMethodType PaymentType { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string TransactionId { get; set; }

        public string PaymentSummary => $"{PaymentDate:yyyy/MM/dd} | {PaymentType} | {Amount.ToRial()}";
    }
}