public class WalletTransactionDto
{
    public long Id { get; set; }
    public long WalletId { get; set; }
    public string TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Description { get; set; }
    public long? PerformedByPersonId { get; set; }
    public string? PerformedByFullName { get; set; }
}