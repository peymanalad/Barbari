
using BarcopoloWebApi.DTOs.Vehicle;

namespace BarcopoloWebApi.Services
{
    public interface IVehicleService
    {
        Task<VehicleDto> CreateAsync(CreateVehicleDto dto, long currentUserId);
        Task<VehicleDto> UpdateAsync(long id, UpdateVehicleDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);


        Task<VehicleDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<VehicleDto>> GetAllAsync(long currentUserId);
        Task<IEnumerable<VehicleDto>> GetAvailableAsync(long currentUserId); 
        Task<IEnumerable<VehicleDto>> GetByDriverIdAsync(long driverId, long currentUserId);
        Task<IEnumerable<VehicleDto>> SearchAsync(VehicleFilterDto filter, long currentUserId);

        Task<int> GetBrokenCountAsync(long currentUserId);
    }
}