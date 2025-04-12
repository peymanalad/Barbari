using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcopoloWebApi.Entities
{
    public class Cargo
    {
        public long Id { get; set; }

        [Required]
        public long OwnerId { get; set; }

        [Required]
        public long CargoTypeId { get; set; }

        [Required]
        public bool NeedsPackaging { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [Required, MaxLength(1000)]
        public string Contents { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Value { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Weight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Length { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Width { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Height { get; set; }

        [MaxLength(50)]
        public string PackagingType { get; set; }

        [Range(0, 10000)]
        public int PackageCount { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public long OrderId { get; set; }

        public virtual Order Order { get; set; }
        public virtual Person Owner { get; set; }
        public virtual CargoType CargoType { get; set; }
        public virtual ICollection<CargoImage> Images { get; set; } = new List<CargoImage>();

        public void SetPackagingInfo(bool needsPackaging, int packageCount)
        {
            if (!needsPackaging && packageCount > 0)
                throw new InvalidOperationException("PackageCount must be 0 when packaging is disabled.");

            NeedsPackaging = needsPackaging;
            PackageCount = packageCount;
        }

        public decimal CalculateVolume() => Length * Width * Height;
    }
}
