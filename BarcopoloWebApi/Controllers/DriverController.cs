using BarcopoloWebApi.DTOs.Driver;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Extensions;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly ILogger<DriverController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public DriverController(IDriverService driverService, ILogger<DriverController> logger, IHttpContextAccessor contextAccessor)
        {
            _driverService = driverService;
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
        public async Task<IActionResult> Create([FromBody] CreateDriverDto driver)
        {
            _logger.LogInformation("Creating driver for person {PersonId}", driver.PersonId);
            var created = await _driverService.CreateAsync(driver, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateDriverDto dto)
        {
            var updated = await _driverService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _driverService.DeleteAsync(id, CurrentUserId);
            return result ? NoContent() : NotFound(new { error = "Driver not found" });
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignToVehicle([FromQuery] long driverId, [FromQuery] long vehicleId)
        {
            _logger.LogInformation("Assigning driver {DriverId} to vehicle {VehicleId}", driverId, vehicleId);
            await _driverService.AssignToVehicleAsync(driverId, vehicleId, CurrentUserId);
            return Ok(new { message = "Driver assigned to vehicle successfully." });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var driver = await _driverService.GetByIdAsync(id, CurrentUserId);
            return driver != null ? Ok(driver) : NotFound(new { error = "Driver not found" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var drivers = await _driverService.GetAllAsync(CurrentUserId);
            return Ok(drivers);
        }
        [AllowAnonymous]
        [HttpPost("self-register")]
        public async Task<IActionResult> SelfRegister([FromBody] SelfRegisterDriverDto dto)
        {
            _logger.LogInformation("Self-register driver attempt for {Phone}", dto.PhoneNumber.MaskSensitive());
            var result = await _driverService.SelfRegisterAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

    }
}
