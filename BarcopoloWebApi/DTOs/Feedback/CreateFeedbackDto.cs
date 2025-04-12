using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Feedback
{
    public class CreateFeedbackDto
    {
        [Required(ErrorMessage = "شناسه سفارش الزامی است.")]
        public long OrderId { get; set; }

        [Required(ErrorMessage = "امتیاز الزامی است.")]
        [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد.")]
        public int Rating { get; set; }

        [MaxLength(1000, ErrorMessage = "نظر نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.")]
        public string? Comment { get; set; }
    }
}