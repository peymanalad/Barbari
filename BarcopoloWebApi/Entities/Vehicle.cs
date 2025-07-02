using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcopoloWebApi.Entities
{
    public class Vehicle
    {
        public long Id { get; set; }

        public long? DriverId { get; set; }

        [Required, MaxLength(50)]
        public string SmartCardCode { get; set; }

        [Required, MaxLength(2)]
        public string PlateIranCode { get; set; } // کد ایران (مثلاً 21)

        [Required, MaxLength(3)]
        public string PlateThreeDigit { get; set; } // سه رقم (مثلاً 737)

        [Required, MaxLength(1)]
        public string PlateLetter { get; set; } // حرف (مثلاً "س")

        [Required, MaxLength(2)]
        public string PlateTwoDigit { get; set; } // دو رقم (مثلاً 44)

        [NotMapped]
        public string? PlateNumber => $"{PlateThreeDigit} {PlateLetter} {PlateTwoDigit} ایران {PlateIranCode}";

        public string GetFormattedPlateNumber()
        {
            return $"{PlateTwoDigit}{PlateLetter}{PlateThreeDigit} ایران {PlateIranCode}";
        }

        [Range(1, 10)]
        public int Axles { get; set; } // تعداد محور

        [MaxLength(50)]
        public string Model { get; set; }

        [MaxLength(30)]
        public string Color { get; set; }

        [MaxLength(50)]
        public string Engine { get; set; }

        [MaxLength(50)]
        public string Chassis { get; set; }

        public bool HasViolations { get; set; }

        public bool IsVan { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? VanCommission { get; set; }

        public bool IsBroken { get; set; }


        public virtual Driver Driver { get; set; }
        public virtual Bargir Bargir { get; set; }
        public virtual ICollection<WarehouseVehicle> WarehouseVehicles { get; set; } = new List<WarehouseVehicle>();
        public virtual ICollection<OrderVehicle> OrderVehicles { get; set; } = new List<OrderVehicle>();


        public bool IsAvailableForUse() => !IsBroken && !HasViolations;

        public bool IsCommissionApplicable() => IsVan && VanCommission.HasValue && VanCommission.Value > 0;
    }
}