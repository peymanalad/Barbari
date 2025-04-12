namespace BarcopoloWebApi.Entities
{
    public class WarehouseVehicle
    {
        public long WarehouseId { get; set; }

        public long VehicleId { get; set; }

        public virtual Warehouse Warehouse { get; set; }

        public virtual Vehicle Vehicle { get; set; }
    }
}