namespace BarcopoloWebApi.DTOs.Vehicle
{
    public class VehicleDto
    {
        public long Id { get; set; }

        public string PlateNumber { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public string SmartCardCode { get; set; }

        public int Axles { get; set; }
        public string Engine { get; set; }
        public string Chassis { get; set; }

        public bool IsBroken { get; set; }
        public bool IsVan { get; set; }
        public decimal? VanCommission { get; set; }

        public string? DriverFullName { get; set; }
        public long? DriverId { get; set; }
    }
}