namespace BarcopoloWebApi.Services.Address
{
    public interface IFrequentAddressService
    {
        Task InsertOrUpdateAsync(Entities.Address address, FrequentAddressType addressType, long? personId = null,
            long? organizationId = null, long? branchId = null);
        Task<List<FrequentAddressDto>> GetAccessibleOriginsAsync(long currentUserId);
        Task<List<FrequentAddressDto>> GetAccessibleDestinationsAsync(long currentUserId);
    }
}
