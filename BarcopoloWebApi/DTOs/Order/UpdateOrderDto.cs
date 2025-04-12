public class UpdateOrderDto
{
    public long? OriginAddressId { get; set; }
    public long? DestinationAddressId { get; set; }

    public string? SenderName { get; set; }
    public string? SenderPhone { get; set; }

    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }

    public string? OrderDescription { get; set; }

    public decimal? Fare { get; set; }
    public decimal? Insurance { get; set; }
    public decimal? Vat { get; set; }

    public DateTime? LoadingTime { get; set; }
    public DateTime? DeliveryTime { get; set; }

    public string? Details { get; set; }
    public string? TrackingNumber { get; set; }
}