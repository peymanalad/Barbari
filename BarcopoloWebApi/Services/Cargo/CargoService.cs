using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Cargo;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Mappers;
using BarcopoloWebApi.Services.Cargo;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using BarcopoloWebApi.Extensions;
using BarcopoloWebApi.Exceptions;

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
            throw new ForbiddenAccessException("اجازه ثبت بار برای دیگران را ندارید.");

        Order? order = null;
        if (dto.OrderId.HasValue)
        {
            order = await _context.Orders
                .Include(o => o.Organization)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId.Value)
                ?? throw new NotFoundException("سفارش یافت نشد.");

            await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);
        }

        if (dto.Weight <= 0)
            throw new AppException("وزن بار باید بیشتر از صفر باشد.");

        if (!dto.NeedsPackaging && dto.PackageCount > 0)
            throw new AppException("وقتی بسته‌بندی غیرفعال است، تعداد بسته باید صفر باشد.");

        var cargoType = await _context.CargoTypes.FindAsync(dto.CargoTypeId)
            ?? throw new NotFoundException("نوع بار وارد شده معتبر نیست.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var cargo = new Cargo
            {
                OwnerId = dto.OwnerId,
                CargoTypeId = dto.CargoTypeId,
                Title = dto.Title.Trim(),
                Contents = dto.Contents?.Trim() ?? string.Empty,
                Value = dto.Value,
                Weight = dto.Weight,
                Length = dto.Length,
                Width = dto.Width,
                Height = dto.Height,
                NeedsPackaging = dto.NeedsPackaging,
                PackagingType = dto.PackagingType?.Trim() ?? string.Empty,
                PackageCount = dto.PackageCount,
                Description = dto.Description?.Trim() ?? string.Empty,
                OrderId = dto.OrderId
            };

            await _context.Cargos.AddAsync(cargo);
            await _context.SaveChangesAsync();

            if (dto.Images is { Count: > 0 })
            {
                var images = dto.Images
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => new CargoImage { CargoId = cargo.Id, ImageUrl = url.Trim() })
                    .ToList();

                await _context.CargoImages.AddRangeAsync(images);
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


    //public async Task<CargoSummaryDto> UpdateAsync(long id, UpdateCargoDto dto, long currentUserId)
    //{
    //    _logger.LogInformation("در حال بروزرسانی بار {CargoId} توسط کاربر {UserId}", id, currentUserId);

    //    var cargo = await _context.Cargos
    //        .Include(c => c.Order)
    //        .Include(c => c.CargoType)
    //        .Include(c => c.Images)
    //        .FirstOrDefaultAsync(c => c.Id == id)
    //        ?? throw new NotFoundException("بار یافت نشد.");

    //    var order = cargo.Order;
    //    var currentUser = await _context.Persons.FindAsync(currentUserId)
    //        ?? throw new NotFoundException("کاربر جاری یافت نشد.");

    //    if (order != null && order.Status >= OrderStatus.Assigned)
    //        throw new AppException("بارهای سفارش تأیید شده قابل ویرایش نیستند.");

    //    await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context, cargo.OwnerId);

    //    using var transaction = await _context.Database.BeginTransactionAsync();

    //    try
    //    {
    //        if (dto.CargoTypeId.HasValue)
    //        {
    //            var cargoType = await _context.CargoTypes.FindAsync(dto.CargoTypeId.Value)
    //                ?? throw new NotFoundException("نوع بار وارد شده معتبر نیست.");
    //            cargo.CargoTypeId = cargoType.Id;
    //        }

    //        if (dto.NeedsPackaging.HasValue)
    //        {
    //            if (!dto.NeedsPackaging.Value && dto.PackageCount.HasValue && dto.PackageCount.Value > 0)
    //                throw new AppException("وقتی بسته‌بندی غیرفعال است، تعداد بسته باید صفر باشد.");

    //            cargo.NeedsPackaging = dto.NeedsPackaging.Value;
    //        }

    //        cargo.Title = dto.Title?.Trim() ?? cargo.Title;
    //        cargo.Contents = dto.Contents?.Trim() ?? cargo.Contents;
    //        cargo.Description = dto.Description?.Trim() ?? cargo.Description;
    //        cargo.Value = dto.Value ?? cargo.Value;

    //        if (dto.Weight.HasValue) cargo.Weight = dto.Weight.Value;
    //        if (dto.Length.HasValue) cargo.Length = dto.Length.Value;
    //        if (dto.Width.HasValue) cargo.Width = dto.Width.Value;
    //        if (dto.Height.HasValue) cargo.Height = dto.Height.Value;
    //        if (!string.IsNullOrWhiteSpace(dto.PackagingType)) cargo.PackagingType = dto.PackagingType.Trim();
    //        if (dto.PackageCount.HasValue) cargo.PackageCount = dto.PackageCount.Value;

    //        // حذف تصاویر مشخص‌شده برای حذف
    //        if (dto.RemoveImages is { Count: > 0 })
    //        {
    //            var toRemove = cargo.Images
    //                .Where(img => dto.RemoveImages.Contains(img.ImageUrl))
    //                .ToList();

    //            _context.CargoImages.RemoveRange(toRemove);
    //        }

    //        // افزودن تصاویر جدید
    //        if (dto.NewImages is { Count: > 0 })
    //        {
    //            var toAdd = dto.NewImages
    //                .Where(url => !string.IsNullOrWhiteSpace(url))
    //                .Select(url => new CargoImage { CargoId = cargo.Id, ImageUrl = url.Trim() });

    //            await _context.CargoImages.AddRangeAsync(toAdd);
    //        }

    //        await _context.SaveChangesAsync();
    //        await transaction.CommitAsync();

    //        _logger.LogInformation("بار {CargoId} با موفقیت بروزرسانی شد", id);

    //        return new CargoSummaryDto
    //        {
    //            Id = cargo.Id,
    //            Title = cargo.Title,
    //            CargoTypeName = cargo.CargoType?.Name ?? "نامشخص"
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        await transaction.RollbackAsync();
    //        _logger.LogError(ex, "خطا در بروزرسانی بار {CargoId}", id);
    //        throw;
    //    }
    //}

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        _logger.LogInformation("در حال حذف بار {CargoId} توسط کاربر {UserId}", id, currentUserId);

        var cargo = await _context.Cargos
            .Include(c => c.Order)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("بار مورد نظر یافت نشد.");

        var order = cargo.Order;

        if (order != null && order.Status >= OrderStatus.Assigned)
            throw new InvalidOperationException("پس از تأیید سفارش امکان حذف بار وجود ندارد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        var isAdmin = currentUser.IsAdminOrSuperAdmin();
        var isOwner = cargo.OwnerId == currentUserId;

        if (!isOwner && !isAdmin)
        {
            if (order == null)
                throw new UnauthorizedAccessAppException("شما مجاز به حذف این بار نیستید.");

            if (order.BranchId.HasValue)
            {
                var isBranchMember = await _context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == order.OrganizationId &&
                    m.BranchId == order.BranchId &&
                    m.PersonId == currentUserId);

                if (!isBranchMember)
                    throw new UnauthorizedAccessAppException("شما مجاز به حذف این بار نیستید.");
            }
            else if (order.OrganizationId.HasValue)
            {
                var isOrgMember = await _context.OrganizationMemberships.AnyAsync(m =>
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

    public async Task<Dictionary<string, object>> UpdateAsync(long id, UpdateCargoDto dto, long currentUserId)
    {
        _logger.LogInformation("در حال بروزرسانی بار {CargoId} توسط کاربر {UserId}", id, currentUserId);

        var cargo = await _context.Cargos
            .Include(c => c.Order)
            .Include(c => c.CargoType)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("بار یافت نشد.");

        var order = cargo.Order;
        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        if (order != null && order.Status >= OrderStatus.Assigned)
            throw new AppException("بارهای سفارش تأیید شده قابل ویرایش نیستند.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context, cargo.OwnerId);

        using var transaction = await _context.Database.BeginTransactionAsync();

        var changes = new Dictionary<string, object>();

        try
        {
            if (dto.CargoTypeId.HasValue && dto.CargoTypeId != cargo.CargoTypeId)
            {
                var cargoType = await _context.CargoTypes.FindAsync(dto.CargoTypeId.Value)
                    ?? throw new NotFoundException("نوع بار وارد شده معتبر نیست.");
                cargo.CargoTypeId = cargoType.Id;
                changes["CargoTypeId"] = cargo.CargoTypeId;
            }

            if (dto.NeedsPackaging.HasValue && dto.NeedsPackaging != cargo.NeedsPackaging)
            {
                if (!dto.NeedsPackaging.Value && dto.PackageCount.HasValue && dto.PackageCount.Value > 0)
                    throw new AppException("وقتی بسته‌بندی غیرفعال است، تعداد بسته باید صفر باشد.");

                cargo.NeedsPackaging = dto.NeedsPackaging.Value;
                changes["NeedsPackaging"] = cargo.NeedsPackaging;
            }

            if (!string.IsNullOrWhiteSpace(dto.Title) && dto.Title.Trim() != cargo.Title)
            {
                cargo.Title = dto.Title.Trim();
                changes["Title"] = cargo.Title;
            }

            if (!string.IsNullOrWhiteSpace(dto.Contents) && dto.Contents.Trim() != cargo.Contents)
            {
                cargo.Contents = dto.Contents.Trim();
                changes["Contents"] = cargo.Contents;
            }

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Trim() != cargo.Description)
            {
                cargo.Description = dto.Description.Trim();
                changes["Description"] = cargo.Description;
            }

            if (dto.Value.HasValue && dto.Value != cargo.Value)
            {
                cargo.Value = dto.Value.Value;
                changes["Value"] = cargo.Value;
            }

            if (dto.Weight.HasValue && dto.Weight != cargo.Weight)
            {
                cargo.Weight = dto.Weight.Value;
                changes["Weight"] = cargo.Weight;
            }

            if (dto.Length.HasValue && dto.Length != cargo.Length)
            {
                cargo.Length = dto.Length.Value;
                changes["Length"] = cargo.Length;
            }

            if (dto.Width.HasValue && dto.Width != cargo.Width)
            {
                cargo.Width = dto.Width.Value;
                changes["Width"] = cargo.Width;
            }

            if (dto.Height.HasValue && dto.Height != cargo.Height)
            {
                cargo.Height = dto.Height.Value;
                changes["Height"] = cargo.Height;
            }

            if (!string.IsNullOrWhiteSpace(dto.PackagingType) && dto.PackagingType.Trim() != cargo.PackagingType)
            {
                cargo.PackagingType = dto.PackagingType.Trim();
                changes["PackagingType"] = cargo.PackagingType;
            }

            if (dto.PackageCount.HasValue && dto.PackageCount != cargo.PackageCount)
            {
                cargo.PackageCount = dto.PackageCount.Value;
                changes["PackageCount"] = cargo.PackageCount;
            }

            if (dto.RemoveImages is { Count: > 0 })
            {
                var toRemove = cargo.Images
                    .Where(img => dto.RemoveImages.Contains(img.ImageUrl))
                    .ToList();

                _context.CargoImages.RemoveRange(toRemove);
                changes["RemovedImages"] = toRemove.Select(x => x.ImageUrl).ToList();
            }

            if (dto.NewImages is { Count: > 0 })
            {
                var toAdd = dto.NewImages
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => new CargoImage { CargoId = cargo.Id, ImageUrl = url.Trim() })
                    .ToList();

                await _context.CargoImages.AddRangeAsync(toAdd);
                changes["AddedImages"] = toAdd.Select(x => x.ImageUrl).ToList();
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("بار {CargoId} با موفقیت بروزرسانی شد", id);

            return changes;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در بروزرسانی بار {CargoId}", id);
            throw;
        }
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
