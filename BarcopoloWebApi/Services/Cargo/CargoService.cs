using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Cargo;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Mappers;
using BarcopoloWebApi.Services.Cargo;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using BarcopoloWebApi.Extensions;

public class CargoService : ICargoService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<CargoService> _logger;

    public CargoService(DataBaseContext context, ILogger<CargoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CargoSummaryDto> CreateAsync(CreateCargoDto dto, long currentUserId)
    {
        _logger.LogInformation("در حال ایجاد بار جدید برای سفارش {OrderId} توسط کاربر {UserId}", dto.OrderId, currentUserId);

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (dto.OwnerId != currentUserId && !currentUser.IsAdminOrSuperAdmin())
            throw new UnauthorizedAccessAppException("اجازه ثبت بار برای دیگران را ندارید.");
        Order? order = null;
        if (dto.OrderId is not null)
        {
            order = await _context.Orders
                            .Include(o => o.Organization)
                            .FirstOrDefaultAsync(o => o.Id == dto.OrderId)
                        ?? throw new NotFoundException("سفارش یافت نشد.");

            await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);
        }

        if (dto.Weight <= 0)
            throw new ArgumentException("وزن بار باید بیشتر از صفر باشد.");

        if (!dto.NeedsPackaging && dto.PackageCount > 0)
            throw new ArgumentException("وقتی بسته‌بندی غیرفعال است، تعداد بسته باید صفر باشد.");

        var cargoType = await _context.CargoTypes.FirstOrDefaultAsync(ct => ct.Id == dto.CargoTypeId);
        if (cargoType == null)
            throw new NotFoundException("نوع بار وارد شده معتبر نیست.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cargo = new Cargo
            {
                OwnerId = dto.OwnerId,
                CargoTypeId = dto.CargoTypeId,
                Title = dto.Title,
                Contents = dto.Contents ?? "",
                Weight = dto.Weight,
                Length = dto.Length,
                Width = dto.Width,
                Height = dto.Height,
                NeedsPackaging = dto.NeedsPackaging,
                PackagingType = dto.PackagingType ?? "",
                PackageCount = dto.PackageCount,
                Description = dto.Description ?? "",
                OrderId = order?.Id
            };

            _context.Cargos.Add(cargo);
            await _context.SaveChangesAsync();

            if (dto.Images is { Count: > 0 })
            {
                var cargoImages = dto.Images
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => new CargoImage
                    {
                        CargoId = cargo.Id,
                        ImageUrl = url.Trim()
                    }).ToList();

                _context.CargoImages.AddRange(cargoImages);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            _logger.LogInformation("بار جدید با شناسه {CargoId} با موفقیت ایجاد شد", cargo.Id);

            return new CargoSummaryDto
            {
                Id = cargo.Id,
                Title = cargo.Title,
                CargoTypeName = cargoType.Name
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در ایجاد بار جدید برای سفارش {OrderId}", dto.OrderId);
            throw;
        }
    }


    public async Task<IEnumerable<CargoDto>> GetByOrderIdAsync(long orderId, long currentUserId)
    {
        _logger.LogInformation("در حال دریافت بارهای سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        var order = await _context.Orders
                        .Include(o => o.Organization)
                        .FirstOrDefaultAsync(o => o.Id == orderId)
                    ?? throw new NotFoundException("سفارش مورد نظر یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
                          ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        var cargos = await _context.Cargos
            .AsNoTracking()
            .Where(c => c.OrderId == orderId)
            .Include(c => c.CargoType)
            .Include(c => c.Images)
            .ToListAsync();

        _logger.LogInformation("تعداد {Count} بار برای سفارش {OrderId} یافت شد", cargos.Count, orderId);

        return cargos.Select(c => c.MapToDto()).ToList();
    }



    public async Task<CargoDto> GetByIdAsync(long id, long currentUserId)
    {
        _logger.LogInformation("در حال دریافت بار با شناسه {CargoId} توسط کاربر {UserId}", id, currentUserId);

        var cargo = await _context.Cargos
                        .Include(c => c.CargoType)
                        .Include(c => c.Images)
                        .Include(c => c.Order)
                        .ThenInclude(o => o.Organization)
                        .FirstOrDefaultAsync(c => c.Id == id)
                    ?? throw new NotFoundException("بار مورد نظر یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
                          ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var order = cargo.Order;

        var isAdmin = currentUser.IsAdminOrSuperAdmin();
        var isOwner = cargo.OwnerId == currentUserId;

        if (!isOwner && !isAdmin)
        {
            if (order.BranchId.HasValue)
            {
                var isBranchMember = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == order.OrganizationId &&
                    m.BranchId == order.BranchId &&
                    m.PersonId == currentUserId);

                if (!isBranchMember)
                    throw new UnauthorizedAccessAppException("شما مجاز به مشاهده این بار نیستید.");
            }
            else if (order.OrganizationId.HasValue)
            {
                var isOrgMember = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == order.OrganizationId &&
                    m.PersonId == currentUserId);

                if (!isOrgMember)
                    throw new UnauthorizedAccessAppException("شما مجاز به مشاهده این بار نیستید.");
            }
            else
            {
                throw new UnauthorizedAccessAppException("شما مجاز به مشاهده این بار نیستید.");
            }
        }

        _logger.LogInformation("بار {CargoId} با موفقیت برای کاربر {UserId} بازیابی شد", id, currentUserId);

        return cargo.MapToDto();
    }


    public async Task<CargoSummaryDto> UpdateAsync(long id, UpdateCargoDto dto, long currentUserId)
    {
        _logger.LogInformation("در حال بروزرسانی بار {CargoId} توسط کاربر {UserId}", id, currentUserId);

        var cargo = await _context.Cargos
            .Include(c => c.Order)
            .Include(c => c.CargoType)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("بار یافت نشد.");

        var order = cargo.Order;

        if (order != null && order.Status >= OrderStatus.Assigned)
            throw new InvalidOperationException("بارهای سفارش تأیید شده قابل ویرایش نیستند.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context, cargo.OwnerId);

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (dto.CargoTypeId.HasValue)
            {
                var cargoType = await _context.CargoTypes
                    .FirstOrDefaultAsync(ct => ct.Id == dto.CargoTypeId);
                if (cargoType == null)
                    throw new NotFoundException("نوع بار وارد شده معتبر نیست.");

                cargo.CargoTypeId = cargoType.Id;
            }

            if (dto.NeedsPackaging.HasValue)
            {
                if (!dto.NeedsPackaging.Value && dto.PackageCount.HasValue && dto.PackageCount.Value > 0)
                    throw new ArgumentException("وقتی بسته‌بندی غیرفعال است، تعداد بسته باید صفر باشد.");

                cargo.NeedsPackaging = dto.NeedsPackaging.Value;
            }

            cargo.Title = dto.Title ?? cargo.Title;
            cargo.Contents = dto.Contents ?? cargo.Contents;
            cargo.Description = dto.Description ?? cargo.Description;

            if (dto.Weight.HasValue) cargo.Weight = dto.Weight.Value;
            if (dto.Length.HasValue) cargo.Length = dto.Length.Value;
            if (dto.Width.HasValue) cargo.Width = dto.Width.Value;
            if (dto.Height.HasValue) cargo.Height = dto.Height.Value;

            if (!string.IsNullOrWhiteSpace(dto.PackagingType))
                cargo.PackagingType = dto.PackagingType;

            if (dto.PackageCount.HasValue)
                cargo.PackageCount = dto.PackageCount.Value;

            if (dto.Images is { Count: > 0 })
            {
                _context.CargoImages.RemoveRange(cargo.Images);

                var newImages = dto.Images
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => new CargoImage
                    {
                        CargoId = cargo.Id,
                        ImageUrl = url.Trim()
                    });

                await _context.CargoImages.AddRangeAsync(newImages);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("بار {CargoId} با موفقیت بروزرسانی شد", id);

            return new CargoSummaryDto
            {
                Id = cargo.Id,
                Title = cargo.Title,
                CargoTypeName = cargo.CargoType?.Name ?? "نامشخص"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در بروزرسانی بار {CargoId}", id);
            throw;
        }
    }


    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        _logger.LogInformation("در حال حذف بار {CargoId} توسط کاربر {UserId}", id, currentUserId);

        var cargo = await _context.Cargos
            .Include(c => c.Order)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("بار مورد نظر یافت نشد.");

        var order = cargo.Order;

        if (order.Status >= OrderStatus.Assigned)
            throw new InvalidOperationException("پس از تأیید سفارش امکان حذف بار وجود ندارد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var isAdmin = currentUser.IsAdminOrSuperAdmin();
        var isOwner = cargo.OwnerId == currentUserId;

        if (!isOwner && !isAdmin)
        {
            if (order.BranchId.HasValue)
            {
                bool isBranchMember = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == order.OrganizationId &&
                    m.BranchId == order.BranchId &&
                    m.PersonId == currentUserId);

                if (!isBranchMember)
                    throw new UnauthorizedAccessAppException("شما مجاز به حذف این بار نیستید.");
            }
            else if (order.OrganizationId.HasValue)
            {
                bool isOrgMember = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == order.OrganizationId &&
                    m.PersonId == currentUserId);

                if (!isOrgMember)
                    throw new UnauthorizedAccessAppException("شما مجاز به حذف این بار نیستید.");
            }
            else
            {
                throw new UnauthorizedAccessAppException("شما مجاز به حذف این بار نیستید.");
            }
        }

        var images = await _context.CargoImages.Where(img => img.CargoId == cargo.Id).ToListAsync();
        _context.CargoImages.RemoveRange(images);

        _context.Cargos.Remove(cargo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("بار {CargoId} توسط کاربر {UserId} حذف شد", id, currentUserId);

        return true;
    }

    public async Task<PagedResult<CargoDto>> SearchAsync(CargoSearchDto filter, long currentUserId)
    {
        _logger.LogInformation("در حال جستجوی بارها توسط کاربر {UserId} | فیلتر: {@Filter}", currentUserId, filter);

        var currentUser = await _context.Persons
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var isAdmin = currentUser.IsAdminOrSuperAdmin();

        var query = _context.Cargos
            .AsNoTracking()
            .Include(c => c.CargoType)
            .Include(c => c.Images)
            .Include(c => c.Order)
            .ThenInclude(o => o.Organization)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(c =>
                c.OwnerId == currentUserId ||
                c.Order.OrganizationId != null &&
                (
                    _context.OrganizationMemberships.Any(m => m.PersonId == currentUserId && m.OrganizationId == c.Order.OrganizationId) ||
                    (c.Order.BranchId != null && _context.OrganizationMemberships.Any(m => m.PersonId == currentUserId && m.BranchId == c.Order.BranchId))
                ));
        }

        if (filter.OrderId.HasValue)
            query = query.Where(c => c.OrderId == filter.OrderId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TitleContains))
        {
            var keyword = filter.TitleContains.NormalizePersian().ToLower();
            query = query.Where(c =>
                c.Title != null &&
                c.Title.ToLower()
                    .Replace("ي", "ی")
                    .Replace("ك", "ک")
                    .Contains(keyword)
            );
        }

        if (filter.MinWeight.HasValue)
            query = query.Where(c => c.Weight >= filter.MinWeight.Value);

        if (filter.MaxWeight.HasValue)
            query = query.Where(c => c.Weight <= filter.MaxWeight.Value);

        var totalCount = await query.CountAsync();

        var cargos = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        _logger.LogInformation("تعداد {Count} نتیجه برای جستجوی بارها یافت شد", cargos.Count);

        return new PagedResult<CargoDto>
        {
            Items = cargos.Select(c => c.MapToDto()).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }






    private static CargoDto MapToDto(Cargo cargo) => new CargoDto
    {
        Id = cargo.Id,
        Title = cargo.Title,
        Contents = cargo.Contents,
        Weight = cargo.Weight,
        Length = cargo.Length,
        Width = cargo.Width,
        Height = cargo.Height,
        PackagingType = cargo.PackagingType,
        PackageCount = cargo.PackageCount,
        Description = cargo.Description,
        NeedsPackaging = cargo.NeedsPackaging,
        CargoTypeName = cargo.CargoType?.Name ?? "نامشخص",
        ImageUrls = cargo.Images.Select(img => img.ImageUrl).ToList()
    };

}
