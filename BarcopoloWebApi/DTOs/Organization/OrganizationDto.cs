namespace BarcopoloWebApi.DTOs.Organization
{
    public class OrganizationDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string AddressSummary { get; set; }

        public int BranchCount { get; set; }

        public bool HasCargoTypeRestrictions => AllowedCargoTypes != null && AllowedCargoTypes.Any();

        public List<string> AllowedCargoTypes { get; set; } = new();
    }
}