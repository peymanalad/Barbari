using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Address;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
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
        await EnsureHasCreateAccessAsync(dto, currentUserId);

        var address = new Address
        {
            PersonId = dto.PersonId,
            OrganizationId = dto.OrganizationId,
            BranchId = dto.BranchId,
            Title = dto.Title,
            City = dto.City,
            Province = dto.Province,
            FullAddress = dto.FullAddress,
            PostalCode = dto.PostalCode,
            Plate = dto.Plate,
            Unit = dto.Unit,
            AdditionalInfo = dto.AdditionalInfo
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("آدرس جدید با شناسه {Id} ثبت شد", address.Id);
        return MapToDto(address);
    }

    public async Task<AddressDto> GetByIdAsync(long id, long currentUserId)
    {
        var address = await _context.Addresses.FindAsync(id)
            ?? throw new NotFoundException("آدرس یافت نشد.");

        await EnsureHasViewAccessAsync(address, currentUserId);
        return MapToDto(address);
    }

    public async Task<IEnumerable<AddressDto>> GetByPersonIdAsync(long personId, long currentUserId)
    {
        if (personId != currentUserId && !await IsSuperadminOrAdminAsync(currentUserId))
            throw new ForbiddenAccessException("شما فقط می‌توانید آدرس‌های خودتان را مشاهده کنید");

        var addresses = await _context.Addresses
            .Where(a => a.PersonId == personId)
            .ToListAsync();

        return addresses.Select(MapToDto);
    }

    public async Task<AddressDto> UpdateAsync(long id, UpdateAddressDto dto, long currentUserId)
    {
        var address = await _context.Addresses.FindAsync(id)
            ?? throw new NotFoundException("آدرس یافت نشد.");

        var user = await _context.Persons
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (user.IsSuperAdmin())
        {
            if (dto.BranchId.HasValue)
            {
                var isBranchValid = await _context.SubOrganizations.AnyAsync(b =>
                    b.Id == dto.BranchId && b.OrganizationId == address.OrganizationId);

                if (!isBranchValid)
                    throw new ForbiddenAccessException("شعبه موردنظر متعلق به این سازمان نیست.");

                address.BranchId = dto.BranchId;
            }

            ApplyAddressUpdate(address, dto);
            await _context.SaveChangesAsync();
            _logger.LogInformation("آدرس {Id} توسط superadmin بروزرسانی شد", id);
            return MapToDto(address);
        }

        // جلوگیری از تغییر سازمان
        // چون DTO سازمان نداره، بررسی میکنیم که مقدار موجود دستکاری نشه
        var originalOrgId = address.OrganizationId;

        if (address.PersonId.HasValue)
        {
            if (address.PersonId != currentUserId)
                throw new ForbiddenAccessException("شما فقط می‌توانید آدرس شخصی خودتان را ویرایش کنید");
        }
        else if (originalOrgId.HasValue && address.BranchId.HasValue)
        {
            var isBranchAdmin = user.Memberships.Any(m =>
                m.OrganizationId == originalOrgId &&
                m.BranchId == address.BranchId &&
                m.Role == SystemRole.branchadmin);

            var isOrgAdmin = user.Memberships.Any(m =>
                m.OrganizationId == originalOrgId &&
                m.Role == SystemRole.orgadmin);

            if (!isBranchAdmin && !isOrgAdmin)
                throw new ForbiddenAccessException("شما اجازه ویرایش آدرس این شعبه را ندارید.");

            if (dto.BranchId.HasValue && dto.BranchId != address.BranchId)
            {
                if (!isOrgAdmin)
                    throw new ForbiddenAccessException("فقط orgadmin می‌تواند شعبه را تغییر دهد.");

                var isBranchValid = await _context.SubOrganizations.AnyAsync(b =>
                    b.Id == dto.BranchId && b.OrganizationId == originalOrgId);

                if (!isBranchValid)
                    throw new ForbiddenAccessException("شعبه انتخاب‌شده متعلق به این سازمان نیست.");

                address.BranchId = dto.BranchId;
            }
        }
        else if (originalOrgId.HasValue)
        {
            var isOrgAdmin = user.Memberships.Any(m =>
                m.OrganizationId == originalOrgId &&
                m.Role == SystemRole.orgadmin);

            if (!isOrgAdmin)
                throw new ForbiddenAccessException("شما اجازه ویرایش آدرس این سازمان را ندارید.");
        }
        else
        {
            throw new AppException("نوع آدرس برای ویرایش قابل تشخیص نیست.");
        }

        ApplyAddressUpdate(address, dto);
        await _context.SaveChangesAsync();
        _logger.LogInformation("آدرس {Id} توسط کاربر {UserId} بروزرسانی شد", id, currentUserId);
        return MapToDto(address);
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        var address = await _context.Addresses.FindAsync(id)
            ?? throw new NotFoundException("آدرس یافت نشد.");

        await EnsureHasEditAccessAsync(address, currentUserId);

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("آدرس با شناسه {Id} حذف شد", id);
        return true;
    }

    public async Task<IEnumerable<AddressDto>> GetByOrganizationIdAsync(long organizationId, long currentUserId)
    {
        var user = await _context.Persons
                       .Include(p => p.Memberships)
                       .FirstOrDefaultAsync(p => p.Id == currentUserId)
                   ?? throw new NotFoundException("کاربر جاری یافت نشد");

        if (user.IsSuperAdmin() || user.IsAdmin())
            return await GetOrganizationAddresses(organizationId);

        if (user.Memberships.Any(m => m.OrganizationId == organizationId && m.Role == SystemRole.orgadmin))
            return await GetOrganizationAddresses(organizationId);

        var hasBranch = await _context.SubOrganizations.AnyAsync(b => b.OrganizationId == organizationId);

        if (!hasBranch && user.Memberships.Any(m => m.OrganizationId == organizationId))
            return await GetOrganizationAddresses(organizationId);

        throw new ForbiddenAccessException("دسترسی غیرمجاز برای مشاهده آدرس‌های این سازمان");
    }

    public async Task<IEnumerable<AddressDto>> GetByBranchIdAsync(long branchId, long currentUserId)
    {
        var user = await _context.Persons.Include(p => p.Memberships).FirstOrDefaultAsync(p => p.Id == currentUserId)
                   ?? throw new NotFoundException("کاربر جاری یافت نشد");

        if (user.IsSuperAdmin() || user.IsAdmin())
            return await GetBranchAddresses(branchId);

        var orgId = await _context.SubOrganizations.Where(b => b.Id == branchId).Select(b => b.OrganizationId).FirstOrDefaultAsync();

        if (user.Memberships.Any(m => m.OrganizationId == orgId && m.Role == SystemRole.orgadmin))
            return await GetBranchAddresses(branchId);

        if (user.Memberships.Any(m => m.BranchId == branchId && m.Role == SystemRole.branchadmin))
            return await GetBranchAddresses(branchId);

        throw new ForbiddenAccessException("دسترسی غیرمجاز برای مشاهده آدرس‌های این شعبه");
    }

    private async Task<IEnumerable<AddressDto>> GetOrganizationAddresses(long organizationId)
    {
        var addresses = await _context.Addresses
            .Where(a => a.OrganizationId == organizationId && a.BranchId == null)
            .ToListAsync();

        return addresses.Select(MapToDto);
    }

    private async Task<IEnumerable<AddressDto>> GetBranchAddresses(long branchId)
    {
        var addresses = await _context.Addresses
            .Where(a => a.BranchId == branchId)
            .ToListAsync();

        return addresses.Select(MapToDto);
    }

    private async Task EnsureHasCreateAccessAsync(CreateAddressDto dto, long userId)
    {
        var user = await _context.Persons.FindAsync(userId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد");

        var isAdminOrSuperadmin = user.IsAdminOrSuperAdmin(); // فرض بر این که این متد تعریف شده

        if (dto.OrganizationId.HasValue)
        {
            var organizationExists = await _context.Organizations
                .AnyAsync(o => o.Id == dto.OrganizationId);

            if (!organizationExists)
                throw new NotFoundException("سازمان مورد نظر یافت نشد");
        }

        if (dto.BranchId.HasValue)
        {
            var branch = await _context.SubOrganizations
                .FirstOrDefaultAsync(b => b.Id == dto.BranchId);

            if (branch == null)
                throw new NotFoundException("شعبه مورد نظر یافت نشد");

            if (dto.OrganizationId.HasValue && branch.OrganizationId != dto.OrganizationId)
                throw new ForbiddenAccessException("این شعبه متعلق به سازمان انتخاب‌شده نیست");
        }

        if (dto.PersonId.HasValue)
        {
            // دسترسی شخصی: فقط خود شخص یا admin/superadmin
            if (dto.PersonId != userId && !isAdminOrSuperadmin)
                throw new ForbiddenAccessException("فقط خود شخص یا مدیر کل/مدیر می‌تواند آدرس شخصی تعریف کند");

            return;
        }

        if (dto.OrganizationId.HasValue && dto.BranchId.HasValue)
        {
            // برای آدرس شعبه: branchadmin همان شعبه یا orgadmin سازمان یا admin/superadmin
            var isBranchAdmin = await _context.OrganizationMemberships.AnyAsync(m =>
                m.PersonId == userId &&
                m.OrganizationId == dto.OrganizationId &&
                m.BranchId == dto.BranchId &&
                m.Role == SystemRole.branchadmin);

            var isOrgAdmin = await _context.OrganizationMemberships.AnyAsync(m =>
                m.PersonId == userId &&
                m.OrganizationId == dto.OrganizationId &&
                m.Role == SystemRole.orgadmin);

            if (!isBranchAdmin && !isOrgAdmin && !isAdminOrSuperadmin)
                throw new ForbiddenAccessException("فقط مدیر سازمان یا مدیر همان شعبه یا مدیر کل می‌تواند آدرس برای آن تعریف کند");

            return;
        }

        if (dto.OrganizationId.HasValue)
        {
            // برای آدرس خود سازمان: فقط orgadmin آن سازمان یا admin/superadmin
            var isOrgAdmin = await _context.OrganizationMemberships.AnyAsync(m =>
                m.PersonId == userId &&
                m.OrganizationId == dto.OrganizationId &&
                m.Role == SystemRole.orgadmin);

            if (!isOrgAdmin && !isAdminOrSuperadmin)
                throw new ForbiddenAccessException("فقط مدیر سازمان یا مدیر کل می‌تواند آدرس سازمان تعریف کند");

            return;
        }

        throw new AppException("نوع آدرس مشخص نیست (شخصی، سازمان یا شعبه)");
    }


    private async Task EnsureHasEditAccessAsync(Address address, long userId)
    {
        var user = await _context.Persons.FindAsync(userId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد");

        if (user.IsSuperAdmin())
            return;

        if (address.PersonId.HasValue)
        {
            if (address.PersonId != userId)
                throw new ForbiddenAccessException("شما فقط می‌توانید آدرس شخصی خودتان را ویرایش کنید");
        }
        else if (address.OrganizationId.HasValue && address.BranchId.HasValue)
        {
            var isBranchAdmin = await _context.OrganizationMemberships.AnyAsync(m =>
                m.PersonId == userId &&
                m.OrganizationId == address.OrganizationId &&
                m.BranchId == address.BranchId &&
                m.Role == SystemRole.branchadmin);

            var isOrgAdmin = await _context.OrganizationMemberships.AnyAsync(m =>
                m.PersonId == userId &&
                m.OrganizationId == address.OrganizationId &&
                m.Role == SystemRole.orgadmin);

            if (!isBranchAdmin && !isOrgAdmin)
                throw new ForbiddenAccessException("فقط مدیر شعبه یا مدیر سازمان می‌تواند آدرس را ویرایش کند");
        }
        else if (address.OrganizationId.HasValue)
        {
            var isOrgAdmin = await _context.OrganizationMemberships.AnyAsync(m =>
                m.PersonId == userId &&
                m.OrganizationId == address.OrganizationId &&
                m.Role == SystemRole.orgadmin);

            if (!isOrgAdmin)
                throw new ForbiddenAccessException("فقط مدیر سازمان می‌تواند آدرس‌های سازمان را ویرایش کند");
        }
        else
        {
            throw new ForbiddenAccessException("نوع آدرس برای ویرایش قابل تشخیص نیست");
        }
    }

    private async Task EnsureHasViewAccessAsync(Address address, long userId)
    {
        var user = await _context.Persons
                       .Include(p => p.Memberships)
                       .FirstOrDefaultAsync(p => p.Id == userId)
                   ?? throw new NotFoundException("کاربر جاری یافت نشد");

        if (user.IsSuperAdmin() || user.IsAdmin())
            return;

        if (address.PersonId.HasValue)
        {
            if (address.PersonId != userId)
                throw new ForbiddenAccessException("شما فقط می‌توانید آدرس شخصی خودتان را مشاهده کنید");
        }
        else if (address.OrganizationId.HasValue && address.BranchId.HasValue)
        {
            var isBranchAdmin = user.Memberships.Any(m =>
                m.OrganizationId == address.OrganizationId &&
                m.BranchId == address.BranchId &&
                m.Role == SystemRole.branchadmin);

            var isOrgAdmin = user.Memberships.Any(m =>
                m.OrganizationId == address.OrganizationId &&
                m.Role == SystemRole.orgadmin);

            if (!isBranchAdmin && !isOrgAdmin)
                throw new ForbiddenAccessException("شما به آدرس‌های این شعبه دسترسی ندارید");
        }
        else if (address.OrganizationId.HasValue)
        {
            var hasBranch = await _context.SubOrganizations.AnyAsync(b => b.OrganizationId == address.OrganizationId);

            var isOrgAdmin = user.Memberships.Any(m =>
                m.OrganizationId == address.OrganizationId &&
                m.Role == SystemRole.orgadmin);

            if (!hasBranch && user.Memberships.Any(m => m.OrganizationId == address.OrganizationId))
                return;

            if (!isOrgAdmin)
                throw new ForbiddenAccessException("شما به آدرس‌های این سازمان دسترسی ندارید");
        }
        else
        {
            throw new ForbiddenAccessException("نوع آدرس قابل تشخیص نیست");
        }
    }

    private Task<bool> IsSuperadminOrAdminAsync(long userId)
    {
        return _context.Persons.AnyAsync(p => p.Id == userId || p.Role == SystemRole.superadmin || p.Role == SystemRole.admin);
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
        AdditionalInfo = a.AdditionalInfo,
        OrganizationId = a.OrganizationId,
        BranchId = a.BranchId,

    };

    private static void ApplyAddressUpdate(Address address, UpdateAddressDto dto)
    {
        address.Title = dto.Title ?? address.Title;
        address.City = dto.City ?? address.City;
        address.Province = dto.Province ?? address.Province;
        address.FullAddress = dto.FullAddress ?? address.FullAddress;
        address.PostalCode = dto.PostalCode ?? address.PostalCode;
        address.Plate = dto.Plate ?? address.Plate;
        address.Unit = dto.Unit ?? address.Unit;
        address.AdditionalInfo = dto.AdditionalInfo ?? address.AdditionalInfo;
    }

}
