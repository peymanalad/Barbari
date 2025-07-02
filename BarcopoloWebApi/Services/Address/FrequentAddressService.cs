using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;

public class FrequentAddressService : IFrequentAddressService
{
    private readonly DataBaseContext _context;

    public FrequentAddressService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task InsertOrUpdateAsync(Address address, FrequentAddressType addressType,
        long? personId = null, long? organizationId = null, long? branchId = null)
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
                AddressType = addressType,
                UsageCount = 1,
                LastUsed = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public Task<List<FrequentAddressDto>> GetDestinationsAsync(long currentUserId, FrequentAddressScope scope)
    {
        return GetFrequentAddressesAsync(currentUserId, scope, FrequentAddressType.Destination);
    }

    public Task<List<FrequentAddressDto>> GetOriginsAsync(long currentUserId, FrequentAddressScope scope)
    {
        return GetFrequentAddressesAsync(currentUserId, scope, FrequentAddressType.Origin);
    }

    private async Task<List<FrequentAddressDto>> GetFrequentAddressesAsync(long currentUserId, FrequentAddressScope scope, FrequentAddressType type)
    {
        IQueryable<FrequentAddress> query = _context.FrequentAddresses
            .Where(f => f.AddressType == type);

        switch (scope.Type)
        {
            case AddressScopeType.Person:
                query = query.Where(f => f.PersonId == currentUserId);
                break;

            case AddressScopeType.Organization:
                if (scope.OrganizationId == null)
                    throw new InvalidOperationException("OrganizationId is required for Organization scope");
                query = query.Where(f => f.OrganizationId == scope.OrganizationId && f.BranchId == null);
                break;

            case AddressScopeType.Branch:
                if (scope.OrganizationId == null || scope.BranchId == null)
                    throw new InvalidOperationException("OrganizationId and BranchId are required for Branch scope");
                query = query.Where(f => f.OrganizationId == scope.OrganizationId && f.BranchId == scope.BranchId);
                break;

            default:
                throw new InvalidOperationException("Scope type is invalid");
        }

        return (await query
            .OrderByDescending(f => f.LastUsed)
            .ToListAsync())
            .Select(MapToDto)
            .ToList();
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
