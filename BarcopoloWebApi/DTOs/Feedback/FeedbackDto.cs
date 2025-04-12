namespace BarcopoloWebApi.DTOs.Feedback
{
    public class FeedbackDto
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public int Rating { get; set; } // 1 تا 5

        public string? Comment { get; set; }

        public DateTime SubmittedAt { get; set; }

        public string CustomerFullName { get; set; }

        public string RatingText => Rating switch
        {
            1 => "خیلی بد",
            2 => "بد",
            3 => "متوسط",
            4 => "خوب",
            5 => "عالی",
            _ => "نامشخص"
        };
    }
}