using BarcopoloWebApi.Entities;

public interface IFrequentAddressService
{
    Task InsertOrUpdateAsync(Address address, FrequentAddressType addressType, long? personId = null,
        long? organizationId = null, long? branchId = null);

    Task<List<FrequentAddressDto>> GetDestinationsAsync(long currentUserId, FrequentAddressScope scope);
    Task<List<FrequentAddressDto>> GetOriginsAsync(long currentUserId, FrequentAddressScope scope);

    Task<List<FrequentAddressDto>> GetFrequentAddressesAsync(
        long currentUserId,
        FrequentAddressType type,
        bool isForOrganization,
        long? organizationId = null,
        long? branchId = null
    );


}