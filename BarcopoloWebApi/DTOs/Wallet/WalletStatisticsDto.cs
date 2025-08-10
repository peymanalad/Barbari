namespace BarcopoloWebApi.DTOs.Wallet
{
    public class WalletStatisticsDto
    {
        public long WalletId { get; set; }
        public decimal Balance { get; set; }
        public int TotalTransactionCount { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal TotalPayments { get; set; }
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }
}