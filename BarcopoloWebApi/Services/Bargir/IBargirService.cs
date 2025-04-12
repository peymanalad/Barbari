using BarcopoloWebApi.DTOs.Bargir;

namespace BarcopoloWebApi.Services
{
    public interface IBargirService
    {
        Task<BargirDto> CreateAsync(CreateBargirDto dto, long currentUserId);
        Task<BargirDto> UpdateAsync(long id, UpdateBargirDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);
        Task AssignToVehicleAsync(long bargirId, long vehicleId, long currentUserId);

        Task<BargirDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<BargirDto>> GetAllAsync(long currentUserId);
    }
}