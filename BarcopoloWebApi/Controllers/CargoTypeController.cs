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
            try
            {
                var types = await _cargoTypeService.GetAllAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در دریافت لیست انواع بار");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCargoTypeDto dto)
        {
            _logger.LogInformation("User {UserId} creating new CargoType", CurrentUserId);
            try
            {
                var result = await _cargoTypeService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در ایجاد نوع بار", dto);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCargoTypeDto dto)
        {
            _logger.LogInformation("User {UserId} updating CargoType {CargoTypeId}", CurrentUserId, id);
            try
            {
                var result = await _cargoTypeService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در بروزرسانی نوع بار", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("User {UserId} deleting CargoType {CargoTypeId}", CurrentUserId, id);
            try
            {
                var result = await _cargoTypeService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "نوع بار یافت نشد" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در حذف نوع بار", new { id });
            }
        }
    }
}
