namespace BarcopoloWebApi.DTOs.Cargo
{
    public class CargoDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Contents { get; set; }
        public decimal Value { get; set; }

        public decimal Weight { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }

        public string PackagingType { get; set; }
        public int PackageCount { get; set; }
        public string Description { get; set; }

        public bool NeedsPackaging { get; set; }
        public string CargoTypeName { get; set; }

        public List<string> ImageUrls { get; set; } = new();
    }
}