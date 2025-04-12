using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Person;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Services.Person;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Services
{
    public class PersonService : IPersonService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<PersonService> _logger;
        private readonly IPasswordHasher<Entities.Person> _passwordHasher;

        public PersonService(DataBaseContext context, ILogger<PersonService> logger, IPasswordHasher<Entities.Person> passwordHasher)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task<BarcopoloWebApi.Entities.Person> GetEntityByIdAsync(long id)
        {
            return await _context.Persons.FirstOrDefaultAsync(p => p.Id == id);
        }


        public async Task<IEnumerable<PersonDto>> GetAllAsync(long currentUserId)
        {
            await EnsureAdminAccessAsync(currentUserId);

            var persons = await _context.Persons
                .Include(p => p.Addresses)
                .ToListAsync();

            return persons.Select(MapToDto);
        }

        public async Task<PersonDto> GetByIdAsync(long id, long currentUserId)
        {
            if (id != currentUserId)
                await EnsureAdminAccessAsync(currentUserId);

            var person = await _context.Persons.FindAsync(id)
                         ?? throw new Exception("کاربر مورد نظر یافت نشد.");

            return MapToDto(person);
        }

        public async Task<PersonDto> CreateAsync(CreatePersonDto dto, long currentUserId)
        {
            await EnsureAdminAccessAsync(currentUserId);

            if (await _context.Persons.AnyAsync(p => p.PhoneNumber == dto.PhoneNumber))
                throw new Exception("شماره موبایل قبلاً ثبت شده است.");

            var person = new Entities.Person
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                NationalCode = dto.NationalCode,
                Role = SystemRole.user,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var rawPassword = string.IsNullOrWhiteSpace(dto.Password) ? dto.PhoneNumber : dto.Password;
            person.PasswordHash = _passwordHasher.HashPassword(person, rawPassword);

            _context.Persons.Add(person);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Person created with Id {Id}", person.Id);
            return MapToDto(person);
        }

        public async Task<PersonDto> UpdateAsync(long id, UpdatePersonDto dto, long currentUserId)
        {
            if (id != currentUserId)
                await EnsureAdminAccessAsync(currentUserId);

            var person = await _context.Persons.FindAsync(id)
                         ?? throw new Exception("کاربر یافت نشد.");

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                person.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                person.LastName = dto.LastName;

            if (!string.IsNullOrWhiteSpace(dto.NationalCode))
                person.NationalCode = dto.NationalCode;

            if (dto.IsActive.HasValue)
                person.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Person with Id {Id} updated", id);
            return MapToDto(person);
        }

        public async Task<bool> DeleteAsync(long id, long currentUserId)
        {
            await EnsureAdminAccessAsync(currentUserId);

            var person = await _context.Persons.FindAsync(id);
            if (person == null)
                return false;

            person.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Person with Id {Id} deactivated", id);
            return true;
        }

        private static PersonDto MapToDto(Entities.Person p) => new PersonDto
        {
            Id = p.Id,
            FullName = p.GetFullName(),
            PhoneNumber = p.PhoneNumber,
            NationalCode = p.NationalCode,
            Role = p.Role.ToString(),
            CreatedAt = p.CreatedAt,
            IsActive = p.IsActive
        };

        private async Task EnsureAdminAccessAsync(long userId)
        {
            var user = await _context.Persons.FindAsync(userId)
                       ?? throw new Exception("کاربر جاری یافت نشد.");

            if (!user.IsAdminOrSuperAdmin())
                throw new UnauthorizedAccessException("دسترسی غیرمجاز.");
        }
    }
}
