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
            var vehicle = await _vehicleService.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateVehicleDto dto)
        {
            _logger.LogInformation("User {UserId} updating vehicle {VehicleId}", CurrentUserId, id);
            var updated = await _vehicleService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("User {UserId} deleting vehicle {VehicleId}", CurrentUserId, id);
            var result = await _vehicleService.DeleteAsync(id, CurrentUserId);
            return result ? NoContent() : NotFound(new { error = "Vehicle not found" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id, CurrentUserId);
            return vehicle != null ? Ok(vehicle) : NotFound(new { error = "Vehicle not found" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vehicles = await _vehicleService.GetAllAsync(CurrentUserId);
            return Ok(vehicles);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable()
        {
            var vehicles = await _vehicleService.GetAvailableAsync(CurrentUserId);
            return Ok(vehicles);
        }

        [HttpGet("by-driver/{driverId}")]
        public async Task<IActionResult> GetByDriverId(long driverId)
        {
            var vehicles = await _vehicleService.GetByDriverIdAsync(driverId, CurrentUserId);
            return Ok(vehicles);
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] VehicleFilterDto filter)
        {
            var vehicles = await _vehicleService.SearchAsync(filter, CurrentUserId);
            return Ok(vehicles);
        }

        [HttpGet("broken-count")]
        public async Task<IActionResult> GetBrokenCount()
        {
            var count = await _vehicleService.GetBrokenCountAsync(CurrentUserId);
            return Ok(new { brokenCount = count });
        }

    }
}
