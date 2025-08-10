public class FrequentAddressDto
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string FullAddress { get; set; }
    public string City { get; set; }
    public string Province { get; set; }
    public string PostalCode { get; set; }
    public string Plate { get; set; }
    public string Unit { get; set; }
    public int UsageCount { get; set; }
    public DateTime LastUsed { get; set; }
    public string AddressType { get; set; }

    public long? PersonId { get; set; }
    public long? OrganizationId { get; set; }
    public long? BranchId { get; set; }
}