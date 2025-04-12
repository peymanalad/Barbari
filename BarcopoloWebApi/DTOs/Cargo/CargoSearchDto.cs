public class CargoSearchDto
{
    public long? OrderId { get; set; }
    public string? TitleContains { get; set; }
    public decimal? MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}