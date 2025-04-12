using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required]
        public long OwnerId { get; set; }

        [Required]
        public long OriginAddressId { get; set; }

        [Required]
        public long DestinationAddressId { get; set; }

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

        [Range(0, 999999)]
        public decimal Fare { get; set; }

        [Range(0, 999999)]
        public decimal Insurance { get; set; }

        [Range(0, 999999)]
        public decimal Vat { get; set; }

        public DateTime? LoadingTime { get; set; }
    }
}