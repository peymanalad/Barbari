using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.OrderEvent
{
    public class CreateOrderEventDto
    {
        [Required(ErrorMessage = "شناسه سفارش الزامی است.")]
        public long OrderId { get; set; }

        [Required(ErrorMessage = "وضعیت سفارش الزامی است.")]
        [MaxLength(30)]
        public string Status { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}