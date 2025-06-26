using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Services.Address;
using System;
using BarcopoloWebApi.Data;
using Microsoft.EntityFrameworkCore;

public class FrequentAddressService : IFrequentAddressService
{
    private readonly DataBaseContext _context;

    public FrequentAddressService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task InsertOrUpdateAsync(
        Address address,
        FrequentAddressType addressType,
        long? personId = null,
        long? organizationId = null,
        long? branchId = null)
    {
        var existing = await _context.FrequentAddresses.FirstOrDefaultAsync(f =>
            f.FullAddress == address.FullAddress &&
            f.PersonId == personId &&
            f.OrganizationId == organizationId &&
            f.BranchId == branchId &&
            f.AddressType == addressType);

        if (existing != null)
        {
            existing.UsageCount++;
            existing.LastUsed = DateTime.UtcNow;
        }
        else
        {
            _context.FrequentAddresses.Add(new FrequentAddress
            {
                PersonId = personId,
                OrganizationId = organizationId,
                BranchId = branchId,
                Title = address.Title,
                FullAddress = address.FullAddress,
                City = address.City,
                Province = address.Province,
                PostalCode = address.PostalCode,
                Plate = address.Plate,
                Unit = address.Unit,
                AddressType = addressType
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<FrequentAddressDto>> GetAccessibleOriginsAsync(long currentUserId)
    {
        return await GetFrequentAddressesAsync(currentUserId, FrequentAddressType.Origin);
    }

    public async Task<List<FrequentAddressDto>> GetAccessibleDestinationsAsync(long currentUserId)
    {
        return await GetFrequentAddressesAsync(currentUserId, FrequentAddressType.Destination);
    }

    private async Task<List<FrequentAddressDto>> GetFrequentAddressesAsync(long currentUserId, FrequentAddressType type)
    {
        var person = await _context.Persons
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == currentUserId)
            ?? throw new UnauthorizedAccessException("کاربر یافت نشد.");

        if (!person.Memberships.Any())
        {
            return await _context.FrequentAddresses
                .Where(f => f.PersonId == person.Id && f.AddressType == type)
                .OrderByDescending(f => f.LastUsed)
                .Select(MapToDto)
                .AsQueryable()
                .ToListAsync();
        }

        var membership = person.Memberships.First();

        if (membership.BranchId.HasValue)
        {
            return await _context.FrequentAddresses
                .Where(f => f.BranchId == membership.BranchId && f.AddressType == type)
                .OrderByDescending(f => f.LastUsed)
                .Select(MapToDto)
                .AsQueryable()
                .ToListAsync();
        }
        else
        {
            return await _context.FrequentAddresses
                .Where(f => f.OrganizationId == membership.OrganizationId && f.BranchId == null && f.AddressType == type)
                .OrderByDescending(f => f.LastUsed)
                .Select(MapToDto)
                .AsQueryable()
                .ToListAsync();
        }
    }

    private static FrequentAddressDto MapToDto(FrequentAddress entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        FullAddress = entity.FullAddress,
        City = entity.City,
        Province = entity.Province,
        AddressType = entity.AddressType
    };
}
