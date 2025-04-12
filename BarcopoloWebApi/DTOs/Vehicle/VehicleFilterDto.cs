namespace BarcopoloWebApi.DTOs.Vehicle
{
    public class VehicleFilterDto
    {
        public string? PlateNumber { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }

        public bool? IsVan { get; set; }
        public bool? IsBroken { get; set; }

        public long? DriverId { get; set; }
    }
}