using BarcopoloWebApi.DTOs; // Ensure this contains your PagedResult<T>
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BarcopoloWebApi.Services.Order;

namespace BarcopoloWebApi.Controllers 
{
    [ApiController]
    [Route("api/[controller]")] 
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100; 

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger, IHttpContextAccessor contextAccessor)
        {
            _orderService = orderService;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_contextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");


        [HttpPost]
        [ProducesResponseType(typeof(OrderDto), 201)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Return object for error messages
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                _logger.LogInformation("در حال ثبت سفارش جدید برای کاربر {OwnerId} توسط {UserId}", dto.OwnerId, CurrentUserId);


                var createdOrder = await _orderService.CreateAsync(dto, CurrentUserId);

                _logger.LogInformation("سفارش {OrderId} با موفقیت برای کاربر {OwnerId} توسط {UserId} ثبت شد", createdOrder.Id, dto.OwnerId, CurrentUserId);
                return CreatedAtAction(nameof(GetOrderById), new { orderId = createdOrder.Id }, createdOrder);
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning(ex, "خطای BadRequest در ثبت سفارش جدید: {ErrorMessage}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "خطای آرگومان نامعتبر در ثبت سفارش جدید: {ErrorMessage}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "خطای NotFound در ثبت سفارش جدید: {ErrorMessage}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessAppException ex) 
            {
                _logger.LogWarning(ex, "خطای دسترسی (Forbidden) در ثبت سفارش توسط کاربر {UserId}: {ErrorMessage}", CurrentUserId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "خطای احراز هویت (Unauthorized) در ثبت سفارش: {ErrorMessage}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور در ثبت سفارش جدید برای کاربر {OwnerId} توسط {UserId}", dto.OwnerId, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست ثبت سفارش." });
            }
        }


        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<OrderDto>), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetAllOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
                pageNumber = Math.Max(1, pageNumber); 

                _logger.LogInformation("دریافت لیست تمام سفارش‌ها توسط کاربر {UserId} - صفحه {PageNumber} اندازه {PageSize}",
                    CurrentUserId, pageNumber, pageSize);


                var pagedOrders = await _orderService.GetAllAsync(CurrentUserId, pageNumber, pageSize);
                return Ok(pagedOrders);
            }
            catch (UnauthorizedAccessAppException ex)
            {
                _logger.LogWarning(ex, "خطای دسترسی (Forbidden) هنگام دریافت تمام سفارش‌ها توسط کاربر {UserId}", CurrentUserId);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "خطای احراز هویت (Unauthorized) هنگام دریافت تمام سفارش‌ها: {ErrorMessage}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور هنگام دریافت تمام سفارش‌ها توسط کاربر {UserId}", CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست دریافت سفارش‌ها." });
            }
        }


        [HttpGet("{orderId}")]
        [ProducesResponseType(typeof(OrderDto), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> GetOrderById([FromRoute] long orderId)
        {
            try
            {
                _logger.LogInformation("دریافت سفارش {OrderId} توسط کاربر {UserId}", orderId, CurrentUserId);
                var order = await _orderService.GetByIdAsync(orderId, CurrentUserId);
                return Ok(order);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("سفارش {OrderId} یافت نشد. درخواست توسط کاربر {UserId}.", orderId, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessAppException ex)
            {
                _logger.LogWarning("خطای دسترسی (Forbidden) به سفارش {OrderId} توسط کاربر {UserId}: {ErrorMessage}", orderId, CurrentUserId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "خطای احراز هویت (Unauthorized) هنگام دریافت سفارش {OrderId}: {ErrorMessage}", orderId, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور هنگام دریافت سفارش {OrderId} توسط کاربر {UserId}", orderId, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست دریافت سفارش." });
            }
        }


        [HttpGet("owner/{ownerId}")]
        [ProducesResponseType(typeof(PagedResult<OrderDto>), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetOrdersByOwner(
            [FromRoute] long ownerId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
                pageNumber = Math.Max(1, pageNumber);

                _logger.LogInformation("دریافت سفارش‌های کاربر {OwnerId} توسط {UserId} - صفحه {PageNumber} اندازه {PageSize}",
                                        ownerId, CurrentUserId, pageNumber, pageSize);

                // Using the IOrderService defined by me (returns PagedResult)
                var pagedOrders = await _orderService.GetByOwnerAsync(ownerId, CurrentUserId, pageNumber, pageSize);
                return Ok(pagedOrders);
            }
            catch (NotFoundException ex)             {
                _logger.LogWarning("خطای NotFound هنگام دریافت سفارش‌های کاربر {OwnerId} توسط {UserId}: {ErrorMessage}", ownerId, CurrentUserId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessAppException ex)
            {
                _logger.LogWarning("خطای دسترسی (Forbidden) به سفارش‌های کاربر {OwnerId} توسط {UserId}: {ErrorMessage}", ownerId, CurrentUserId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "خطای احراز هویت (Unauthorized) هنگام دریافت سفارش‌های کاربر {OwnerId}: {ErrorMessage}", ownerId, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور هنگام دریافت سفارش‌های کاربر {OwnerId} توسط {UserId}", ownerId, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست دریافت سفارش‌ها." });
            }
        }



        [HttpPost("{orderId}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOrder([FromRoute] long orderId, [FromQuery] string? reason = null)
        {
            try
            {
                _logger.LogInformation("در حال لغو سفارش {OrderId} توسط کاربر {UserId}. دلیل: {Reason}", orderId, CurrentUserId, reason ?? "مشخص نشده");
                await _orderService.CancelAsync(orderId, CurrentUserId, reason);
                _logger.LogInformation("سفارش {OrderId} با موفقیت توسط کاربر {UserId} لغو شد.", orderId, CurrentUserId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("سفارش {OrderId} برای لغو یافت نشد. درخواست توسط کاربر {UserId}.", orderId, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogWarning(ex, "عملیات لغو برای سفارش {OrderId} در وضعیت فعلی مجاز نیست: {ErrorMessage}", orderId, ex.Message);
                return BadRequest(new { message = ex.Message }); 
            }
            catch (UnauthorizedAccessAppException ex)
            {
                _logger.LogWarning("خطای دسترسی (Forbidden) برای لغو سفارش {OrderId} توسط کاربر {UserId}: {ErrorMessage}", orderId, CurrentUserId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "خطای احراز هویت (Unauthorized) هنگام لغو سفارش {OrderId}: {ErrorMessage}", orderId, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور هنگام لغو سفارش {OrderId} توسط کاربر {UserId}", orderId, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست لغو سفارش." });
            }
        }


        [HttpPut("{orderId}")]
        [ProducesResponseType(typeof(OrderDto), 200)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> UpdateOrder([FromRoute] long orderId, [FromBody] UpdateOrderDto dto)
        {
            try
            {
                _logger.LogInformation("بروزرسانی سفارش {OrderId} توسط {UserId}", orderId, CurrentUserId);
                var updatedOrder = await _orderService.UpdateAsync(orderId, dto, CurrentUserId);
                _logger.LogInformation("سفارش {OrderId} با موفقیت توسط کاربر {UserId} به‌روز شد.", orderId, CurrentUserId);
                return Ok(updatedOrder);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("سفارش {OrderId} برای به‌روزرسانی یافت نشد. درخواست توسط کاربر {UserId}.", orderId, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogWarning(ex, "عملیات به‌روزرسانی برای سفارش {OrderId} در وضعیت فعلی مجاز نیست: {ErrorMessage}", orderId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (BadRequestException ex) 
            {
                _logger.LogWarning(ex, "خطای BadRequest در به‌روزرسانی سفارش {OrderId}: {ErrorMessage}", orderId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessAppException ex)
            {
                _logger.LogWarning("خطای دسترسی (Forbidden) برای به‌روزرسانی سفارش {OrderId} توسط کاربر {UserId}: {ErrorMessage}", orderId, CurrentUserId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "خطای احراز هویت (Unauthorized) هنگام به‌روزرسانی سفارش {OrderId}: {ErrorMessage}", orderId, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور هنگام به‌روزرسانی سفارش {OrderId} توسط کاربر {UserId}", orderId, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست به‌روزرسانی سفارش." });
            }
        }


        [HttpGet("status/{trackingNumber}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderStatusDto), 200)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderStatusDto>> GetOrderStatusByTrackingNumber([FromRoute] string trackingNumber)
        {
            try
            {
                _logger.LogInformation("در حال بررسی وضعیت سفارش با شماره پیگیری {TrackingNumber}", trackingNumber);
                var status = await _orderService.GetByTrackingNumberAsync(trackingNumber);
                return Ok(status);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("سفارشی با شماره پیگیری {TrackingNumber} یافت نشد.", trackingNumber);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور هنگام بررسی وضعیت شماره پیگیری {TrackingNumber}", trackingNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست وضعیت سفارش." });
            }
        }


        [HttpPatch("{orderId}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeOrderStatus([FromRoute] long orderId, [FromBody] ChangeOrderStatusDto dto)
        {
            try
            {
                _logger.LogInformation("تغییر وضعیت سفارش {OrderId} به {NewStatus} توسط {UserId}. توضیحات: {Remarks}",
                                       orderId, dto.NewStatus, CurrentUserId, dto.Remarks ?? "ندارد");

                await _orderService.ChangeStatusAsync(orderId, dto, CurrentUserId);
                _logger.LogInformation("وضعیت سفارش {OrderId} با موفقیت به {NewStatus} توسط کاربر {UserId} تغییر یافت.", orderId, dto.NewStatus, CurrentUserId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("سفارش {OrderId} برای تغییر وضعیت یافت نشد. درخواست توسط کاربر {UserId}.", orderId, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "تغییر وضعیت سفارش {OrderId} به {NewStatus} مجاز نیست: {ErrorMessage}", orderId, dto.NewStatus, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "خطای آرگومان نامعتبر هنگام تغییر وضعیت سفارش {OrderId}: {ErrorMessage}", orderId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessAppException ex)
            {
                _logger.LogWarning("خطای دسترسی (Forbidden) برای تغییر وضعیت سفارش {OrderId} توسط کاربر {UserId}: {ErrorMessage}", orderId, CurrentUserId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "خطای احراز هویت (Unauthorized) هنگام تغییر وضعیت سفارش {OrderId}: {ErrorMessage}", orderId, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای داخلی سرور هنگام تغییر وضعیت سفارش {OrderId} به {NewStatus} توسط کاربر {UserId}", orderId, dto.NewStatus, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "خطا در پردازش درخواست تغییر وضعیت سفارش." });
            }
        }
    }
}