using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Vehicle;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Services;

public class WarehouseVehicleService : IWarehouseVehicleService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<WarehouseVehicleService> _logger;

    public WarehouseVehicleService(DataBaseContext context, ILogger<WarehouseVehicleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task EnsureAdminOrSuperAdminAsync(long currentUserId)
    {
        var user = await _context.Persons.FindAsync(currentUserId)
                   ?? throw new Exception("کاربر یافت نشد.");

        if (user.Role != SystemRole.admin && user.Role != SystemRole.superadmin)
            throw new UnauthorizedAccessException("شما مجاز به انجام این عملیات نیستید.");
    }

    public async Task AssignVehicleToWarehouse(long warehouseId, long vehicleId, long currentUserId)
    {
        await EnsureAdminOrSuperAdminAsync(currentUserId);

        _logger.LogInformation("Assigning vehicle {VehicleId} to warehouse {WarehouseId} by user {UserId}", vehicleId, warehouseId, currentUserId);

        bool exists = await _context.WarehouseVehicles
            .AnyAsync(wv => wv.WarehouseId == warehouseId && wv.VehicleId == vehicleId);

        if (exists)
        {
            _logger.LogInformation("Vehicle {VehicleId} is already assigned to warehouse {WarehouseId}", vehicleId, warehouseId);
            return;
        }

        var newAssignment = new WarehouseVehicle
        {
            WarehouseId = warehouseId,
            VehicleId = vehicleId,
        };

        _context.WarehouseVehicles.Add(newAssignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle {VehicleId} assigned to warehouse {WarehouseId}", vehicleId, warehouseId);
    }

    public async Task<bool> RemoveVehicleFromWarehouse(long warehouseId, long vehicleId, long currentUserId)
    {
        await EnsureAdminOrSuperAdminAsync(currentUserId);

        _logger.LogInformation("Removing vehicle {VehicleId} from warehouse {WarehouseId} by user {UserId}", vehicleId, warehouseId, currentUserId);

        var assignment = await _context.WarehouseVehicles
            .FirstOrDefaultAsync(wv => wv.WarehouseId == warehouseId && wv.VehicleId == vehicleId);

        if (assignment == null)
        {
            _logger.LogWarning("No assignment found for vehicle {VehicleId} in warehouse {WarehouseId}", vehicleId, warehouseId);
            return false;
        }

        _context.WarehouseVehicles.Remove(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle {VehicleId} successfully removed from warehouse {WarehouseId}", vehicleId, warehouseId);
        return true;
    }

    public async Task<IEnumerable<VehicleDto>> GetVehiclesByWarehouse(long warehouseId, long currentUserId)
    {
        await EnsureAdminOrSuperAdminAsync(currentUserId);

        _logger.LogInformation("Fetching vehicles assigned to warehouse {WarehouseId} by user {UserId}", warehouseId, currentUserId);

        var vehicles = await _context.WarehouseVehicles
            .Where(wv => wv.WarehouseId == warehouseId)
            .Include(wv => wv.Vehicle)
            .ThenInclude(v => v.Driver)
            .ThenInclude(d => d.Person)
            .Select(wv => new VehicleDto
            {
                Id = wv.Vehicle.Id,
                SmartCardCode = wv.Vehicle.SmartCardCode,
                PlateIranCode = wv.Vehicle.PlateIranCode,
                PlateThreeDigit = wv.Vehicle.PlateThreeDigit,
                PlateLetter = wv.Vehicle.PlateLetter,
                PlateTwoDigit = wv.Vehicle.PlateTwoDigit,
                Model = wv.Vehicle.Model,
                Color = wv.Vehicle.Color,
                Engine = wv.Vehicle.Engine,
                Chassis = wv.Vehicle.Chassis,
                Axles = wv.Vehicle.Axles,
                IsVan = wv.Vehicle.IsVan,
                VanCommission = wv.Vehicle.VanCommission,
                IsBroken = wv.Vehicle.IsBroken,
                HasViolations = wv.Vehicle.HasViolations,
                DriverId = wv.Vehicle.DriverId,
                DriverFullName = wv.Vehicle.Driver != null && wv.Vehicle.Driver.Person != null
                    ? wv.Vehicle.Driver.Person.GetFullName()
                    : "بدون راننده"
            })

            .ToListAsync();

        return vehicles;
    }
    public async Task<PagedResult<VehicleDto>> GetUnassignedVehicles(long currentUserId, int page = 1, int pageSize = 20)
    {
        if (!await IsAuthorizedAsync(currentUserId))
        {
            _logger.LogWarning("Unauthorized access to retrieve unassigned vehicles by user {UserId}", currentUserId);
            throw new UnauthorizedAccessException("Not authorized to view unassigned vehicles.");
        }

        var assignedVehicleIdsQuery = _context.WarehouseVehicles.Select(wv => wv.VehicleId);

        var baseQuery = _context.Vehicles
            .Where(v => !assignedVehicleIdsQuery.Contains(v.Id));

        var totalCount = await baseQuery.CountAsync();

        var vehicles = await baseQuery
            .OrderBy(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(v => v.Driver)
            .ThenInclude(d => d.Person)
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                Model = v.Model,
                Color = v.Color,
                SmartCardCode = v.SmartCardCode,
                IsBroken = v.IsBroken,
                IsVan = v.IsVan,
                DriverFullName = v.Driver != null && v.Driver.Person != null
                    ? v.Driver.Person.FirstName + " " + v.Driver.Person.LastName
                    : "بدون راننده"
            })
            .ToListAsync();

        return new PagedResult<VehicleDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = vehicles
        };
    }


    private async Task<bool> IsAuthorizedAsync(long currentUserId)
    {
        var user = await _context.Persons.FindAsync(currentUserId);
        return user != null && (user.Role == SystemRole.admin || user.Role == SystemRole.superadmin);
    }


}
