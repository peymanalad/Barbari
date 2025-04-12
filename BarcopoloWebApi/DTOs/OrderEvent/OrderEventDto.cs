namespace BarcopoloWebApi.DTOs.OrderEvent
{
    public class OrderEventDto
    {
        public long Id { get; set; }

        public long OrderId { get; set; } 

        public string Status { get; set; }

        public DateTime EventDateTime { get; set; }

        public string? Remarks { get; set; }

        public string ChangedByFullName { get; set; }

        public string TimeAgo => GetRelativeTime();

        private string GetRelativeTime()
        {
            var span = DateTime.UtcNow - EventDateTime;

            if (span.TotalMinutes < 1)
                return "لحظاتی پیش";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} دقیقه پیش";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} ساعت پیش";
            if (span.TotalDays < 30)
                return $"{(int)span.TotalDays} روز پیش";

            return EventDateTime.ToString("yyyy/MM/dd HH:mm");
        }
    }
}