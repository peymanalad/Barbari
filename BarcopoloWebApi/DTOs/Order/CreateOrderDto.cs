using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required]
        public long OwnerId { get; set; }

        public long? OriginAddressId { get; set; }

        public bool IsManualOrigin { get; set; }

        public string? OriginFullAddress { get; set; }
        public string? OriginCity { get; set; }
        public string? OriginProvince { get; set; }
        public string? OriginPostalCode { get; set; }
        public string? OriginPlate { get; set; }
        public string? OriginUnit { get; set; }
        public string? OriginTitle { get; set; }
        public bool SaveOriginAsFrequent { get; set; } = false;

        public long? DestinationAddressId { get; set; }
        public bool IsManualDestination { get; set; }


        public string? DestinationFullAddress { get; set; }
        public string? DestinationCity { get; set; }
        public string? DestinationProvince { get; set; }
        public string? DestinationPostalCode { get; set; }
        public string? DestinationPlate { get; set; }
        public string? DestinationUnit { get; set; }
        public string? DestinationTitle { get; set; }
        public bool SaveDestinationAsFrequent { get; set; } = false;


        [Required]
        public string SenderName { get; set; }

        [Required]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل فرستنده نامعتبر است")]
        public string SenderPhone { get; set; }

        [Required]
        public string ReceiverName { get; set; }

        [Required]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل گیرنده نامعتبر است")]
        public string ReceiverPhone { get; set; }

        public string? Details { get; set; }


        public bool IsForOrganization { get; set; } = false;
        public long? OrganizationId { get; set; } 
        public long? BranchId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DeclaredValue { get; set; } = 0;
        public bool IsInsuranceRequested { get; set; } = false;

        [Range(0, double.MaxValue)]
        public decimal Fare { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Insurance { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Vat { get; set; }

        public DateTime? LoadingTime { get; set; }
        
    }
}