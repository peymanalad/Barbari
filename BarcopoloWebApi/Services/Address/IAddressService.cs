using BarcopoloWebApi.DTOs;
using BarcopoloWebApi.DTOs.Address;

namespace BarcopoloWebApi.Services
{
    public interface IAddressService
    {
        Task<AddressDto> CreateAsync(CreateAddressDto dto, long currentUserId);
        Task<AddressDto> UpdateAsync(long id, UpdateAddressDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);

        Task<AddressDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<AddressDto>> GetByPersonIdAsync(long personId, long currentUserId);
    }
}