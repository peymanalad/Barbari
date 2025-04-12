public class BargirDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public decimal MinCapacity { get; set; }
    public decimal MaxCapacity { get; set; }
    public long? VehicleId { get; set; }
    public string? VehiclePlateNumber { get; set; } 
}