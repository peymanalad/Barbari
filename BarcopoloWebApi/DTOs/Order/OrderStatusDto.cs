public class OrderStatusDto
{
    public long OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime? LastUpdated { get; set; }
}