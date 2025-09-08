using System.Text.Json.Serialization;
using BarcopoloWebApi.Helper;

namespace BarcopoloWebApi.DTOs.Payment
{
    public class PaymentSummaryDto
    {
        public long OrderId { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal TotalAmount { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal PaidAmount { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal RemainingAmount { get; set; }

        public IEnumerable<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
    }
}