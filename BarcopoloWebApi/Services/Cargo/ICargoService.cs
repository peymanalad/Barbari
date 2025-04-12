using BarcopoloWebApi.DTOs.Cargo;

namespace BarcopoloWebApi.Services.Cargo
{
    public interface ICargoService
    {
        Task<CargoSummaryDto> CreateAsync(CreateCargoDto dto, long currentUserId);
        Task<CargoSummaryDto> UpdateAsync(long id, UpdateCargoDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);

        Task<CargoDto> GetByIdAsync(long id, long currentUserId);
        Task<IEnumerable<CargoDto>> GetByOrderIdAsync(long orderId, long currentUserId);
        Task<PagedResult<CargoDto>> SearchAsync(CargoSearchDto filter, long currentUserId);
    }
}