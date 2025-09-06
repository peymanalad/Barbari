using System;
using System.Text.Json.Serialization;
using BarcopoloWebApi.Helper;

namespace BarcopoloWebApi.DTOs.Wallet
{
    public class WalletTransactionDto
    {
        public long Id { get; set; }
        public long WalletId { get; set; }
        public string TransactionType { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal Amount { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal BalanceBefore { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal BalanceAfter { get; set; }

        public DateTime PerformedAt { get; set; }
        public string? Description { get; set; }
        public long? PerformedByPersonId { get; set; }
        public string? PerformedByFullName { get; set; }
    }
}