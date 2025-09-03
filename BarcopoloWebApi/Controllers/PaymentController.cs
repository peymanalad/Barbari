using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger, IHttpContextAccessor contextAccessor)
        {
            _paymentService = paymentService;
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
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            _logger.LogInformation("Creating payment for OrderId {OrderId}", dto.OrderId);
            var payment = await _paymentService.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var payment = await _paymentService.GetByIdAsync(id, CurrentUserId);
            return payment != null ? Ok(payment) : NotFound(new { error = "Payment not found" });
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(long orderId)
        {
            var payments = await _paymentService.GetByOrderIdAsync(orderId, CurrentUserId);
            return Ok(payments);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePaymentDto dto)
        {
            _logger.LogInformation("Updating payment {PaymentId}", id);
            var updated = await _paymentService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting payment {PaymentId}", id);
            var result = await _paymentService.DeleteAsync(id, CurrentUserId);
            return result ? NoContent() : NotFound(new { error = "Payment not found" });
        }

        [HttpGet("remaining/{orderId}")]
        public async Task<IActionResult> GetRemaining(long orderId)
        {
            var remaining = await _paymentService.GetRemainingAmountAsync(orderId, CurrentUserId);
            return Ok(new { orderId, remainingAmount = remaining });
        }
    }
}
