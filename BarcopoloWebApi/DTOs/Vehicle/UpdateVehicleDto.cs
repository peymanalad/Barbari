using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Vehicle
{
    public class UpdateVehicleDto
    {
        [MaxLength(50)]
        public string? SmartCardCode { get; set; }

        [MaxLength(2)]
        public string? PlateIranCode { get; set; }

        [MaxLength(3)]
        public string? PlateThreeDigit { get; set; }

        [MaxLength(1)]
        public string? PlateLetter { get; set; }

        [MaxLength(2)]
        public string? PlateTwoDigit { get; set; }

        [Range(1, 10)]
        public int? Axles { get; set; }

        [MaxLength(50)]
        public string? Model { get; set; }

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Engine { get; set; }

        [MaxLength(50)]
        public string? Chassis { get; set; }

        public bool? HasViolations { get; set; }
        public bool? IsVan { get; set; }
        public decimal? VanCommission { get; set; }
        public bool? IsBroken { get; set; }

        public long? DriverId { get; set; }
    }
}