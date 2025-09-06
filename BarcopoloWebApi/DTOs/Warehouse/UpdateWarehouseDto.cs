using BarcopoloWebApi.Helper;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.DTOs.Warehouse
{
    public class UpdateWarehouseDto
    {
        public string? WarehouseName { get; set; }

        public string? InternalTelephone { get; set; }


        public decimal? ManagerPercentage { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal? Rent { get; set; }
        public decimal? TerminalPercentage { get; set; }
        public decimal? VatPercentage { get; set; }
        public decimal? IncomePercentage { get; set; }
        public decimal? CommissionPercentage { get; set; }
        public decimal? UnloadingPercentage { get; set; }
        public decimal? DriverPaymentPercentage { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal? InsuranceAmount { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal? PerCargoInsurance { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal? ReceiptIssuingCost { get; set; }

        public string? PrintText { get; set; }


        public bool? IsDriverNetMandatory { get; set; }
        public bool? IsWaybillFareMandatory { get; set; }
        public bool? IsCargoValueMandatory { get; set; }
        public bool? IsStampCostMandatory { get; set; }
        public bool? IsParkingCostMandatory { get; set; }
        public bool? IsLoadingMandatory { get; set; }
        public bool? IsWarehousingMandatory { get; set; }
        public bool? IsExcessCostMandatory { get; set; }
        public bool? IsActive { get; set; }
    }
}