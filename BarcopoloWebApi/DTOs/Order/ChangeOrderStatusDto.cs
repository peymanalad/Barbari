using BarcopoloWebApi.Enums;

public class ChangeOrderStatusDto
{
    public OrderStatus NewStatus { get; set; }
    public string? Remarks { get; set; }
}