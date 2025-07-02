using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Bargir;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarcopoloWebApi.Services
{
    public class BargirService : IBargirService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<BargirService> _logger;

        public BargirService(DataBaseContext context, ILogger<BargirService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BargirDto> CreateAsync(CreateBargirDto dto, long currentUserId)
        {
            _logger.LogInformation("User {UserId} requested to create a Bargir", currentUserId);

            await EnsureAdminAccess(currentUserId);

            if (dto.MaxCapacity < dto.MinCapacity)
            {
                _logger.LogWarning("Invalid capacity range: Min = {Min}, Max = {Max}", dto.MinCapacity, dto.MaxCapacity);
                throw new AppException("حداکثر ظرفیت نمی‌تواند کمتر از حداقل ظرفیت باشد.");
            }

            Vehicle? vehicle = null;
            if (dto.VehicleId.HasValue)
            {
                vehicle = await _context.Vehicles.FindAsync(dto.VehicleId.Value);
                if (vehicle == null)
                    throw new NotFoundException("وسیله نقلیه مورد نظر یافت نشد.");

                if (vehicle.IsVan)
                    throw new AppException("نمی‌توان بارگیر را به خودرو وانت اختصاص داد.");

                var existing = await _context.Bargirs.AnyAsync(b => b.VehicleId == vehicle.Id);
                if (existing)
                    throw new AppException("برای این وسیله نقلیه قبلاً بارگیر ثبت شده است.");
            }

            var bargir = new Bargir
            {
                Name = dto.Name.Trim(),
                MinCapacity = (decimal)dto.MinCapacity,
                MaxCapacity = (decimal)dto.MaxCapacity,
                VehicleId = dto.VehicleId
            };

            _context.Bargirs.Add(bargir);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bargir with Id {BargirId} created by user {UserId}", bargir.Id, currentUserId);

            return await MapToDtoAsync(bargir.Id);
        }

        public async Task<BargirDto> GetByIdAsync(long id, long currentUserId)
        {
            await EnsureAdminAccess(currentUserId);
            _logger.LogInformation("Retrieving Bargir with Id {Id}", id);

            return await MapToDtoAsync(id)
                   ?? throw new NotFoundException("بارگیر مورد نظر یافت نشد.");
        }

        public async Task<IEnumerable<BargirDto>> GetAllAsync(long currentUserId)
        {
            await EnsureAdminAccess(currentUserId);
            _logger.LogInformation("Retrieving all Bargirs");

            var bargirs = await _context.Bargirs
                .Include(b => b.Vehicle)
                .ToListAsync();

            return bargirs.Select(MapToDto);

        }

        public async Task<BargirDto> UpdateAsync(long id, UpdateBargirDto dto, long currentUserId)
        {
            await EnsureAdminAccess(currentUserId);
            _logger.LogInformation("Updating Bargir with Id {Id}", id);

            var bargir = await _context.Bargirs.FindAsync(id)
                         ?? throw new NotFoundException("بارگیر یافت نشد.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                bargir.Name = dto.Name.Trim();

            if (dto.MinCapacity.HasValue)
                bargir.MinCapacity = (decimal)dto.MinCapacity.Value;

            if (dto.MaxCapacity.HasValue)
                bargir.MaxCapacity = (decimal)dto.MaxCapacity.Value;

            if (dto.VehicleId.HasValue)
            {
                var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId.Value)
                              ?? throw new NotFoundException("وسیله نقلیه یافت نشد.");

                if (vehicle.IsVan)
                    throw new AppException("نمی‌توان بارگیر را به خودرو وانت اختصاص داد.");

                var duplicate = await _context.Bargirs
                    .AnyAsync(b => b.VehicleId == dto.VehicleId && b.Id != id);
                if (duplicate)
                    throw new AppException("برای این وسیله نقلیه قبلاً بارگیر ثبت شده است.");

                bargir.VehicleId = dto.VehicleId;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Bargir with Id {Id} updated successfully", id);

            return await MapToDtoAsync(id);
        }

        public async Task<bool> DeleteAsync(long id, long currentUserId)
        {
            await EnsureAdminAccess(currentUserId);
            _logger.LogInformation("Deleting Bargir with Id {Id}", id);

            var bargir = await _context.Bargirs.FindAsync(id);
            if (bargir == null)
            {
                _logger.LogWarning("Bargir with Id {Id} not found", id);
                return false;
            }

            _context.Bargirs.Remove(bargir);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bargir with Id {Id} deleted", id);
            return true;
        }

        public async Task AssignToVehicleAsync(long bargirId, long vehicleId, long currentUserId)
        {
            await EnsureAdminAccess(currentUserId);
            _logger.LogInformation("Assigning Bargir {BargirId} to Vehicle {VehicleId}", bargirId, vehicleId);

            var bargir = await _context.Bargirs.FindAsync(bargirId)
                         ?? throw new NotFoundException("بارگیر یافت نشد.");

            var vehicle = await _context.Vehicles.FindAsync(vehicleId)
                          ?? throw new NotFoundException("وسیله نقلیه یافت نشد.");

            if (vehicle.IsVan)
                throw new AppException("نمی‌توان بارگیر را به خودرو وانت اختصاص داد.");

            var duplicate = await _context.Bargirs
                .AnyAsync(b => b.VehicleId == vehicleId && b.Id != bargirId);
            if (duplicate)
                throw new AppException("برای این وسیله نقلیه قبلاً بارگیر ثبت شده است.");

            bargir.VehicleId = vehicleId;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Bargir {BargirId} assigned to Vehicle {VehicleId}", bargirId, vehicleId);
        }

        private async Task EnsureAdminAccess(long userId)
        {
            var user = await _context.Persons.FindAsync(userId)
                       ?? throw new ForbiddenAccessException("کاربر یافت نشد");

            if (!user.IsAdminOrSuperAdmin())
                throw new ForbiddenAccessException("دسترسی غیرمجاز");
        }

        private async Task<BargirDto?> MapToDtoAsync(long id)
        {
            var bargir = await _context.Bargirs
                .Include(b => b.Vehicle)
                .FirstOrDefaultAsync(b => b.Id == id);

            return bargir == null ? null : MapToDto(bargir);
        }

        private static BargirDto MapToDto(Bargir b) => new()
        {
            Id = b.Id,
            Name = b.Name,
            MinCapacity = b.MinCapacity,
            MaxCapacity = b.MaxCapacity,
            VehicleId = b.VehicleId,
            VehiclePlateNumber = b.Vehicle?.PlateNumber,
            PlateIranCode = b.Vehicle?.PlateIranCode,
            PlateThreeDigit = b.Vehicle?.PlateThreeDigit,
            PlateLetter = b.Vehicle?.PlateLetter,
            PlateTwoDigit = b.Vehicle?.PlateTwoDigit
        };

    }
}
