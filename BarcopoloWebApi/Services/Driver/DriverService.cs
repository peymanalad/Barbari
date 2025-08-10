using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Driver;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace BarcopoloWebApi.Services;

public class DriverService : IDriverService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<DriverService> _logger;
    private readonly IPasswordHasher<Entities.Person> _passwordHasher;


    public DriverService(DataBaseContext context, ILogger<DriverService> logger , IPasswordHasher<Entities.Person> passwordHasher)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<DriverDto> CreateAsync(CreateDriverDto dto, long currentUserId)
    {
        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        bool isAdmin = currentUser.IsAdminOrSuperAdmin();

        var duplicateErrors = new List<string>();
        if (!string.IsNullOrWhiteSpace(dto.SmartCardCode))
        {
            bool exists = await _context.Drivers.AnyAsync(d => d.SmartCardCode == dto.SmartCardCode);
            if (exists) duplicateErrors.Add("کد هوشمند وارد شده قبلاً استفاده شده است.");
        }

        if (!string.IsNullOrWhiteSpace(dto.IdentificationNumber))
        {
            bool exists = await _context.Drivers.AnyAsync(d => d.IdentificationNumber == dto.IdentificationNumber);
            if (exists) duplicateErrors.Add("شماره شناسنامه وارد شده قبلاً استفاده شده است.");
        }

        if (!string.IsNullOrWhiteSpace(dto.LicenseNumber))
        {
            bool exists = await _context.Drivers.AnyAsync(d => d.LicenseNumber == dto.LicenseNumber);
            if (exists) duplicateErrors.Add("شماره گواهینامه وارد شده قبلاً استفاده شده است.");
        }

        if (!string.IsNullOrWhiteSpace(dto.NationalCode))
        {
            bool exists = await _context.Persons.AnyAsync(p => p.NationalCode == dto.NationalCode);
            if (exists) duplicateErrors.Add("کد ملی وارد شده قبلاً در سیستم وجود دارد.");
        }

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            bool exists = await _context.Persons.AnyAsync(p => p.PhoneNumber == dto.PhoneNumber);
            if (exists) duplicateErrors.Add("شماره موبایل وارد شده قبلاً در سیستم وجود دارد.");
        }

        if (duplicateErrors.Any())
            throw new AppException("خطاهای تکراری:\n" + string.Join("\n", duplicateErrors));

        if (dto.PersonId.HasValue)
        {
            if (!isAdmin)
                throw new ForbiddenAccessException("فقط مدیران می‌توانند راننده‌ای برای شخص دیگر ایجاد کنند.");

            var person = await _context.Persons.FindAsync(dto.PersonId.Value)
                ?? throw new NotFoundException("شخص وارد شده وجود ندارد.");

            bool exists = await _context.Drivers.AnyAsync(d => d.PersonId == dto.PersonId.Value);
            if (exists)
                throw new AppException("برای این شخص قبلاً راننده ثبت شده است.");

            var driver = CreateDriverEntity(dto, dto.PersonId.Value);
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            _logger.LogInformation("راننده برای شخص {PersonId} توسط ادمین ایجاد شد", dto.PersonId);
            return MapToDto(driver, person);
        }
        else
        {
            // ثبت‌نام توسط خود راننده
            if (string.IsNullOrWhiteSpace(dto.NationalCode) || string.IsNullOrWhiteSpace(dto.PhoneNumber))
                throw new AppException("کد ملی و شماره تماس الزامی هستند.");

            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.NationalCode == dto.NationalCode &&
                    p.PhoneNumber == dto.PhoneNumber);

            if (person == null)
            {
                person = new Entities.Person
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    NationalCode = dto.NationalCode,
                    PhoneNumber = dto.PhoneNumber,
                    Role = SystemRole.user,
                    IsActive = true
                };

                person.PasswordHash = _passwordHasher.HashPassword(person, dto.NationalCode);

                _context.Persons.Add(person);
                await _context.SaveChangesAsync();
                _logger.LogInformation("شخص جدید برای راننده ساخته شد: {PersonId}", person.Id);
            }

            else
            {
                _logger.LogInformation("شخص با کد ملی و شماره موبایل موجود بود: {PersonId}", person.Id);
            }

            bool exists = await _context.Drivers.AnyAsync(d => d.PersonId == person.Id);
            if (exists)
                throw new AppException("برای این شخص قبلاً راننده ثبت شده است.");

            var driver = CreateDriverEntity(dto, person.Id);
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            _logger.LogInformation("راننده برای شخص {PersonId} ثبت‌نام کرد", person.Id);
            return MapToDto(driver, person);
        }
    }

    public async Task<DriverDto> UpdateAsync(long id, UpdateDriverDto dto, long currentUserId)
    {
        var driver = await _context.Drivers.Include(d => d.Person).FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("راننده یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        bool isOwner = driver.PersonId == currentUserId;
        bool isAdmin = currentUser.IsAdminOrSuperAdmin();

        if (!isAdmin && !isOwner)
            throw new ForbiddenAccessException("شما مجاز به ویرایش این راننده نیستید.");

        if (!isAdmin && (dto.NationalCode != null || dto.PhoneNumber != null))
            throw new ForbiddenAccessException("شما مجاز به تغییر کد ملی یا شماره تماس نیستید.");

        var duplicateErrors = new List<string>();

        if (!string.IsNullOrWhiteSpace(dto.SmartCardCode) && dto.SmartCardCode != driver.SmartCardCode)
        {
            bool exists = await _context.Drivers.AnyAsync(d => d.SmartCardCode == dto.SmartCardCode && d.Id != id);
            if (exists) duplicateErrors.Add("کد هوشمند وارد شده قبلاً استفاده شده است.");
        }

        if (!string.IsNullOrWhiteSpace(dto.IdentificationNumber) && dto.IdentificationNumber != driver.IdentificationNumber)
        {
            bool exists = await _context.Drivers.AnyAsync(d => d.IdentificationNumber == dto.IdentificationNumber && d.Id != id);
            if (exists) duplicateErrors.Add("شماره شناسنامه وارد شده قبلاً استفاده شده است.");
        }

        if (!string.IsNullOrWhiteSpace(dto.LicenseNumber) && dto.LicenseNumber != driver.LicenseNumber)
        {
            bool exists = await _context.Drivers.AnyAsync(d => d.LicenseNumber == dto.LicenseNumber && d.Id != id);
            if (exists) duplicateErrors.Add("شماره گواهینامه وارد شده قبلاً استفاده شده است.");
        }

        if (isAdmin)
        {
            if (!string.IsNullOrWhiteSpace(dto.NationalCode) && dto.NationalCode != driver.Person.NationalCode)
            {
                bool exists = await _context.Persons.AnyAsync(p => p.NationalCode == dto.NationalCode && p.Id != driver.PersonId);
                if (exists) duplicateErrors.Add("کد ملی وارد شده قبلاً استفاده شده است.");
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != driver.Person.PhoneNumber)
            {
                bool exists = await _context.Persons.AnyAsync(p => p.PhoneNumber == dto.PhoneNumber && p.Id != driver.PersonId);
                if (exists) duplicateErrors.Add("شماره موبایل وارد شده قبلاً استفاده شده است.");
            }
        }

        if (duplicateErrors.Any())
            throw new AppException("خطاهای تکراری:\n" + string.Join("\n", duplicateErrors));

        if (!string.IsNullOrWhiteSpace(dto.SmartCardCode)) driver.SmartCardCode = dto.SmartCardCode;
        if (!string.IsNullOrWhiteSpace(dto.IdentificationNumber)) driver.IdentificationNumber = dto.IdentificationNumber;
        if (!string.IsNullOrWhiteSpace(dto.LicenseNumber)) driver.LicenseNumber = dto.LicenseNumber;
        if (!string.IsNullOrWhiteSpace(dto.LicenseIssuePlace)) driver.LicenseIssuePlace = dto.LicenseIssuePlace;
        if (dto.LicenseIssueDate.HasValue) driver.LicenseIssueDate = dto.LicenseIssueDate.Value;
        if (dto.LicenseExpiryDate.HasValue) driver.LicenseExpiryDate = dto.LicenseExpiryDate.Value;
        if (!string.IsNullOrWhiteSpace(dto.InsuranceNumber)) driver.InsuranceNumber = dto.InsuranceNumber;
        if (dto.HasViolations.HasValue) driver.HasViolations = dto.HasViolations.Value;

        await _context.SaveChangesAsync();
        _logger.LogInformation("راننده {Id} توسط کاربر {UserId} ویرایش شد", id, currentUserId);

        return MapToDto(driver, driver.Person);
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("شما مجاز به حذف راننده نیستید.");

        var driver = await _context.Drivers
            .Include(d => d.Vehicles)
            .Include(d => d.Person)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver == null)
            throw new NotFoundException("راننده یافت نشد.");

        if (driver.Vehicles.Any())
            throw new AppException("راننده به وسایل نقلیه متصل است و قابل حذف نیست.");

        _context.Drivers.Remove(driver);
        await _context.SaveChangesAsync();
        _logger.LogInformation("راننده {Id} حذف شد", id);

        return true;
    }

    public async Task<DriverDto> GetByIdAsync(long id, long currentUserId)
    {
        var driver = await _context.Drivers.Include(d => d.Person).FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("راننده یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (!(currentUser.IsAdminOrSuperAdmin() || driver.PersonId == currentUserId))
            throw new ForbiddenAccessException("شما مجاز به مشاهده این راننده نیستید.");

        return MapToDto(driver, driver.Person);
    }

    public async Task<IEnumerable<DriverDto>> GetAllAsync(long currentUserId)
    {
        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("شما مجاز به مشاهده راننده‌ها نیستید.");

        var drivers = await _context.Drivers.Include(d => d.Person).ToListAsync();

        return drivers.Select(d => MapToDto(d, d.Person));
    }

    public async Task<DriverDto> SelfRegisterAsync(SelfRegisterDriverDto dto)
    {
        _logger.LogInformation("شروع ثبت‌نام راننده توسط خودش: {Phone} - {NationalCode}", dto.PhoneNumber, dto.NationalCode);

        // 1. بررسی وجود شخص بر اساس شماره ملی و موبایل
        var person = await _context.Persons.FirstOrDefaultAsync(p =>
            p.NationalCode == dto.NationalCode && p.PhoneNumber == dto.PhoneNumber);

        if (person == null)
        {
            person = new Entities.Person
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                NationalCode = dto.NationalCode,
                Role = SystemRole.user,
                PasswordHash = "" // این قسمت باید در فرآیند واقعی رمزگذاری شود
            };

            _context.Persons.Add(person);
            await _context.SaveChangesAsync();

            _logger.LogInformation("شخص جدید با نقش user ساخته شد: {PersonId}", person.Id);
        }
        else
        {
            // بررسی وجود راننده برای این شخص
            var existingDriver = await _context.Drivers.AnyAsync(d => d.PersonId == person.Id);
            if (existingDriver)
            {
                _logger.LogWarning("برای این شخص قبلاً راننده ثبت شده است: {PersonId}", person.Id);
                throw new AppException("برای این شخص قبلاً راننده ثبت شده است.");
            }
        }

        // 2. ایجاد راننده
        var driver = new Driver
        {
            PersonId = person.Id,
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

        _logger.LogInformation("راننده جدید با شناسه {DriverId} ثبت شد", driver.Id);

        return new DriverDto
        {
            Id = driver.Id,
            SmartCardCode = driver.SmartCardCode,
            IdentificationNumber = driver.IdentificationNumber,
            LicenseNumber = driver.LicenseNumber,
            LicenseExpiryDate = driver.LicenseExpiryDate,
            HasViolations = driver.HasViolations,
            FullName = person.GetFullName(),
            PhoneNumber = person.PhoneNumber
        };
    }
    public async Task AssignToVehicleAsync(long driverId, long vehicleId, long currentUserId)
    {
        var currentUser = await _context.Persons.FindAsync(currentUserId)
                          ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("شما مجاز به انجام این عملیات نیستید.");

        var driver = await _context.Drivers.FindAsync(driverId)
                     ?? throw new NotFoundException("راننده موردنظر یافت نشد.");

        var vehicle = await _context.Vehicles.FindAsync(vehicleId)
                      ?? throw new NotFoundException("وسیله نقلیه موردنظر یافت نشد.");

        vehicle.DriverId = driver.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("راننده {DriverId} به وسیله نقلیه {VehicleId} توسط کاربر {UserId} اختصاص یافت.",
            driverId, vehicleId, currentUserId);
    }



    // ------------------ Helper Methods ------------------

    private Driver CreateDriverEntity(CreateDriverDto dto, long personId)
    {
        return new Driver
        {
            PersonId = personId,
            SmartCardCode = dto.SmartCardCode,
            IdentificationNumber = dto.IdentificationNumber,
            LicenseNumber = dto.LicenseNumber,
            LicenseIssuePlace = dto.LicenseIssuePlace,
            LicenseIssueDate = dto.LicenseIssueDate,
            LicenseExpiryDate = dto.LicenseExpiryDate,
            InsuranceNumber = dto.InsuranceNumber,
            HasViolations = dto.HasViolations
        };
    }

    private DriverDto MapToDto(Driver driver, Entities.Person person)
    {
        return new DriverDto
        {
            Id = driver.Id,
            SmartCardCode = driver.SmartCardCode,
            IdentificationNumber = driver.IdentificationNumber,
            LicenseNumber = driver.LicenseNumber,
            LicenseExpiryDate = driver.LicenseExpiryDate,
            HasViolations = driver.HasViolations,
            FullName = person.GetFullName(),
            PhoneNumber = person.PhoneNumber,
        };
    }
}
