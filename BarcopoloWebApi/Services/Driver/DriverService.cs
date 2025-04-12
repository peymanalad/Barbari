using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Driver;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BarcopoloWebApi.Services
{
    public class DriverService : IDriverService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<DriverService> _logger;

        public DriverService(DataBaseContext context, ILogger<DriverService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DriverDto> CreateAsync(CreateDriverDto dto, long currentUserId)
        {
            _logger.LogInformation("در حال ایجاد راننده برای شخص با شناسه {PersonId} توسط کاربر {UserId}", dto.PersonId, currentUserId);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || !(currentUser.Role == SystemRole.admin || currentUser.Role == SystemRole.superadmin))
            {
                _logger.LogWarning("کاربر {UserId} مجاز به ایجاد راننده نیست", currentUserId);
                throw new AppException("شما مجاز به ایجاد راننده نیستید.");
            }

            var person = await _context.Persons.FirstOrDefaultAsync(p => p.Id == dto.PersonId);
            if (person == null)
            {
                _logger.LogWarning("شخصی با شناسه {PersonId} یافت نشد", dto.PersonId);
                throw new AppException("شخص مورد نظر یافت نشد.");
            }

            var existingDriver = await _context.Drivers.AnyAsync(d => d.PersonId == dto.PersonId);
            if (existingDriver)
            {
                _logger.LogWarning("راننده‌ای قبلاً برای شخص {PersonId} ثبت شده است", dto.PersonId);
                throw new AppException("برای این شخص قبلاً راننده ثبت شده است.");
            }

            var driver = new Driver
            {
                PersonId = dto.PersonId,
                SmartCardCode = dto.SmartCardCode,
                IdentificationNumber = dto.IdentificationNumber,
                LicenseNumber = dto.LicenseNumber,
                LicenseIssuePlace = dto.LicenseIssuePlace,
                LicenseIssueDate = dto.LicenseIssueDate,
                LicenseExpiryDate = dto.LicenseExpiryDate,
                InsuranceNumber = dto.InsuranceNumber,
                HasViolations = dto.HasViolations
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            _logger.LogInformation("راننده با شناسه {DriverId} برای شخص {PersonId} ایجاد شد", driver.Id, dto.PersonId);

            return new DriverDto
            {
                Id = driver.Id,
                SmartCardCode = driver.SmartCardCode,
                IdentificationNumber = driver.IdentificationNumber,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                HasViolations = driver.HasViolations,
                FullName = $"{person.FirstName} {person.LastName}",
                PhoneNumber = person.PhoneNumber
            };
        }


        public async Task<DriverDto> UpdateAsync(long id, UpdateDriverDto dto, long currentUserId)
        {
            _logger.LogInformation("در حال به‌روزرسانی راننده با شناسه {DriverId} توسط کاربر {UserId}", id, currentUserId);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || !(currentUser.Role == SystemRole.admin || currentUser.Role == SystemRole.superadmin))
            {
                _logger.LogWarning("کاربر {UserId} مجاز به ویرایش راننده نیست", currentUserId);
                throw new AppException("شما مجاز به ویرایش راننده نیستید.");
            }

            var driver = await _context.Drivers
                .Include(d => d.Person)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                _logger.LogWarning("راننده‌ای با شناسه {DriverId} یافت نشد", id);
                throw new AppException("راننده یافت نشد.");
            }

            if (!string.IsNullOrWhiteSpace(dto.SmartCardCode))
                driver.SmartCardCode = dto.SmartCardCode;

            if (!string.IsNullOrWhiteSpace(dto.IdentificationNumber))
                driver.IdentificationNumber = dto.IdentificationNumber;

            if (!string.IsNullOrWhiteSpace(dto.LicenseNumber))
                driver.LicenseNumber = dto.LicenseNumber;

            if (!string.IsNullOrWhiteSpace(dto.LicenseIssuePlace))
                driver.LicenseIssuePlace = dto.LicenseIssuePlace;

            if (dto.LicenseIssueDate.HasValue)
                driver.LicenseIssueDate = dto.LicenseIssueDate.Value;

            if (dto.LicenseExpiryDate.HasValue)
                driver.LicenseExpiryDate = dto.LicenseExpiryDate.Value;

            if (!string.IsNullOrWhiteSpace(dto.InsuranceNumber))
                driver.InsuranceNumber = dto.InsuranceNumber;

            if (dto.HasViolations.HasValue)
                driver.HasViolations = dto.HasViolations.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("راننده با شناسه {DriverId} با موفقیت ویرایش شد", driver.Id);

            return new DriverDto
            {
                Id = driver.Id,
                SmartCardCode = driver.SmartCardCode,
                IdentificationNumber = driver.IdentificationNumber,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                HasViolations = driver.HasViolations,
                FullName = $"{driver.Person.FirstName} {driver.Person.LastName}",
                PhoneNumber = driver.Person.PhoneNumber
            };
        }

        public async Task<bool> DeleteAsync(long id, long currentUserId)
        {
            _logger.LogInformation("در حال حذف راننده با شناسه {DriverId} توسط کاربر {UserId}", id, currentUserId);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || !(currentUser.Role == SystemRole.admin || currentUser.Role == SystemRole.superadmin))
            {
                _logger.LogWarning("کاربر {UserId} مجاز به حذف راننده نیست", currentUserId);
                throw new AppException("شما مجاز به حذف راننده نیستید.");
            }

            var driver = await _context.Drivers
                .Include(d => d.Vehicles)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                _logger.LogWarning("راننده‌ای با شناسه {DriverId} یافت نشد", id);
                return false;
            }

            if (driver.Vehicles.Any())
            {
                _logger.LogWarning("راننده {DriverId} به وسایل نقلیه متصل است و امکان حذف وجود ندارد", id);
                throw new AppException("این راننده به یک یا چند وسیله نقلیه اختصاص داده شده و امکان حذف ندارد.");
            }

            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();

            _logger.LogInformation("راننده با شناسه {DriverId} با موفقیت حذف شد", id);
            return true;
        }

        public async Task AssignToVehicleAsync(long driverId, long vehicleId, long currentUserId)
        {
            _logger.LogInformation("در حال اختصاص راننده {DriverId} به وسیله {VehicleId} توسط کاربر {UserId}", driverId, vehicleId, currentUserId);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || !(currentUser.Role == SystemRole.admin || currentUser.Role == SystemRole.superadmin))
            {
                _logger.LogWarning("کاربر {UserId} مجاز به اختصاص راننده نیست", currentUserId);
                throw new AppException("شما مجاز به اختصاص راننده نیستید.");
            }

            var driver = await _context.Drivers
                .Include(d => d.Person)
                .FirstOrDefaultAsync(d => d.Id == driverId);

            if (driver == null)
            {
                _logger.LogWarning("راننده‌ای با شناسه {DriverId} یافت نشد", driverId);
                throw new AppException("راننده مورد نظر یافت نشد.");
            }

            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
            {
                _logger.LogWarning("وسیله‌ای با شناسه {VehicleId} یافت نشد", vehicleId);
                throw new AppException("وسیله نقلیه مورد نظر یافت نشد.");
            }

            vehicle.DriverId = driver.PersonId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("راننده {DriverId} با شخص {PersonId} به وسیله {VehicleId} اختصاص یافت", driverId, driver.PersonId, vehicleId);
        }

        public async Task<DriverDto> GetByIdAsync(long id, long currentUserId)
        {
            _logger.LogInformation("در حال دریافت راننده با شناسه {DriverId} توسط کاربر {UserId}", id, currentUserId);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || !(currentUser.Role == SystemRole.admin || currentUser.Role == SystemRole.superadmin))
            {
                _logger.LogWarning("کاربر {UserId} مجاز به مشاهده راننده نیست", currentUserId);
                throw new AppException("شما مجاز به مشاهده راننده‌ها نیستید.");
            }

            var driver = await _context.Drivers
                .Include(d => d.Person)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                _logger.LogWarning("راننده‌ای با شناسه {DriverId} یافت نشد", id);
                throw new AppException("راننده مورد نظر یافت نشد.");
            }

            var dto = new DriverDto
            {
                Id = driver.Id,
                SmartCardCode = driver.SmartCardCode,
                IdentificationNumber = driver.IdentificationNumber,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                HasViolations = driver.HasViolations,
                FullName = $"{driver.Person.FirstName} {driver.Person.LastName}",
                PhoneNumber = driver.Person.PhoneNumber
            };

            _logger.LogInformation("راننده {DriverId} با موفقیت بازیابی شد", id);
            return dto;
        }

        public async Task<IEnumerable<DriverDto>> GetAllAsync(long currentUserId)
        {
            _logger.LogInformation("کاربر {UserId} در حال دریافت لیست راننده‌ها است", currentUserId);

            var currentUser = await _context.Persons.FindAsync(currentUserId);
            if (currentUser == null || !(currentUser.Role == SystemRole.admin || currentUser.Role == SystemRole.superadmin))
            {
                _logger.LogWarning("کاربر {UserId} اجازه مشاهده لیست راننده‌ها را ندارد", currentUserId);
                throw new AppException("شما مجاز به مشاهده راننده‌ها نیستید.");
            }

            var drivers = await _context.Drivers
                .Include(d => d.Person)
                .ToListAsync();

            var result = drivers.Select(driver => new DriverDto
            {
                Id = driver.Id,
                SmartCardCode = driver.SmartCardCode,
                IdentificationNumber = driver.IdentificationNumber,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                HasViolations = driver.HasViolations,
                FullName = $"{driver.Person.FirstName} {driver.Person.LastName}",
                PhoneNumber = driver.Person.PhoneNumber
            }).ToList();

            _logger.LogInformation("تعداد {Count} راننده دریافت شد", result.Count);

            return result;
        }


    }
}
