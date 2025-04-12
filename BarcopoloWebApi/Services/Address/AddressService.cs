using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Address;
using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Services;

public class AddressService : IAddressService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<AddressService> _logger;

    public AddressService(DataBaseContext context, ILogger<AddressService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AddressDto> CreateAsync(CreateAddressDto dto, long currentUserId)
    {
        await EnsureCanAccessPersonAsync(dto.PersonId, currentUserId, "create");

        var address = new Address
        {
            PersonId = dto.PersonId,
            City = dto.City,
            Province = dto.Province,
            Title = dto.Title,
            PostalCode = dto.PostalCode,
            Plate = dto.Plate,
            Unit = dto.Unit,
            FullAddress = dto.FullAddress,
            AdditionalInfo = dto.AdditionalInfo
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Address created with Id {Id} for PersonId {PersonId}", address.Id, dto.PersonId);
        return MapToDto(address);
    }

    public async Task<AddressDto> GetByIdAsync(long id, long currentUserId)
    {
        var address = await _context.Addresses.FindAsync(id)
            ?? throw new Exception("آدرس یافت نشد.");

        await EnsureCanAccessPersonAsync(address.PersonId, currentUserId, "view");

        return MapToDto(address);
    }

    public async Task<IEnumerable<AddressDto>> GetByPersonIdAsync(long personId, long currentUserId)
    {
        await EnsureCanAccessPersonAsync(personId, currentUserId, "view");

        var addresses = await _context.Addresses
            .Where(a => a.PersonId == personId)
            .ToListAsync();

        return addresses.Select(MapToDto);
    }

    public async Task<AddressDto> UpdateAsync(long id, UpdateAddressDto dto, long currentUserId)
    {
        var address = await _context.Addresses.FindAsync(id)
            ?? throw new Exception("آدرس یافت نشد.");

        await EnsureCanAccessPersonAsync(address.PersonId, currentUserId, "update");

        address.City = dto.City ?? address.City;
        address.Province = dto.Province ?? address.Province;
        address.Title = dto.Title ?? address.Title;
        address.PostalCode = dto.PostalCode ?? address.PostalCode;
        address.Plate = dto.Plate ?? address.Plate;
        address.Unit = dto.Unit ?? address.Unit;
        address.FullAddress = dto.FullAddress ?? address.FullAddress;
        address.AdditionalInfo = dto.AdditionalInfo ?? address.AdditionalInfo;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Address with Id {Id} updated successfully", id);
        return MapToDto(address);
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        var address = await _context.Addresses.FindAsync(id)
            ?? throw new Exception("آدرس یافت نشد.");

        await EnsureCanAccessPersonAsync(address.PersonId, currentUserId, "delete");

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Address with Id {Id} deleted successfully", id);
        return true;
    }


    private async Task EnsureCanAccessPersonAsync(long personId, long currentUserId, string action)
    {
        var currentPerson = await _context.Persons.FindAsync(currentUserId)
            ?? throw new Exception("کاربر جاری یافت نشد.");

        if (currentPerson.Id != personId && currentPerson.Role != Enums.SystemRole.superadmin)
        {
            _logger.LogWarning("User {UserId} is not authorized to {Action} address for PersonId {TargetId}", currentUserId, action, personId);
            throw new UnauthorizedAccessException("دسترسی غیرمجاز");
        }

        var targetPerson = await _context.Persons.FindAsync(personId);
        if (targetPerson == null || !targetPerson.IsActive)
        {
            _logger.LogWarning("Target person {TargetId} is invalid or inactive.", personId);
            throw new Exception("کاربر مورد نظر معتبر یا فعال نیست.");
        }
    }

    private static AddressDto MapToDto(Address a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        City = a.City,
        Province = a.Province,
        FullAddress = a.FullAddress,
        PostalCode = a.PostalCode,
        Plate = a.Plate,
        Unit = a.Unit,
        AdditionalInfo = a.AdditionalInfo
    };
}
