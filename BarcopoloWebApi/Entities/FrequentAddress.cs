using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Helper;
using System.ComponentModel.DataAnnotations;

public class FrequentAddress
{
    public long Id { get; set; }

    public long? PersonId { get; set; }
    public long? OrganizationId { get; set; }
    public long? BranchId { get; set; }

    [Required]
    public FrequentAddressType AddressType { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } 

    [Required, MaxLength(1000)]
    public string FullAddress { get; set; }

    [MaxLength(100)]
    public string City { get; set; }

    [MaxLength(100)]
    public string Province { get; set; }

    [MaxLength(20)]
    public string PostalCode { get; set; }

    [MaxLength(10)]
    public string Plate { get; set; }

    [MaxLength(10)]
    public string Unit { get; set; }

    public int UsageCount { get; set; } = 1;

    public DateTime LastUsed { get; set; } = TehranDateTime.Now;

    public virtual Person Person { get; set; }
    public virtual Organization Organization { get; set; }
    public virtual SubOrganization Branch { get; set; }
}