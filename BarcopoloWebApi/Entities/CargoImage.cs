using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.Entities
{
    public class CargoImage
    {
        public long Id { get; set; }

        [Required]
        public long CargoId { get; set; }

        [Required, MaxLength(500)]
        public string ImageUrl { get; set; }


        public virtual Cargo Cargo { get; set; }
    }
}