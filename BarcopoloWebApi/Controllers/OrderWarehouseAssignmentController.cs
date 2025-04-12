using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize(Roles = "admin,superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderWarehouseAssignmentController : ControllerBase
    {
        private readonly IOrderWarehouseAssignmentService _service;
        private readonly ILogger<OrderWarehouseAssignmentController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public OrderWarehouseAssignmentController(
            IOrderWarehouseAssignmentService service,
            ILogger<OrderWarehouseAssignmentController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _service = service;
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

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromQuery] long orderId, [FromQuery] long warehouseId)
        {
            _logger.LogInformation("User {UserId} assigning warehouse {WarehouseId} to order {OrderId}", CurrentUserId, warehouseId, orderId);

            try
            {
                await _service.AssignAsync(orderId, warehouseId, CurrentUserId);
                return Ok(new { message = "انبار با موفقیت به سفارش اختصاص یافت." });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در اختصاص انبار", new { orderId, warehouseId });
            }
        }

        [HttpGet("{orderId}/assigned")]
        public async Task<IActionResult> GetAssignedWarehouse(long orderId)
        {
            _logger.LogInformation("User {UserId} requesting assigned warehouse for order {OrderId}", CurrentUserId, orderId);

            try
            {
                var warehouseId = await _service.GetAssignedWarehouseIdAsync(orderId, CurrentUserId);
                return Ok(new { warehouseId });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در دریافت انبار اختصاص یافته", new { orderId });
            }
        }

        [HttpDelete("{orderId}/remove")]
        public async Task<IActionResult> Remove(long orderId)
        {
            _logger.LogInformation("User {UserId} removing warehouse assignment from order {OrderId}", CurrentUserId, orderId);

            try
            {
                var result = await _service.RemoveAsync(orderId, CurrentUserId);
                return result ? Ok(new { message = "انبار از سفارش حذف شد." }) :
                                NotFound(new { message = "سفارش یا انبار مرتبط یافت نشد." });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در حذف انبار از سفارش", new { orderId });
            }
        }
    }
}
