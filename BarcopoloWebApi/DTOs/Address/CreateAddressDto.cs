public class CreateAddressDto
{
    public long PersonId { get; set; }

    public string City { get; set; }
    public string Province { get; set; }
    public string Title { get; set; }
    public string PostalCode { get; set; }
    public string Plate { get; set; }
    public string Unit { get; set; }
    public string FullAddress { get; set; }
    public string? AdditionalInfo { get; set; }
}