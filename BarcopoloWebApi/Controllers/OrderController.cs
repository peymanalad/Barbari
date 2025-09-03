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
using BarcopoloWebApi.Entities;

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
            _logger.LogInformation("در حال ثبت سفارش جدید برای کاربر {OwnerId} توسط {UserId}", dto.OwnerId, CurrentUserId);


            var createdOrder = await _orderService.CreateAsync(dto, CurrentUserId);

            _logger.LogInformation("سفارش {OrderId} با موفقیت برای کاربر {OwnerId} توسط {UserId} ثبت شد", createdOrder.Id, dto.OwnerId, CurrentUserId);
            return CreatedAtAction(nameof(GetOrderById), new { orderId = createdOrder.Id }, createdOrder);
        }


        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<OrderDto>), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetAllOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            page = Math.Max(1, page);

            _logger.LogInformation("دریافت لیست تمام سفارش‌ها توسط کاربر {UserId} - صفحه {PageNumber} اندازه {PageSize}",
                CurrentUserId, page, pageSize);

            var pagedOrders = await _orderService.GetAllAsync(CurrentUserId, page, pageSize);
            return Ok(pagedOrders);
        }


        [HttpGet("{orderId}")]
        [ProducesResponseType(typeof(OrderDto), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> GetOrderById([FromRoute] long orderId)
        {
            _logger.LogInformation("دریافت سفارش {OrderId} توسط کاربر {UserId}", orderId, CurrentUserId);
            var order = await _orderService.GetByIdAsync(orderId, CurrentUserId);
            return Ok(order);
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
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            pageNumber = Math.Max(1, pageNumber);

            _logger.LogInformation("دریافت سفارش‌های کاربر {OwnerId} توسط {UserId} - صفحه {PageNumber} اندازه {PageSize}",
                                    ownerId, CurrentUserId, pageNumber, pageSize);

            var pagedOrders = await _orderService.GetByOwnerAsync(ownerId, CurrentUserId, pageNumber, pageSize);
            return Ok(pagedOrders);
        }



        [HttpPost("{orderId}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOrder([FromRoute] long orderId, [FromQuery] string? reason = null)
        {
            _logger.LogInformation("در حال لغو سفارش {OrderId} توسط کاربر {UserId}. دلیل: {Reason}", orderId, CurrentUserId, reason ?? "مشخص نشده");
            await _orderService.CancelAsync(orderId, CurrentUserId, reason);
            _logger.LogInformation("سفارش {OrderId} با موفقیت توسط کاربر {UserId} لغو شد.", orderId, CurrentUserId);
            return NoContent();
        }


        [HttpPut("{orderId}")]
        [ProducesResponseType(typeof(OrderDto), 200)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> UpdateOrder([FromRoute] long orderId, [FromBody] UpdateOrderDto dto)
        {
            _logger.LogInformation("بروزرسانی سفارش {OrderId} توسط {UserId}", orderId, CurrentUserId);
            var updatedOrder = await _orderService.UpdateAsync(orderId, dto, CurrentUserId);
            _logger.LogInformation("سفارش {OrderId} با موفقیت توسط کاربر {UserId} به‌روز شد.", orderId, CurrentUserId);
            return Ok(updatedOrder);
        }


        [HttpGet("status/{trackingNumber}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderStatusDto), 200)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderStatusDto>> GetOrderStatusByTrackingNumber([FromRoute] string trackingNumber)
        {
            _logger.LogInformation("در حال بررسی وضعیت سفارش با شماره پیگیری {TrackingNumber}", trackingNumber);
            var status = await _orderService.GetByTrackingNumberAsync(trackingNumber);
            return Ok(status);
        }


        [HttpPatch("{orderId}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeOrderStatus([FromRoute] long orderId, [FromBody] ChangeOrderStatusDto dto)
        {
            _logger.LogInformation("تغییر وضعیت سفارش {OrderId} به {NewStatus} توسط {UserId}. توضیحات: {Remarks}",
                                   orderId, dto.NewStatus, CurrentUserId, dto.Remarks ?? "ندارد");
            await _orderService.ChangeStatusAsync(orderId, dto, CurrentUserId);
            _logger.LogInformation("وضعیت سفارش {OrderId} با موفقیت به {NewStatus} توسط کاربر {UserId} تغییر یافت.", orderId, dto.NewStatus, CurrentUserId);
            return NoContent();
        }


        [HttpPatch("{orderId}/assign-personnel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignOrderPersonnel([FromRoute] long orderId, [FromBody] AssignOrderPersonnelDto dto)
        {
            _logger.LogInformation("در حال اختصاص نقش‌های جدید به سفارش {OrderId} توسط کاربر {UserId}", orderId, CurrentUserId);
            await _orderService.AssignOrderPersonnelAsync(orderId, dto, CurrentUserId);
            return NoContent();
        }

    }
}