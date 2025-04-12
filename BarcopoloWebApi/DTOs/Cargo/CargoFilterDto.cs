public class CargoFilterDto
{
    public string? TitleContains { get; set; }
    public decimal? MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? CargoTypeName { get; set; }
    public bool? NeedsPackaging { get; set; }
    public long? OrderId { get; set; }
    public long? OwnerId { get; set; }
}