using BarcopoloWebApi.DTOs.CargoType;

namespace BarcopoloWebApi.Services.CargoType
{
    public interface ICargoTypeService
    {
        Task<CargoTypeDto> CreateAsync(CreateCargoTypeDto dto, long currentUserId);
        Task<CargoTypeDto> UpdateAsync(long id, UpdateCargoTypeDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);
        Task<IEnumerable<CargoTypeDto>> GetAllAsync();

    }
}