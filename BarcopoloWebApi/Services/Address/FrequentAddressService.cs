using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.Helper;
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
            existing.LastUsed = TehranDateTime.Now;
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
                LastUsed = TehranDateTime.Now
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

    public async Task<List<FrequentAddressDto>> GetFrequentAddressesAsync(
        long currentUserId,
        FrequentAddressType type,
        bool isForOrganization,
        long? organizationId = null,
        long? branchId = null)
    {
        if (!Enum.IsDefined(typeof(FrequentAddressType), type))
            throw new AppException("نوع آدرس نامعتبر است.");

        IQueryable<FrequentAddress> query = _context.FrequentAddresses
            .Where(f => f.AddressType == type);

        if (isForOrganization)
        {
            if (organizationId == null)
                throw new AppException("شناسه سازمان اجباری است.");

            query = query.Where(f =>
                f.OrganizationId == organizationId &&
                f.BranchId == branchId); // اگر branchId == null یعنی آدرس سازمانی کلی
        }
        else
        {
            query = query.Where(f => f.PersonId == currentUserId);
        }

        var result = await query
            .OrderByDescending(f => f.UsageCount)
            .ThenByDescending(f => f.LastUsed)
            .ToListAsync();

        return result.Select(f => new FrequentAddressDto
        {
            Id = f.Id,
            Title = f.Title,
            FullAddress = f.FullAddress,
            City = f.City,
            Province = f.Province,
            PostalCode = f.PostalCode,
            Plate = f.Plate,
            Unit = f.Unit,
            UsageCount = f.UsageCount,
            LastUsed = f.LastUsed,
            AddressType = f.AddressType.ToString(),
            PersonId = f.PersonId,
            OrganizationId = f.OrganizationId,
            BranchId = f.BranchId
        }).ToList();
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
        //AddressType = entity.AddressType
    };
}
