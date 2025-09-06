using System.Text.Json.Serialization;
using BarcopoloWebApi.Helper;

public class WalletDto
{
    public long WalletId { get; set; }
    [JsonConverter(typeof(CurrencyDecimalConverter))]
    public decimal Balance { get; set; }
    public string OwnerType { get; set; } 
    public long OwnerId { get; set; }
}