using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Address
{
    public class CreateAddressDto
    {
        [Required]
        public long PersonId { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        [MaxLength(100)]
        public string Province { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; }

        [MaxLength(10)]
        public string Plate { get; set; }

        [MaxLength(10)]
        public string Unit { get; set; }

        [Required]
        [MaxLength(500)]
        public string FullAddress { get; set; }

        [MaxLength(250)]
        public string? AdditionalInfo { get; set; }
    }
}