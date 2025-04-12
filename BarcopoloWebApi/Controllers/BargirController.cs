using BarcopoloWebApi.DTOs.Bargir;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize(Roles = "admin,superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class BargirController : ControllerBase
    {
        private readonly IBargirService _bargirService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<BargirController> _logger;

        public BargirController(IBargirService bargirService, IHttpContextAccessor contextAccessor, ILogger<BargirController> logger)
        {
            _bargirService = bargirService;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        private long CurrentUserId =>
            long.Parse(_contextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");

        private IActionResult HandleError(Exception ex, string message, object? data = null)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message, data });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBargirDto dto)
        {
            _logger.LogInformation("Creating new Bargir");
            try
            {
                var bargir = await _bargirService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = bargir.Id }, bargir);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating Bargir", dto);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("Getting Bargir with id {Id}", id);
            try
            {
                var bargir = await _bargirService.GetByIdAsync(id, CurrentUserId);
                return bargir != null ? Ok(bargir) : NotFound(new { error = "Bargir not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving Bargir", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Fetching all Bargirs");
            try
            {
                var bargeers = await _bargirService.GetAllAsync(CurrentUserId);
                return Ok(bargeers);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error fetching Bargirs");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateBargirDto dto)
        {
            _logger.LogInformation("Updating Bargir with id {Id}", id);
            try
            {
                var updated = await _bargirService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating Bargir", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting Bargir with id {Id}", id);
            try
            {
                var result = await _bargirService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Bargir not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting Bargir", new { id });
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignToVehicle([FromQuery] long bargirId, [FromQuery] long vehicleId)
        {
            _logger.LogInformation("Assigning Bargir {BargirId} to vehicle {VehicleId}", bargirId, vehicleId);
            try
            {
                await _bargirService.AssignToVehicleAsync(bargirId, vehicleId, CurrentUserId);
                return Ok(new { message = "Bargir assigned to vehicle successfully." });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error assigning Bargir to vehicle", new { bargirId, vehicleId });
            }
        }
    }
}
