using BarcopoloWebApi.DTOs.Warehouse;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        private readonly ILogger<WarehouseController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public WarehouseController(IWarehouseService warehouseService, IHttpContextAccessor contextAccessor, ILogger<WarehouseController> logger)
        {
            _warehouseService = warehouseService;
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
        public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
        {
            _logger.LogInformation("User {UserId} creating warehouse '{Name}'", CurrentUserId, dto.WarehouseName);
            try
            {
                var warehouse = await _warehouseService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, warehouse);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating warehouse", dto);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var warehouse = await _warehouseService.GetByIdAsync(id, CurrentUserId);
                return warehouse != null ? Ok(warehouse) : NotFound(new { error = "Warehouse not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving warehouse", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllAsync(CurrentUserId);
                return Ok(warehouses);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving warehouses");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateWarehouseDto dto)
        {
            _logger.LogInformation("User {UserId} updating warehouse {WarehouseId}", CurrentUserId, id);
            try
            {
                var warehouse = await _warehouseService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(warehouse);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating warehouse", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("User {UserId} deleting warehouse {WarehouseId}", CurrentUserId, id);
            try
            {
                var result = await _warehouseService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Warehouse not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting warehouse", new { id });
            }
        }
    }
}
