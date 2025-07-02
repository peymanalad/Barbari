using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Vehicle;
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
    public class VehicleService : IVehicleService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(DataBaseContext context, ILogger<VehicleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto, long currentUserId)
        {
            _logger.LogInformation("User {UserId} is attempting to create a new vehicle", currentUserId);

            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !(user.Role == SystemRole.superadmin || user.Role == SystemRole.admin))
                throw new AppException("شما دسترسی ایجاد وسیله نقلیه را ندارید.");

            if (await _context.Vehicles.AnyAsync(v => v.PlateIranCode == dto.PlateIranCode
                                                    && v.PlateLetter == dto.PlateLetter
                                                    && v.PlateThreeDigit == dto.PlateThreeDigit
                                                    && v.PlateTwoDigit == dto.PlateTwoDigit))
                throw new AppException("این پلاک قبلاً ثبت شده است.");

            if (await _context.Vehicles.AnyAsync(v => v.SmartCardCode == dto.SmartCardCode))
                throw new AppException("کد کارت هوشمند وارد شده قبلاً ثبت شده است.");

            var vehicle = new Vehicle
            {
                DriverId = dto.DriverId,
                SmartCardCode = dto.SmartCardCode,
                PlateIranCode = dto.PlateIranCode,
                PlateLetter = dto.PlateLetter,
                PlateThreeDigit = dto.PlateThreeDigit,
                PlateTwoDigit = dto.PlateTwoDigit,
                Axles = dto.Axles,
                Model = dto.Model,
                Color = dto.Color,
                Engine = dto.Engine,
                Chassis = dto.Chassis,
                IsBroken = dto.IsBroken,
                IsVan = dto.IsVan,
                VanCommission = dto.VanCommission
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return await MapToDtoAsync(vehicle);
        }

        public async Task<VehicleDto> GetByIdAsync(long id, long currentUserId)
        {
            await EnsureReadAccess(currentUserId);

            var vehicle = await _context.Vehicles
                .Include(v => v.Driver).ThenInclude(d => d.Person)
                .FirstOrDefaultAsync(v => v.Id == id)
                ?? throw new NotFoundException("وسیله نقلیه یافت نشد.");

            return await MapToDtoAsync(vehicle);
        }

        public async Task<IEnumerable<VehicleDto>> GetAllAsync(long currentUserId)
        {
            await EnsureReadAccess(currentUserId);

            var vehicles = await _context.Vehicles
                .Include(v => v.Driver).ThenInclude(d => d.Person)
                .ToListAsync();

            return vehicles.Select(MapToDto);
        }

        public async Task<VehicleDto> UpdateAsync(long id, UpdateVehicleDto dto, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !(user.Role == SystemRole.superadmin || user.Role == SystemRole.admin))
                throw new AppException("شما دسترسی ویرایش وسیله نقلیه را ندارید.");

            var vehicle = await _context.Vehicles.Include(v => v.Driver).ThenInclude(d => d.Person)
                            .FirstOrDefaultAsync(v => v.Id == id)
                            ?? throw new NotFoundException("وسیله نقلیه یافت نشد.");

            if (dto.DriverId.HasValue)
            {
                var driver = await _context.Drivers.Include(d => d.Person)
                                .FirstOrDefaultAsync(d => d.Id == dto.DriverId);
                if (driver == null)
                    throw new NotFoundException("راننده یافت نشد.");
                vehicle.DriverId = dto.DriverId;
            }

            if (!string.IsNullOrWhiteSpace(dto.SmartCardCode))
                vehicle.SmartCardCode = dto.SmartCardCode;

            vehicle.PlateIranCode = dto.PlateIranCode ?? vehicle.PlateIranCode;
            vehicle.PlateLetter = dto.PlateLetter ?? vehicle.PlateLetter;
            vehicle.PlateThreeDigit = dto.PlateThreeDigit ?? vehicle.PlateThreeDigit;
            vehicle.PlateTwoDigit = dto.PlateTwoDigit ?? vehicle.PlateTwoDigit;
            vehicle.Axles = dto.Axles ?? vehicle.Axles;
            vehicle.Model = dto.Model ?? vehicle.Model;
            vehicle.Color = dto.Color ?? vehicle.Color;
            vehicle.Engine = dto.Engine ?? vehicle.Engine;
            vehicle.Chassis = dto.Chassis ?? vehicle.Chassis;
            vehicle.IsBroken = dto.IsBroken ?? vehicle.IsBroken;
            vehicle.IsVan = dto.IsVan ?? vehicle.IsVan;
            vehicle.VanCommission = dto.VanCommission ?? vehicle.VanCommission;

            await _context.SaveChangesAsync();

            return await MapToDtoAsync(vehicle);
        }

        public async Task<bool> DeleteAsync(long id, long currentUserId)
        {
            var user = await _context.Persons.FindAsync(currentUserId);
            if (user == null || !(user.Role == SystemRole.superadmin || user.Role == SystemRole.admin))
                throw new AppException("شما مجاز به حذف وسیله نقلیه نیستید.");

            var vehicle = await _context.Vehicles
                .Include(v => v.OrderVehicles)
                .Include(v => v.WarehouseVehicles)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
                return false;

            if (vehicle.OrderVehicles.Any())
                throw new AppException("وسیله نقلیه به سفارش‌هایی اختصاص یافته است.");

            if (vehicle.WarehouseVehicles.Any())
                throw new AppException("وسیله نقلیه به انبار تخصیص داده شده است.");

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<VehicleDto>> SearchAsync(VehicleFilterDto filter, long currentUserId)
        {
            await EnsureReadAccess(currentUserId);

            var query = _context.Vehicles
                .Include(v => v.Driver).ThenInclude(d => d.Person)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.PlateNumber))
                query = query.Where(v => v.GetFormattedPlateNumber().Contains(filter.PlateNumber));

            if (!string.IsNullOrWhiteSpace(filter.Model))
                query = query.Where(v => v.Model.Contains(filter.Model));

            if (!string.IsNullOrWhiteSpace(filter.Color))
                query = query.Where(v => v.Color.Contains(filter.Color));

            if (filter.IsVan.HasValue)
                query = query.Where(v => v.IsVan == filter.IsVan);

            if (filter.IsBroken.HasValue)
                query = query.Where(v => v.IsBroken == filter.IsBroken);

            if (filter.DriverId.HasValue)
                query = query.Where(v => v.DriverId == filter.DriverId);

            var vehicles = await query.ToListAsync();
            return vehicles.Select(MapToDto);
        }

        public async Task<IEnumerable<VehicleDto>> GetAvailableAsync(long currentUserId)
        {
            await EnsureReadAccess(currentUserId);

            var vehicles = await _context.Vehicles
                .Include(v => v.Driver).ThenInclude(d => d.Person)
                .Where(v => !v.IsBroken && !v.HasViolations)
                .ToListAsync();

            return vehicles.Select(MapToDto);
        }

        public async Task<IEnumerable<VehicleDto>> GetByDriverIdAsync(long driverId, long currentUserId)
        {
            await EnsureReadAccess(currentUserId);

            var vehicles = await _context.Vehicles
                .Include(v => v.Driver).ThenInclude(d => d.Person)
                .Where(v => v.DriverId == driverId)
                .ToListAsync();

            return vehicles.Select(MapToDto);
        }

        public async Task<int> GetBrokenCountAsync(long currentUserId)
        {
            await EnsureReadAccess(currentUserId);
            return await _context.Vehicles.CountAsync(v => v.IsBroken);
        }

        private VehicleDto MapToDto(Vehicle v) => new()
        {
            Id = v.Id,
            SmartCardCode = v.SmartCardCode,
            PlateIranCode = v.PlateIranCode,
            PlateLetter = v.PlateLetter,
            PlateThreeDigit = v.PlateThreeDigit,
            PlateTwoDigit = v.PlateTwoDigit,
            PlateNumber = v.GetFormattedPlateNumber(),
            Axles = v.Axles,
            Model = v.Model,
            Color = v.Color,
            Engine = v.Engine,
            Chassis = v.Chassis,
            IsBroken = v.IsBroken,
            IsVan = v.IsVan,
            VanCommission = v.VanCommission,
            DriverId = v.DriverId,
            DriverFullName = v.Driver?.Person?.GetFullName()
        };

        private async Task<VehicleDto> MapToDtoAsync(Vehicle v)
        {
            return MapToDto(v);
        }

        private async Task EnsureReadAccess(long userId)
        {
            var user = await _context.Persons.FindAsync(userId);
            if (user == null || !(user.Role == SystemRole.superadmin || user.Role == SystemRole.admin || user.Role == SystemRole.monitor))
                throw new AppException("دسترسی غیرمجاز به اطلاعات وسایل نقلیه.");
        }
    }
}
