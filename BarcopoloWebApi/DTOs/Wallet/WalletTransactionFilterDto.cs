// DTO برای فیلتر

using System.ComponentModel.DataAnnotations;
using BarcopoloWebApi.Enums;

public class WalletTransactionFilterDto
{
    public long WalletId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public TransactionType? TransactionType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxAmount { get; set; }
}