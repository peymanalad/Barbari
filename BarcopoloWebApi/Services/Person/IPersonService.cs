using BarcopoloWebApi.DTOs.Person;

namespace BarcopoloWebApi.Services.Person
{
    public interface IPersonService
    {
        Task<PersonDto> CreateAsync(CreatePersonDto dto, long currentUserId);
        Task<PersonDto> UpdateAsync(long id, UpdatePersonDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);
        Task<bool> ActivateAsync(long id, long currentUserId);

        Task<PersonDto> GetByIdAsync(long id, long currentUserId);
        Task<BarcopoloWebApi.Entities.Person> GetEntityByIdAsync(long id);
        Task<IEnumerable<PersonDto>> GetAllAsync(long currentUserId);
        Task<long> FindPersonByNationalCodeAsync(string? nationalCode);
        Task<bool> CheckExistenceByNationalCodeAsync(PersonExistenceRequestDto dto, long currentUserId);



    }
}