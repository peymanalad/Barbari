namespace BarcopoloWebApi.DTOs.Vehicle
{
    public class VehicleDto
    {
        public long Id { get; set; }

        public string SmartCardCode { get; set; }

        public string PlateIranCode { get; set; }     // سمت راست پلاک (مثلاً: "11")
        public string PlateThreeDigit { get; set; }   // سه رقم اول (مثلاً: "365")
        public string PlateLetter { get; set; }       // حرف وسط (مثلاً: "ب")
        public string PlateTwoDigit { get; set; }     // دو رقم آخر (مثلاً: "12")

        public string PlateNumber { get; set; }

        public int Axles { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public string? Engine { get; set; }
        public string? Chassis { get; set; }
        public bool HasViolations { get; set; }
        public bool IsVan { get; set; }
        public decimal? VanCommission { get; set; }
        public bool IsBroken { get; set; }

        public long? DriverId { get; set; }
        public string? DriverFullName { get; set; }
    }
}