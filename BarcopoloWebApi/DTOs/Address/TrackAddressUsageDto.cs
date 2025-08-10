using BarcopoloWebApi.Entities;

public class TrackAddressUsageDto
{
    public Address Address { get; set; } // موجود در Entities
    public FrequentAddressType Type { get; set; } // Origin یا Destination
}
