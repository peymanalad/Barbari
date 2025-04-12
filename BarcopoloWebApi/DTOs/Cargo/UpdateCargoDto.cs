namespace BarcopoloWebApi.DTOs.Cargo
{
    public class UpdateCargoDto
    {
        public string? CargoType { get; set; }
        public bool? NeedsPackaging { get; set; }

        public string? Title { get; set; }
        public string? Contents { get; set; }
        public decimal? Value { get; set; }

        public decimal? Weight { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }

        public string? PackagingType { get; set; }
        public int? PackageCount { get; set; }

        public string? Description { get; set; }

        public List<string>? Images { get; set; } = new();
    }
}