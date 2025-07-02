using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Warehouse;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.Services;
using Microsoft.EntityFrameworkCore;

public class WarehouseService : IWarehouseService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<WarehouseService> _logger;

    public WarehouseService(DataBaseContext context, ILogger<WarehouseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task EnsureAdminAccessAsync(long userId)
    {
        var user = await _context.Persons.FindAsync(userId)
            ?? throw new AppException("کاربر یافت نشد.");

        if (!user.IsAdminOrSuperAdmin())
        {
            _logger.LogWarning("User {UserId} unauthorized access attempt", userId);
            throw new AppException("شما دسترسی لازم را ندارید.");
        }
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, long currentUserId)
    {
        await EnsureAdminAccessAsync(currentUserId);
        _logger.LogInformation("Creating warehouse '{Name}'", dto.WarehouseName);

        var warehouse = new Warehouse
        {
            AddressId = dto.AddressId,
            WarehouseName = dto.WarehouseName,
            InternalTelephone = dto.InternalTelephone ?? "",
            ManagerPercentage = dto.ManagerPercentage,
            Rent = dto.Rent,
            TerminalPercentage = dto.TerminalPercentage,
            VatPercentage = dto.VatPercentage,
            InsuranceAmount = dto.InsuranceAmount,
            IsActive = dto.IsActive,
            IsCargoValueMandatory = dto.IsCargoValueMandatory,
            IsDriverNetMandatory = dto.IsDriverNetMandatory,
            PrintText = dto.PrintText ?? ""
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return MapToDto(await _context.Warehouses.Include(w => w.Address).FirstAsync(w => w.Id == warehouse.Id));
    }

    public async Task<WarehouseDto> GetByIdAsync(long id, long currentUserId)
    {
        await EnsureAdminAccessAsync(currentUserId);

        var warehouse = await _context.Warehouses
            .Include(w => w.Address)
            .FirstOrDefaultAsync(w => w.Id == id)
            ?? throw new AppException("انبار یافت نشد.");

        return MapToDto(warehouse);
    }

    public async Task<IEnumerable<WarehouseDto>> GetAllAsync(long currentUserId)
    {
        await EnsureAdminAccessAsync(currentUserId);

        var warehouses = await _context.Warehouses
            .Include(w => w.Address)
            .ToListAsync();

        return warehouses.Select(MapToDto);
    }

    public async Task<WarehouseDto> UpdateAsync(long id, UpdateWarehouseDto dto, long currentUserId)
    {
        await EnsureAdminAccessAsync(currentUserId);

        var warehouse = await _context.Warehouses.FindAsync(id)
            ?? throw new AppException("انبار یافت نشد.");

        if (!string.IsNullOrWhiteSpace(dto.WarehouseName))
            warehouse.WarehouseName = dto.WarehouseName;

        if (!string.IsNullOrWhiteSpace(dto.InternalTelephone))
            warehouse.InternalTelephone = dto.InternalTelephone;

        warehouse.ManagerPercentage = dto.ManagerPercentage ?? warehouse.ManagerPercentage;
        warehouse.Rent = dto.Rent ?? warehouse.Rent;
        warehouse.TerminalPercentage = dto.TerminalPercentage ?? warehouse.TerminalPercentage;
        warehouse.VatPercentage = dto.VatPercentage ?? warehouse.VatPercentage;
        warehouse.IncomePercentage = dto.IncomePercentage ?? warehouse.IncomePercentage;
        warehouse.CommissionPercentage = dto.CommissionPercentage ?? warehouse.CommissionPercentage;
        warehouse.UnloadingPercentage = dto.UnloadingPercentage ?? warehouse.UnloadingPercentage;
        warehouse.DriverPaymentPercentage = dto.DriverPaymentPercentage ?? warehouse.DriverPaymentPercentage;
        warehouse.InsuranceAmount = dto.InsuranceAmount ?? warehouse.InsuranceAmount;
        warehouse.PerCargoInsurance = dto.PerCargoInsurance ?? warehouse.PerCargoInsurance;
        warehouse.ReceiptIssuingCost = dto.ReceiptIssuingCost ?? warehouse.ReceiptIssuingCost;

        if (!string.IsNullOrWhiteSpace(dto.PrintText))
            warehouse.PrintText = dto.PrintText;

        warehouse.IsDriverNetMandatory = dto.IsDriverNetMandatory ?? warehouse.IsDriverNetMandatory;
        warehouse.IsWaybillFareMandatory = dto.IsWaybillFareMandatory ?? warehouse.IsWaybillFareMandatory;
        warehouse.IsCargoValueMandatory = dto.IsCargoValueMandatory ?? warehouse.IsCargoValueMandatory;
        warehouse.IsStampCostMandatory = dto.IsStampCostMandatory ?? warehouse.IsStampCostMandatory;
        warehouse.IsParkingCostMandatory = dto.IsParkingCostMandatory ?? warehouse.IsParkingCostMandatory;
        warehouse.IsLoadingMandatory = dto.IsLoadingMandatory ?? warehouse.IsLoadingMandatory;
        warehouse.IsWarehousingMandatory = dto.IsWarehousingMandatory ?? warehouse.IsWarehousingMandatory;
        warehouse.IsExcessCostMandatory = dto.IsExcessCostMandatory ?? warehouse.IsExcessCostMandatory;
        warehouse.IsActive = dto.IsActive ?? warehouse.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(await _context.Warehouses.Include(w => w.Address).FirstAsync(w => w.Id == id));
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        await EnsureAdminAccessAsync(currentUserId);

        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null)
        {
            _logger.LogWarning("Warehouse with Id {Id} not found", id);
            return false;
        }

        _context.Warehouses.Remove(warehouse);
        await _context.SaveChangesAsync();
        return true;
    }

    private static WarehouseDto MapToDto(Warehouse w) => new WarehouseDto
    {
        Id = w.Id,
        WarehouseName = w.WarehouseName,
        InternalTelephone = w.InternalTelephone,
        AddressSummary = w.Address?.FullAddress ?? "—",
        IsActive = w.IsActive,

        ManagerPercentage = w.ManagerPercentage,
        Rent = w.Rent,
        TerminalPercentage = w.TerminalPercentage,
        VatPercentage = w.VatPercentage,
        InsuranceAmount = w.InsuranceAmount,
        IncomePercentage = w.IncomePercentage,
        CommissionPercentage = w.CommissionPercentage,
        UnloadingPercentage = w.UnloadingPercentage,
        DriverPaymentPercentage = w.DriverPaymentPercentage,
        PerCargoInsurance = w.PerCargoInsurance,
        ReceiptIssuingCost = w.ReceiptIssuingCost,

        PrintText = w.PrintText ?? "",
        IsCargoValueMandatory = w.IsCargoValueMandatory,
        IsDriverNetMandatory = w.IsDriverNetMandatory
    };
}
