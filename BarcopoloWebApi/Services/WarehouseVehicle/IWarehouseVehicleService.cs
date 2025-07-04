﻿using BarcopoloWebApi.DTOs.Vehicle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BarcopoloWebApi.Services
{
    public interface IWarehouseVehicleService
    {
        Task AssignVehicleToWarehouse(long warehouseId, long vehicleId, long currentUserId);
        Task<bool> RemoveVehicleFromWarehouse(long warehouseId, long vehicleId, long currentUserId);
        Task<IEnumerable<VehicleDto>> GetVehiclesByWarehouse(long warehouseId, long currentUserId);
        Task<IEnumerable<VehicleDto>> GetUnassignedVehicles(long currentUserId);

    }
}