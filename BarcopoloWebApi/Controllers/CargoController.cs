using BarcopoloWebApi.DTOs.Cargo;
using BarcopoloWebApi.Services.Cargo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CargoController : ControllerBase
    {
        private readonly ICargoService _cargoService;
        private readonly ILogger<CargoController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public CargoController(ICargoService cargoService, ILogger<CargoController> logger, IHttpContextAccessor contextAccessor)
        {
            _cargoService = cargoService;
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
        public async Task<IActionResult> Create([FromBody] CreateCargoDto dto)
        {
            _logger.LogInformation("Creating cargo for order {OrderId}", dto.OrderId);
            try
            {
                var cargo = await _cargoService.CreateAsync(dto,CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = cargo.Id }, cargo);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating cargo", dto);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var cargo = await _cargoService.GetByIdAsync(id, CurrentUserId);
                return cargo != null ? Ok(cargo) : NotFound(new { error = "Cargo not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving cargo", new { id });
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(long orderId)
        {
            try
            {
                var cargos = await _cargoService.GetByOrderIdAsync(orderId, CurrentUserId);
                return Ok(cargos);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving cargos by order", new { orderId });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCargoDto dto)
        {
            _logger.LogInformation("Updating cargo with id {CargoId}", id);
            try
            {
                var updated = await _cargoService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating cargo", new { id });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting cargo with id {CargoId}", id);
            try
            {
                var result = await _cargoService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Cargo not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting cargo", new { id });
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchCargos([FromQuery] CargoSearchDto input)
        {
            try
            {
                var result = await _cargoService.SearchAsync(input, CurrentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در جستجوی بارها");
            }
        }


    }
}
