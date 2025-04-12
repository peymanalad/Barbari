using BarcopoloWebApi.DTOs.Wallet;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BarcopoloWebApi.Services.Wallet;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WalletTransactionController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletTransactionController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WalletTransactionController(
            IWalletService walletService,
            ILogger<WalletTransactionController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _walletService = walletService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue("UserId") ?? "0");

        [HttpGet("{walletId}/transactions")]
        public async Task<IActionResult> GetTransactions(long walletId, [FromQuery] TransactionFilterDto filter)
        {
            _logger.LogInformation("دریافت لیست تراکنش‌ها برای کیف پول {WalletId} توسط کاربر {UserId}", walletId, CurrentUserId);

            try
            {
                var transactions = await _walletService.GetTransactionsAsync(walletId, filter, CurrentUserId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت تراکنش‌های کیف پول {WalletId}", walletId);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}