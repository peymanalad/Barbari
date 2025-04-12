using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.CargoType;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BarcopoloWebApi.Services.CargoType
{
    public class CargoTypeService : ICargoTypeService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<CargoTypeService> _logger;

        public CargoTypeService(DataBaseContext context, ILogger<CargoTypeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CargoTypeDto> CreateAsync(CreateCargoTypeDto dto, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null ||
                !new[] { SystemRole.admin, SystemRole.superadmin, SystemRole.monitor }.Contains(user.Role))
            {
                _logger.LogWarning("User {UserId} is not authorized to create cargo types", currentUserId);
                throw new AppException("شما دسترسی لازم برای افزودن نوع بار را ندارید.");
            }

            _logger.LogInformation("User {UserId} is creating a new cargo type with name: {Name}", currentUserId,
                dto.Name);

            if (await _context.CargoTypes.AnyAsync(c => c.Name == dto.Name.Trim()))
                throw new AppException("نوع بار با این نام قبلاً ثبت شده است.");

            var entity = new Entities.CargoType
            {
                Name = dto.Name.Trim()
            };

            _context.CargoTypes.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cargo type created with Id {Id}", entity.Id);

            return new CargoTypeDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        public async Task<CargoTypeDto> UpdateAsync(long id, UpdateCargoTypeDto dto, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null ||
                !new[] { SystemRole.admin, SystemRole.superadmin, SystemRole.monitor }.Contains(user.Role))
            {
                _logger.LogWarning("User {UserId} is not authorized to update cargo types", currentUserId);
                throw new AppException("شما دسترسی لازم برای ویرایش نوع بار را ندارید.");
            }

            _logger.LogInformation("User {UserId} is updating cargo type with Id: {Id}", currentUserId, id);

            var cargoType = await _context.CargoTypes.FindAsync(id);
            if (cargoType == null)
            {
                _logger.LogWarning("Cargo type with Id {Id} not found", id);
                throw new NotFoundException("نوع بار یافت نشد.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                string trimmedName = dto.Name.Trim();
                if (await _context.CargoTypes.AnyAsync(c => c.Id != id && c.Name == trimmedName))
                {
                    throw new AppException("نوع بار دیگری با این نام وجود دارد.");
                }

                cargoType.Name = trimmedName;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cargo type with Id {Id} updated successfully", id);

            return new CargoTypeDto
            {
                Id = cargoType.Id,
                Name = cargoType.Name
            };
        }

        public async Task<bool> DeleteAsync(long id, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !new[] { SystemRole.admin, SystemRole.superadmin, SystemRole.monitor }.Contains(user.Role))
            {
                _logger.LogWarning("User {UserId} is not authorized to delete cargo types", currentUserId);
                throw new AppException("شما دسترسی لازم برای حذف نوع بار را ندارید.");
            }

            _logger.LogInformation("User {UserId} is attempting to delete cargo type with Id: {Id}", currentUserId, id);

            var cargoType = await _context.CargoTypes.FindAsync(id);
            if (cargoType == null)
            {
                _logger.LogWarning("Cargo type with Id {Id} not found", id);
                return false;
            }

            _context.CargoTypes.Remove(cargoType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cargo type with Id {Id} was successfully deleted", id);
            return true;
        }

        public async Task<IEnumerable<CargoTypeDto>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all cargo types");

            var types = await _context.CargoTypes
                .OrderBy(t => t.Name)
                .ToListAsync();

            return types.Select(t => new CargoTypeDto
            {
                Id = t.Id,
                Name = t.Name
            });
        }


    }
}
