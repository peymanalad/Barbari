using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Entities
{
    public class Warehouse
    {
        public long Id { get; set; }

        [Required]
        public long AddressId { get; set; }

        [Required, MaxLength(100)]
        public string WarehouseName { get; set; }

        [MaxLength(20)]
        public string InternalTelephone { get; set; }


        [Range(0, 100)]
        public decimal ManagerPercentage { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Rent { get; set; }

        [Range(0, 100)]
        public decimal TerminalPercentage { get; set; }

        [Range(0, 100)]
        public decimal VatPercentage { get; set; }

        [Range(0, 100)]
        public decimal IncomePercentage { get; set; }

        [Range(0, 100)]
        public decimal CommissionPercentage { get; set; }

        [Range(0, 100)]
        public decimal UnloadingPercentage { get; set; }

        [Range(0, 100)]
        public decimal DriverPaymentPercentage { get; set; }

        [Range(0, double.MaxValue)]
        public decimal InsuranceAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PerCargoInsurance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ReceiptIssuingCost { get; set; }

        [MaxLength(1000)]
        public string PrintText { get; set; }


        public bool IsDriverNetMandatory { get; set; }
        public bool IsWaybillFareMandatory { get; set; }
        public bool IsCargoValueMandatory { get; set; }
        public bool IsStampCostMandatory { get; set; }
        public bool IsParkingCostMandatory { get; set; }
        public bool IsLoadingMandatory { get; set; }
        public bool IsWarehousingMandatory { get; set; }
        public bool IsExcessCostMandatory { get; set; }

        public bool IsActive { get; set; }


        public virtual Address Address { get; set; }

        [JsonIgnore]
        public virtual ICollection<WarehouseVehicle> WarehouseVehicles { get; set; } = new List<WarehouseVehicle>();


        public decimal GetTotalCostPerCargo()
        {
            return InsuranceAmount + PerCargoInsurance + ReceiptIssuingCost;
        }

        public bool IsValidPercentageConfig()
        {
            return ManagerPercentage + TerminalPercentage + CommissionPercentage + DriverPaymentPercentage <= 100;
        }
    }
}
