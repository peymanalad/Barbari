namespace BarcopoloWebApi.DTOs.Bargir
{
    public class UpdateBargirDto
    {
        public string? Name { get; set; }
        public float? MinCapacity { get; set; }
        public float? MaxCapacity { get; set; }
        public long? VehicleId { get; set; }
    }
}