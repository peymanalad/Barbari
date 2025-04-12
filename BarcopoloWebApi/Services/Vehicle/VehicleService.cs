using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Vehicle;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || (currentUser.Role != SystemRole.superadmin && currentUser.Role != SystemRole.admin))
            {
                _logger.LogWarning("User {UserId} is not authorized to create vehicles", currentUserId);
                throw new AppException("شما دسترسی ایجاد وسیله نقلیه را ندارید.");
            }

            if (await _context.Vehicles.AnyAsync(v => v.PlateNumber == dto.PlateNumber))
            {
                _logger.LogWarning("Plate number {PlateNumber} is already in use", dto.PlateNumber);
                throw new AppException("پلاک وارد شده قبلاً ثبت شده است.");
            }

            if (await _context.Vehicles.AnyAsync(v => v.SmartCardCode == dto.SmartCardCode))
            {
                _logger.LogWarning("SmartCardCode {SmartCardCode} is already in use", dto.SmartCardCode);
                throw new AppException("کد کارت هوشمند وارد شده قبلاً ثبت شده است.");
            }

            var vehicle = new Vehicle
            {
                DriverId = dto.DriverId,
                SmartCardCode = dto.SmartCardCode,
                PlateNumber = dto.PlateNumber,
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

            _logger.LogInformation("Vehicle created successfully with Id {VehicleId} by user {UserId}", vehicle.Id, currentUserId);

            return new VehicleDto
            {
                Id = vehicle.Id,
                SmartCardCode = vehicle.SmartCardCode,
                PlateNumber = vehicle.PlateNumber,
                Axles = vehicle.Axles,
                Model = vehicle.Model,
                Color = vehicle.Color,
                Engine = vehicle.Engine,
                Chassis = vehicle.Chassis,
                IsBroken = vehicle.IsBroken,
                IsVan = vehicle.IsVan,
                VanCommission = vehicle.VanCommission,
                DriverId = vehicle.DriverId,
                DriverFullName = vehicle.DriverId.HasValue
                    ? await _context.Persons
                        .Where(p => p.Id == vehicle.DriverId)
                        .Select(p => p.FirstName + " " + p.LastName)
                        .FirstOrDefaultAsync()
                    : null
            };
        }


        public async Task<VehicleDto> GetByIdAsync(long id, long currentUserId)
        {
            _logger.LogInformation("User {UserId} is requesting vehicle by Id {VehicleId}", currentUserId, id);

            var person = await _context.Persons.FindAsync(currentUserId);
            if (person == null || !(person.Role == SystemRole.superadmin || person.Role == SystemRole.admin || person.Role == SystemRole.monitor))
            {
                _logger.LogWarning("User {UserId} is not authorized to view vehicle {VehicleId}", currentUserId, id);
                throw new AppException("شما مجاز به مشاهده این وسیله نقلیه نیستید.");
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                _logger.LogWarning("Vehicle with Id {VehicleId} not found", id);
                throw new AppException("وسیله نقلیه مورد نظر یافت نشد.");
            }

            var dto = new VehicleDto
            {
                Id = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                Model = vehicle.Model,
                Color = vehicle.Color,
                SmartCardCode = vehicle.SmartCardCode,
                Axles = vehicle.Axles,
                Engine = vehicle.Engine,
                Chassis = vehicle.Chassis,
                IsBroken = vehicle.IsBroken,
                IsVan = vehicle.IsVan,
                VanCommission = vehicle.VanCommission,
                DriverId = vehicle.DriverId,
                DriverFullName = vehicle.Driver != null ? vehicle.Driver.Person.GetFullName() : null
            };

            _logger.LogInformation("Vehicle with Id {VehicleId} retrieved successfully by user {UserId}", id, currentUserId);
            return dto;
        }


        public async Task<IEnumerable<VehicleDto>> GetAllAsync(long currentUserId)
        {
            _logger.LogInformation("User {UserId} is requesting all vehicles", currentUserId);

            var person = await _context.Persons.FindAsync(currentUserId);
            if (person == null || !(person.Role == SystemRole.superadmin || person.Role == SystemRole.admin || person.Role == SystemRole.monitor))
            {
                _logger.LogWarning("Unauthorized access by user {UserId} to get all vehicles", currentUserId);
                throw new AppException("شما مجاز به مشاهده لیست وسایل نقلیه نیستید.");
            }

            var vehicles = await _context.Vehicles
                .Include(v => v.Driver)
                .ToListAsync();

            _logger.LogInformation("{Count} vehicles retrieved successfully by user {UserId}", vehicles.Count, currentUserId);

            return vehicles.Select(vehicle => new VehicleDto
            {
                Id = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                Model = vehicle.Model,
                Color = vehicle.Color,
                SmartCardCode = vehicle.SmartCardCode,
                Axles = vehicle.Axles,
                Engine = vehicle.Engine,
                Chassis = vehicle.Chassis,
                IsBroken = vehicle.IsBroken,
                IsVan = vehicle.IsVan,
                VanCommission = vehicle.VanCommission,
                DriverId = vehicle.DriverId,
                DriverFullName = vehicle.Driver != null ? vehicle.Driver.Person.GetFullName() : null
            });
        }

        public async Task<VehicleDto> UpdateAsync(long id, UpdateVehicleDto dto, long currentUserId)
        {
            _logger.LogInformation("User {UserId} is attempting to update vehicle {VehicleId}", currentUserId, id);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || (currentUser.Role != SystemRole.superadmin && currentUser.Role != SystemRole.admin))
            {
                _logger.LogWarning("User {UserId} is not authorized to update vehicles", currentUserId);
                throw new AppException("شما دسترسی ویرایش وسیله نقلیه را ندارید.");
            }

            var vehicle = await _context.Vehicles.Include(v => v.Driver).FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle == null)
            {
                _logger.LogWarning("Vehicle with Id {VehicleId} not found", id);
                throw new AppException("وسیله نقلیه مورد نظر یافت نشد.");
            }

            if (!string.IsNullOrWhiteSpace(dto.PlateNumber) &&
                dto.PlateNumber != vehicle.PlateNumber &&
                await _context.Vehicles.AnyAsync(v => v.PlateNumber == dto.PlateNumber && v.Id != id))
            {
                _logger.LogWarning("Plate number {PlateNumber} already exists", dto.PlateNumber);
                throw new AppException("پلاک وارد شده قبلاً استفاده شده است.");
            }

            if (dto.DriverId.HasValue && dto.DriverId != vehicle.DriverId)
            {
                var driverExists = await _context.Persons.AnyAsync(p => p.Id == dto.DriverId.Value);
                if (!driverExists)
                {
                    _logger.LogWarning("Driver with Id {DriverId} does not exist", dto.DriverId.Value);
                    throw new AppException("راننده یافت نشد.");
                }
                vehicle.DriverId = dto.DriverId;
            }

            if (!string.IsNullOrWhiteSpace(dto.PlateNumber)) vehicle.PlateNumber = dto.PlateNumber;
            if (!string.IsNullOrWhiteSpace(dto.Model)) vehicle.Model = dto.Model;
            if (!string.IsNullOrWhiteSpace(dto.Color)) vehicle.Color = dto.Color;
            if (dto.IsBroken.HasValue) vehicle.IsBroken = dto.IsBroken.Value;
            if (dto.IsVan.HasValue) vehicle.IsVan = dto.IsVan.Value;
            if (dto.VanCommission.HasValue) vehicle.VanCommission = dto.VanCommission;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Vehicle with Id {VehicleId} updated successfully by user {UserId}", id, currentUserId);

            return new VehicleDto
            {
                Id = vehicle.Id,
                SmartCardCode = vehicle.SmartCardCode,
                PlateNumber = vehicle.PlateNumber,
                Axles = vehicle.Axles,
                Model = vehicle.Model,
                Color = vehicle.Color,
                Engine = vehicle.Engine,
                Chassis = vehicle.Chassis,
                IsBroken = vehicle.IsBroken,
                IsVan = vehicle.IsVan,
                VanCommission = vehicle.VanCommission,
                DriverId = vehicle.DriverId,
                DriverFullName = vehicle.Driver != null ? vehicle.Driver.Person.GetFullName() : null
            };
        }

        public async Task<bool> DeleteAsync(long id, long currentUserId)
        {
            _logger.LogInformation("User {UserId} is attempting to delete vehicle {VehicleId}", currentUserId, id);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || (currentUser.Role != SystemRole.superadmin && currentUser.Role != SystemRole.admin))
            {
                _logger.LogWarning("User {UserId} is not authorized to delete vehicles", currentUserId);
                throw new AppException("شما مجاز به حذف وسیله نقلیه نیستید.");
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.OrderVehicles)
                .Include(v => v.WarehouseVehicles)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                _logger.LogWarning("Vehicle with Id {VehicleId} not found", id);
                return false;
            }

            if (vehicle.OrderVehicles.Any())
            {
                _logger.LogWarning("Vehicle {VehicleId} is associated with orders and cannot be deleted", id);
                throw new AppException("این وسیله نقلیه به سفارش‌هایی اختصاص یافته و قابل حذف نیست.");
            }

            if (vehicle.WarehouseVehicles.Any())
            {
                _logger.LogWarning("Vehicle {VehicleId} is assigned to a warehouse and cannot be deleted", id);
                throw new AppException("این وسیله نقلیه به انباری تخصیص داده شده و قابل حذف نیست.");
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Vehicle with Id {VehicleId} deleted successfully by user {UserId}", id, currentUserId);
            return true;
        }

        public async Task<IEnumerable<VehicleDto>> SearchAsync(VehicleFilterDto filter, long currentUserId)
        {
            _logger.LogInformation("User {UserId} is searching vehicles with filters: {@Filters}", currentUserId, filter);

            var person = await _context.Persons.FindAsync(currentUserId);
            if (person == null || !(person.Role == SystemRole.superadmin || person.Role == SystemRole.admin || person.Role == SystemRole.monitor))
            {
                _logger.LogWarning("Unauthorized vehicle search attempt by user {UserId}", currentUserId);
                throw new AppException("شما مجاز به جستجوی وسیله نقلیه نیستید.");
            }

            var query = _context.Vehicles
                .Include(v => v.Driver)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.PlateNumber))
                query = query.Where(v => v.PlateNumber.Contains(filter.PlateNumber));

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

            _logger.LogInformation("Vehicle search by user {UserId} returned {Count} results", currentUserId, vehicles.Count);

            return vehicles.Select(vehicle => new VehicleDto
            {
                Id = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                Model = vehicle.Model,
                Color = vehicle.Color,
                SmartCardCode = vehicle.SmartCardCode,
                Axles = vehicle.Axles,
                Engine = vehicle.Engine,
                Chassis = vehicle.Chassis,
                IsBroken = vehicle.IsBroken,
                IsVan = vehicle.IsVan,
                VanCommission = vehicle.VanCommission,
                DriverId = vehicle.DriverId,
                DriverFullName = vehicle.Driver != null ? vehicle.Driver.Person.GetFullName() : null
            });
        }

        public async Task<IEnumerable<VehicleDto>> GetAvailableAsync(long currentUserId)
        {
            _logger.LogInformation("User {UserId} is retrieving available vehicles", currentUserId);

            var person = await _context.Persons.FindAsync(currentUserId);
            if (person == null || !(person.Role == SystemRole.superadmin || person.Role == SystemRole.admin || person.Role == SystemRole.monitor))
            {
                _logger.LogWarning("Unauthorized access to available vehicles by user {UserId}", currentUserId);
                throw new AppException("شما مجاز به مشاهده لیست وسایل نقلیه در دسترس نیستید.");
            }

            var vehicles = await _context.Vehicles
                .Include(v => v.Driver)
                .Where(v => !v.IsBroken && !v.HasViolations)
                .ToListAsync();

            _logger.LogInformation("User {UserId} found {Count} available vehicles", currentUserId, vehicles.Count);

            return vehicles.Select(vehicle => new VehicleDto
            {
                Id = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                Model = vehicle.Model,
                Color = vehicle.Color,
                SmartCardCode = vehicle.SmartCardCode,
                Axles = vehicle.Axles,
                Engine = vehicle.Engine,
                Chassis = vehicle.Chassis,
                IsBroken = vehicle.IsBroken,
                IsVan = vehicle.IsVan,
                VanCommission = vehicle.VanCommission,
                DriverId = vehicle.DriverId,
                DriverFullName = vehicle.Driver != null ? vehicle.Driver.Person.GetFullName() : null
            });
        }

        public async Task<IEnumerable<VehicleDto>> GetByDriverIdAsync(long driverId, long currentUserId)
        {
            _logger.LogInformation("User {UserId} is retrieving vehicles for Driver {DriverId}", currentUserId, driverId);

            var person = await _context.Persons.FindAsync(currentUserId);
            if (person == null || !(person.Role == SystemRole.superadmin || person.Role == SystemRole.admin || person.Role == SystemRole.monitor))
            {
                _logger.LogWarning("Unauthorized access to vehicles by driver by user {UserId}", currentUserId);
                throw new AppException("شما مجاز به مشاهده وسایل نقلیه این راننده نیستید.");
            }

            var vehicles = await _context.Vehicles
                .Include(v => v.Driver)
                .Where(v => v.DriverId == driverId)
                .ToListAsync();

            _logger.LogInformation("Found {Count} vehicles for Driver {DriverId}", vehicles.Count, driverId);

            return vehicles.Select(vehicle => new VehicleDto
            {
                Id = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                Model = vehicle.Model,
                Color = vehicle.Color,
                SmartCardCode = vehicle.SmartCardCode,
                Axles = vehicle.Axles,
                Engine = vehicle.Engine,
                Chassis = vehicle.Chassis,
                IsBroken = vehicle.IsBroken,
                IsVan = vehicle.IsVan,
                VanCommission = vehicle.VanCommission,
                DriverId = vehicle.DriverId,
                DriverFullName = vehicle.Driver != null ? vehicle.Driver.Person.GetFullName() : null
            });
        }

        public async Task<int> GetBrokenCountAsync(long currentUserId)
        {
            _logger.LogInformation("User {UserId} is requesting broken vehicle count", currentUserId);

            var person = await _context.Persons.FindAsync(currentUserId);
            if (person == null || !(person.Role == SystemRole.superadmin || person.Role == SystemRole.admin || person.Role == SystemRole.monitor))
            {
                _logger.LogWarning("Unauthorized access to broken vehicle count by user {UserId}", currentUserId);
                throw new AppException("شما مجاز به مشاهده تعداد وسایل نقلیه معیوب نیستید.");
            }

            var count = await _context.Vehicles.CountAsync(v => v.IsBroken);

            _logger.LogInformation("Broken vehicle count: {Count}", count);
            return count;
        }




    }
}
