using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.Entities
{
    public class Bargir
    {
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinCapacity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxCapacity { get; set; }

        public long? VehicleId { get; set; }

        public virtual Vehicle Vehicle { get; set; }


        public bool IsWithinCapacity(decimal weight)
        {
            return weight >= MinCapacity && weight <= MaxCapacity;
        }
    }
}