using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Wallet
{
    public long Id { get; set; }

    public WalletOwnerType OwnerType { get; set; }
    public long OwnerId { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal Balance { get; set; }

    public virtual ICollection<WalletTransaction> Transactions { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }

    public virtual Organization? OwnerOrganization { get; set; }
    public virtual SubOrganization? OwnerBranch { get; set; }
    public virtual Person? OwnerPerson { get; set; }
}