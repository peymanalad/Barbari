using System.ComponentModel.DataAnnotations;

public class SelfRegisterDriverDto
{
    [Required, MaxLength(50)]
    public string FirstName { get; set; }

    [Required, MaxLength(50)]
    public string LastName { get; set; }

    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; }

    [Required, MaxLength(10)]
    public string NationalCode { get; set; }

    [Required, MaxLength(50)]
    public string SmartCardCode { get; set; }

    [Required, MaxLength(20)]
    public string IdentificationNumber { get; set; }

    [Required, MaxLength(30)]
    public string LicenseNumber { get; set; }

    [MaxLength(50)]
    public string LicenseIssuePlace { get; set; }

    [Required]
    public DateTime LicenseIssueDate { get; set; }

    [Required]
    public DateTime LicenseExpiryDate { get; set; }

    [MaxLength(30)]
    public string InsuranceNumber { get; set; }

    public bool HasViolations { get; set; }
}