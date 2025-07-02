
using BarcopoloWebApi.DTOs.Driver;

namespace BarcopoloWebApi.Services
{
    public interface IDriverService
    {
        Task<DriverDto> CreateAsync(CreateDriverDto dto, long currentUserId);
        Task<DriverDto> UpdateAsync(long id, UpdateDriverDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);
        Task AssignToVehicleAsync(long driverId, long vehicleId, long currentUserId);
        Task<DriverDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<DriverDto>> GetAllAsync(long currentUserId);
        Task<DriverDto> SelfRegisterAsync(SelfRegisterDriverDto dto);
    }
}