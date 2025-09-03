using BarcopoloWebApi.DTOs.CargoType;
using BarcopoloWebApi.Services.CargoType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CargoTypeController : ControllerBase
    {
        private readonly ICargoTypeService _cargoTypeService;
        private readonly ILogger<CargoTypeController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CargoTypeController(
            ICargoTypeService cargoTypeService,
            ILogger<CargoTypeController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _cargoTypeService = cargoTypeService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_httpContextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");

        private IActionResult HandleError(Exception ex, string message, object? data = null)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message, data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var types = await _cargoTypeService.GetAllAsync();
            return Ok(types);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCargoTypeDto dto)
        {
            _logger.LogInformation("User {UserId} creating new CargoType", CurrentUserId);
            var result = await _cargoTypeService.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCargoTypeDto dto)
        {
            _logger.LogInformation("User {UserId} updating CargoType {CargoTypeId}", CurrentUserId, id);
            var result = await _cargoTypeService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("User {UserId} deleting CargoType {CargoTypeId}", CurrentUserId, id);
            var result = await _cargoTypeService.DeleteAsync(id, CurrentUserId);
            return result ? NoContent() : NotFound(new { error = "نوع بار یافت نشد" });
        }
    }
}
