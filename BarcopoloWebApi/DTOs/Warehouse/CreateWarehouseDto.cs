using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Warehouse
{
    public class CreateWarehouseDto
    {
        [Required]
        [MaxLength(100)]
        public string WarehouseName { get; set; }

        [Required]
        public long AddressId { get; set; }

        [MaxLength(20)]
        public string InternalTelephone { get; set; }

        public decimal ManagerPercentage { get; set; }
        public decimal Rent { get; set; }
        public decimal TerminalPercentage { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal InsuranceAmount { get; set; }

        public string? PrintText { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsCargoValueMandatory { get; set; }
        public bool IsDriverNetMandatory { get; set; }
    }
}