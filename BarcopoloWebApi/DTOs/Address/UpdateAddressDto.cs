using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Address
{
    public class UpdateAddressDto
    {
        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Province { get; set; }

        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(10)]
        public string? Plate { get; set; }

        [MaxLength(10)]
        public string? Unit { get; set; }

        [MaxLength(500)]
        public string? FullAddress { get; set; }

        [MaxLength(250)]
        public string? AdditionalInfo { get; set; }
    }
}