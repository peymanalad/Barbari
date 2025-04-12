namespace BarcopoloWebApi.Entities
{
    public class OrganizationCargoType
    {
        public long Id { get; set; }

        public long OrganizationId { get; set; }

        public long CargoTypeId { get; set; }

        public virtual Organization Organization { get; set; }

        public virtual CargoType CargoType { get; set; }
    }
}