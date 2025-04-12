using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.Entities
{
    public class Vehicle
    {
        public long Id { get; set; }

        public long? DriverId { get; set; }

        [Required, MaxLength(50)]
        public string SmartCardCode { get; set; }

        [Required, MaxLength(20)]
        public string PlateNumber { get; set; }

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