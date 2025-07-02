using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Vehicle
{
    public class CreateVehicleDto
    {
        [Required, MaxLength(50)]
        public string SmartCardCode { get; set; }

        [Required, MaxLength(2)]
        public string PlateIranCode { get; set; }

        [Required, MaxLength(3)]
        public string PlateThreeDigit { get; set; }

        [Required, MaxLength(1)]
        public string PlateLetter { get; set; }

        [Required, MaxLength(2)]
        public string PlateTwoDigit { get; set; }

        [Range(1, 10)]
        public int Axles { get; set; }

        [MaxLength(50)]
        public string? Model { get; set; }

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Engine { get; set; }

        [MaxLength(50)]
        public string? Chassis { get; set; }

        public bool HasViolations { get; set; }
        public bool IsVan { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? VanCommission { get; set; }

        public bool IsBroken { get; set; }

        public long? DriverId { get; set; }
    }
}