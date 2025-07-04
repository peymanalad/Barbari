﻿using BarcopoloWebApi.DTOs.Feedback;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<FeedbackController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public FeedbackController(IFeedbackService feedbackService, ILogger<FeedbackController> logger, IHttpContextAccessor contextAccessor)
        {
            _feedbackService = feedbackService;
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
        public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
        {
            _logger.LogInformation("Creating feedback for OrderId {OrderId}", dto.OrderId);
            try
            {
                var feedback = await _feedbackService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetByOrderId), new { orderId = feedback.OrderId }, feedback);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating feedback", dto);
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(long orderId)
        {
            try
            {
                var feedback = await _feedbackService.GetByOrderIdAsync(orderId, CurrentUserId);
                return feedback != null ? Ok(feedback) : NotFound(new { error = "Feedback not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving feedback", new { orderId });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateFeedbackDto dto)
        {
            _logger.LogInformation("Updating feedback with Id {Id}", id);
            try
            {
                var updated = await _feedbackService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating feedback", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting feedback with Id {Id}", id);
            try
            {
                var result = await _feedbackService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Feedback not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting feedback", new { id });
            }
        }
    }
}
