using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize(Roles = "admin,superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseVehicleController : ControllerBase
    {
        private readonly IWarehouseVehicleService _warehouseVehicleService;
        private readonly ILogger<WarehouseVehicleController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public WarehouseVehicleController(
            IWarehouseVehicleService warehouseVehicleService,
            ILogger<WarehouseVehicleController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _warehouseVehicleService = warehouseVehicleService;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_contextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");

        private IActionResult HandleError(Exception ex, string message)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message });
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignVehicle([FromQuery] long warehouseId, [FromQuery] long vehicleId)
        {
            _logger.LogInformation("Assigning vehicle {VehicleId} to warehouse {WarehouseId} by user {UserId}", vehicleId, warehouseId, CurrentUserId);
            try
            {
                await _warehouseVehicleService.AssignVehicleToWarehouse(warehouseId, vehicleId, CurrentUserId);
                return Ok(new { message = "Vehicle assigned to warehouse successfully." });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error assigning vehicle to warehouse");
            }
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveVehicle([FromQuery] long warehouseId, [FromQuery] long vehicleId)
        {
            _logger.LogInformation("Removing vehicle {VehicleId} from warehouse {WarehouseId}", vehicleId, warehouseId);
            try
            {
                var result = await _warehouseVehicleService.RemoveVehicleFromWarehouse(warehouseId, vehicleId, CurrentUserId);
                return result ? Ok(new { message = "Vehicle removed." }) : NotFound(new { error = "Assignment not found." });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error removing vehicle from warehouse");
            }
        }

        [HttpGet("warehouse/{warehouseId}")]
        public async Task<IActionResult> GetVehiclesByWarehouse(long warehouseId)
        {
            try
            {
                var vehicles = await _warehouseVehicleService.GetVehiclesByWarehouse(warehouseId, CurrentUserId);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving vehicles in warehouse");
            }
        }
    }
}
