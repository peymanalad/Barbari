using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Feedback
{
    public class UpdateFeedbackDto
    {
        [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد.")]
        public int? Rating { get; set; }

        [MaxLength(1000, ErrorMessage = "نظر نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.")]
        public string? Comment { get; set; }
    }
}