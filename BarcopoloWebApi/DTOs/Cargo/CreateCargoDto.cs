using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Cargo
{
    public class CreateCargoDto
    {
        [Required]
        public long OwnerId { get; set; }

        [Required(ErrorMessage = "نوع بار الزامی است")]
        public string CargoType { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Contents { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "مقدار بار معتبر نیست")]
        public decimal Value { get; set; }

        [Range(0, 9999)]
        public decimal Weight { get; set; }
        [Range(0, 9999)]
        public decimal Length { get; set; }
        [Range(0, 9999)]
        public decimal Width { get; set; }
        [Range(0, 9999)]
        public decimal Height { get; set; }

        public bool NeedsPackaging { get; set; }

        public string? PackagingType { get; set; }

        [Range(0, 10000)]
        public int PackageCount { get; set; }

        public string? Description { get; set; }

        public List<string>? Images { get; set; } = new(); 

        [Required]
        public long OrderId { get; set; }
    }
}