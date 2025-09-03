using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderVehicleAssignmentController : ControllerBase
    {
        private readonly IOrderVehicleAssignmentService _vehicleAssignmentService;
        private readonly ILogger<OrderVehicleAssignmentController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public OrderVehicleAssignmentController(
            IOrderVehicleAssignmentService vehicleAssignmentService,
            ILogger<OrderVehicleAssignmentController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _vehicleAssignmentService = vehicleAssignmentService;
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
        public async Task<IActionResult> Assign([FromQuery] long orderId, [FromQuery] long vehicleId)
        {
            _logger.LogInformation("Assigning vehicle {VehicleId} to order {OrderId}", vehicleId, orderId);
            await _vehicleAssignmentService.AssignAsync(orderId, vehicleId, CurrentUserId);
            return Ok(new { message = "Vehicle assigned to order successfully." });
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> Remove([FromQuery] long orderId, [FromQuery] long vehicleId)
        {
            _logger.LogInformation("Removing vehicle {VehicleId} from order {OrderId}", vehicleId, orderId);
            var result = await _vehicleAssignmentService.RemoveAsync(orderId, vehicleId, CurrentUserId);
            return result
                ? Ok(new { message = "Vehicle removed from order successfully." })
                : NotFound(new { error = "Assignment not found." });
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetVehiclesByOrderId(long orderId)
        {
            _logger.LogInformation("Getting assigned vehicles for order {OrderId}", orderId);
            var vehicles = await _vehicleAssignmentService.GetByOrderIdAsync(orderId, CurrentUserId);
            return Ok(vehicles);
        }
    }
}
