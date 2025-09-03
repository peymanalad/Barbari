using BarcopoloWebApi.DTOs.Withdrawal;
using BarcopoloWebApi.Services;
using BarcopoloWebApi.Services.WalletManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WithdrawalRequestController : ControllerBase
    {
        private readonly IWithdrawalRequestService _withdrawalService;
        private readonly ILogger<WithdrawalRequestController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WithdrawalRequestController(
            IWithdrawalRequestService withdrawalService,
            ILogger<WithdrawalRequestController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _withdrawalService = withdrawalService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_httpContextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");

        private IActionResult HandleError(Exception ex, string message)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWithdrawalRequestDto dto)
        {
            _logger.LogInformation("درخواست برداشت توسط کاربر {UserId}", CurrentUserId);
            var result = await _withdrawalService.CreateAsync(dto, CurrentUserId);
            return Ok(result);
        }

        [HttpPost("review/{id}")]
        [Authorize(Roles = "admin,superadmin")]
        public async Task<IActionResult> Review(long id, [FromBody] WithdrawalReviewDto dto)
        {
            _logger.LogInformation("بررسی درخواست برداشت {RequestId} توسط کاربر {UserId}", id, CurrentUserId);
            var result = await _withdrawalService.ReviewAsync(id, dto, CurrentUserId);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var results = await _withdrawalService.GetRequestsAsync(CurrentUserId);
            return Ok(results);
        }
    }
}
