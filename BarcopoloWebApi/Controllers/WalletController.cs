using BarcopoloWebApi.DTOs.Wallet;
using BarcopoloWebApi.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BarcopoloWebApi.Services.WalletManagement;

namespace BarcopoloWebApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public WalletController(
            IWalletService walletService,
            ILogger<WalletController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _walletService = walletService;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_contextAccessor.HttpContext?.User?.FindFirstValue("UserId") ?? "0");


        [HttpGet("details")]
        public async Task<IActionResult> GetWalletDetails([FromQuery] bool organizationMode)
        {
            _logger.LogInformation("دریافت اطلاعات کیف پول برای کاربر {UserId} - حالت سازمان: {OrgMode}", CurrentUserId, organizationMode);
            var wallet = await _walletService.GetWalletDetailsAsync(CurrentUserId, organizationMode);
            return Ok(wallet);
        }

        [HttpPost("{walletId}/transactions")]
        public async Task<IActionResult> GetTransactions(long walletId, [FromBody] TransactionFilterDto filter)
        {
            _logger.LogInformation("دریافت تراکنش‌های کیف پول {WalletId} توسط کاربر {UserId}", walletId, CurrentUserId);
            var transactions = await _walletService.GetTransactionsAsync(walletId, filter, CurrentUserId);
            return Ok(transactions);
        }


        [HttpGet("access/{walletId}")]
        public async Task<IActionResult> GetWalletAccess(long walletId)
        {
            var access = await _walletService.GetWalletAccessLevelAsync(walletId, CurrentUserId);
            return Ok(new { access = access.ToString() });
        }


        [HttpPost("charge")]
        public async Task<IActionResult> Charge([FromBody] ChargeWalletDto dto)
        {
            _logger.LogInformation("درخواست شارژ کیف پول {WalletId} توسط کاربر {UserId}", dto.WalletId, CurrentUserId);
            await _walletService.ChargeWalletAsync(dto, CurrentUserId);
            return Ok(new { message = "کیف پول با موفقیت شارژ شد." });
        }

        [HttpPost("pay")]
        public async Task<IActionResult> PayWithWallet([FromQuery] long walletId, [FromQuery] decimal amount, [FromQuery] long orderId)
        {
            await _walletService.PayWithWalletAsync(walletId, amount, orderId, CurrentUserId);
            return Ok(new { message = "پرداخت با موفقیت انجام شد." });
        }


        [HttpGet("has-sufficient-balance")]
        public async Task<IActionResult> HasSufficientBalance([FromQuery] long walletId, [FromQuery] decimal amount)
        {
            var isEnough = await _walletService.HasSufficientBalanceAsync(walletId, amount, CurrentUserId);
            return Ok(new { isSufficient = isEnough });
        }
    }
}
