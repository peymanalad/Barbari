using System;
using System.Text.Json.Serialization;
using BarcopoloWebApi.Helper;

namespace BarcopoloWebApi.DTOs.Wallet
{
    public class WalletStatisticsDto
    {
        public long WalletId { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal Balance { get; set; }

        public int TotalTransactionCount { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal TotalDeposits { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal TotalWithdrawals { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal TotalPayments { get; set; }
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }
}