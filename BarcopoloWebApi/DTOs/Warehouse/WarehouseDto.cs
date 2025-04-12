namespace BarcopoloWebApi.DTOs.Warehouse
{
    public class WarehouseDto
    {
        public long Id { get; set; }

        public string WarehouseName { get; set; }

        public string InternalTelephone { get; set; }

        public string AddressSummary { get; set; }

        public bool IsActive { get; set; }


        public decimal ManagerPercentage { get; set; }

        public decimal Rent { get; set; }

        public decimal TerminalPercentage { get; set; }

        public decimal VatPercentage { get; set; }

        public decimal InsuranceAmount { get; set; }

        public string PrintText { get; set; }


        public bool IsDriverNetMandatory { get; set; }

        public bool IsCargoValueMandatory { get; set; }
    }
}