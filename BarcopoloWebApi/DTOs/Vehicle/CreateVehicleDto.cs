using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Vehicle
{
    public class CreateVehicleDto
    {
        [Required]
        [MaxLength(20)]
        public string SmartCardCode { get; set; }

        [Required]
        [MaxLength(20)]
        public string PlateNumber { get; set; }

        [Required]
        [Range(1, 10)]
        public int Axles { get; set; }

        [Required]
        [MaxLength(50)]
        public string Model { get; set; }

        [MaxLength(30)]
        public string Color { get; set; }

        [MaxLength(50)]
        public string Engine { get; set; }

        [MaxLength(50)]
        public string Chassis { get; set; }

        public bool IsBroken { get; set; }
        public bool IsVan { get; set; }

        [Range(0, 100)]
        public decimal? VanCommission { get; set; }

        public long? DriverId { get; set; }
    }
}