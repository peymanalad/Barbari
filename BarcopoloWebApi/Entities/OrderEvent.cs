using System.ComponentModel.DataAnnotations;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.Entities
{
    public class OrderEvent
    {
        public long Id { get; set; }

        [Required]
        public long OrderId { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        public DateTime EventDateTime { get; set; } = DateTime.UtcNow;

        public long? ChangedByPersonId { get; set; }

        [MaxLength(1000)]
        public string Remarks { get; set; }


        public virtual Order Order { get; set; }

        public virtual Person ChangedByPerson { get; set; }


        public string GetEventSummary()
        {
            return $"{EventDateTime:G} | {Status} | {Remarks}";
        }

        public bool IsSystemGenerated() => ChangedByPersonId == null;
    }
}