using BarcopoloWebApi.DTOs.Vehicle;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILogger<VehicleController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public VehicleController(IVehicleService vehicleService, ILogger<VehicleController> logger, IHttpContextAccessor contextAccessor)
        {
            _vehicleService = vehicleService;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_contextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");

        private IActionResult HandleError(Exception ex, string message, object? data = null)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message, data });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            _logger.LogInformation("User {UserId} creating vehicle", CurrentUserId);
            try
            {
                var vehicle = await _vehicleService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating vehicle", dto);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateVehicleDto dto)
        {
            _logger.LogInformation("User {UserId} updating vehicle {VehicleId}", CurrentUserId, id);
            try
            {
                var updated = await _vehicleService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating vehicle", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("User {UserId} deleting vehicle {VehicleId}", CurrentUserId, id);
            try
            {
                var result = await _vehicleService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Vehicle not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting vehicle", new { id });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var vehicle = await _vehicleService.GetByIdAsync(id, CurrentUserId);
                return vehicle != null ? Ok(vehicle) : NotFound(new { error = "Vehicle not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving vehicle", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var vehicles = await _vehicleService.GetAllAsync(CurrentUserId);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving vehicles");
            }
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable()
        {
            try
            {
                var vehicles = await _vehicleService.GetAvailableAsync(CurrentUserId);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving available vehicles");
            }
        }

        [HttpGet("by-driver/{driverId}")]
        public async Task<IActionResult> GetByDriverId(long driverId)
        {
            try
            {
                var vehicles = await _vehicleService.GetByDriverIdAsync(driverId, CurrentUserId);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving vehicles by driver", new { driverId });
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] VehicleFilterDto filter)
        {
            try
            {
                var vehicles = await _vehicleService.SearchAsync(filter, CurrentUserId);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error searching vehicles", filter);
            }
        }

        [HttpGet("broken-count")]
        public async Task<IActionResult> GetBrokenCount()
        {
            try
            {
                var count = await _vehicleService.GetBrokenCountAsync(CurrentUserId);
                return Ok(new { brokenCount = count });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error getting broken vehicle count");
            }
        }

    }
}
