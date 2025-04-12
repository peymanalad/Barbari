using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.Services.OrderEvent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderEventController : ControllerBase
    {
        private readonly IOrderEventService _orderEventService;
        private readonly ILogger<OrderEventController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public OrderEventController(IOrderEventService orderEventService, ILogger<OrderEventController> logger, IHttpContextAccessor contextAccessor)
        {
            _orderEventService = orderEventService;
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
        public async Task<IActionResult> Create([FromBody] CreateOrderEventDto dto)
        {
            _logger.LogInformation("User {UserId} adding event to order {OrderId}", CurrentUserId, dto.OrderId);
            try
            {
                var orderEvent = await _orderEventService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetByOrderId), new { orderId = orderEvent.OrderId }, orderEvent);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating order event", dto);
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(long orderId)
        {
            try
            {
                var events = await _orderEventService.GetByOrderIdAsync(orderId, CurrentUserId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving order events", new { orderId });
            }
        }
    }
}
