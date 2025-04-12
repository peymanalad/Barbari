using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.Services.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger, IHttpContextAccessor contextAccessor)
        {
            _orderService = orderService;
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
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            _logger.LogInformation("Creating order by user {UserId}", CurrentUserId);
            try
            {
                var order = await _orderService.CreateAsync(dto,CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating order", dto);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var order = await _orderService.GetByIdAsync(id, CurrentUserId);
                return order != null ? Ok(order) : NotFound(new { error = "Order not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving order", new { id });
            }
        }

        [AllowAnonymous]
        [HttpGet("tracking/{trackingNumber}")]
        public async Task<IActionResult> GetByTracking(string trackingNumber)
        {
            try
            {
                var order = await _orderService.GetByTrackingNumberAsync(trackingNumber);
                return order != null ? Ok(order) : NotFound(new { error = "Order not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving order by tracking", new { trackingNumber });
            }
        }

        [HttpGet("owner/{ownerId}")]
        public async Task<IActionResult> GetByOwner(long ownerId)
        {
            try
            {
                var orders = await _orderService.GetByOwnerAsync(ownerId, CurrentUserId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving orders by owner", new { ownerId });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOrderDto dto)
        {
            _logger.LogInformation("Updating order {OrderId}", id);
            try
            {
                var updated = await _orderService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating order", new { id });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            _logger.LogInformation("Canceling order {OrderId} by user {UserId}", id, CurrentUserId);
            try
            {
                var result = await _orderService.CancelAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Order not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error canceling order", new { id });
            }
        }

        [HttpPut("{orderId}/status")]
        public async Task<IActionResult> ChangeStatus(long orderId, [FromBody] ChangeOrderStatusDto dto)
        {
            _logger.LogInformation("User {UserId} is attempting to change status of order {OrderId}", CurrentUserId, orderId);
            try
            {
                var result = await _orderService.ChangeStatusAsync(orderId, dto.NewStatus, dto.Remarks, CurrentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در تغییر وضعیت سفارش", new { orderId, dto.NewStatus });
            }
        }


    }
}
