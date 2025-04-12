namespace BarcopoloWebApi.DTOs.SubOrganization
{
    public class SubOrganizationDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public long OrganizationId { get; set; }

        public string OrganizationName { get; set; }

        public string AddressSummary { get; set; }
    }
}