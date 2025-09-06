using BarcopoloWebApi.Helper;
using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.Entities
{
    public class Feedback
    {
        public long Id { get; set; }

        [Required]
        public long OrderId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } 

        [MaxLength(1000)]
        public string Comment { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = TehranDateTime.Now;
        public virtual Order Order { get; set; }


        public bool IsPositive() => Rating >= 4;

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Comment);
    }
}