public class WalletDto
{
    public long WalletId { get; set; }
    public decimal Balance { get; set; }
    public string OwnerType { get; set; } 
    public long OwnerId { get; set; }
}