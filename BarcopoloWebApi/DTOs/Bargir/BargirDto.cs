namespace BarcopoloWebApi.DTOs.Bargir
{
    public class BargirDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal MinCapacity { get; set; }
        public decimal MaxCapacity { get; set; }
        public long? VehicleId { get; set; }

        // فقط خواندنی - نمایش کامل پلاک بر اساس ساختار جدید
        public string? VehiclePlateNumber { get; set; }

        // جزئیات تفکیک‌شده پلاک (اختیاری، در صورت نیاز به نمایش جداگانه در فرانت)
        public string? PlateIranCode { get; set; }
        public string? PlateThreeDigit { get; set; }
        public string? PlateLetter { get; set; }
        public string? PlateTwoDigit { get; set; }
    }
}