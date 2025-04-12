namespace BarcopoloWebApi.Entities
{
    public class OrderVehicle
    {
        public long OrderId { get; set; }

        public long VehicleId { get; set; }

        public virtual Order Order { get; set; }

        public virtual Vehicle Vehicle { get; set; }
    }
}