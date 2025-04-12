using BarcopoloWebApi.DTOs.Warehouse;

namespace BarcopoloWebApi.Services
{
    public interface IWarehouseService
    {

        Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, long currentUserId);
        Task<WarehouseDto> UpdateAsync(long id, UpdateWarehouseDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);


        Task<WarehouseDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<WarehouseDto>> GetAllAsync(long currentUserId);
    }
}