public class PersonDto
{
    public long Id { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string? NationalCode { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}