using BarcopoloWebApi.Services.Address;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FrequentAddressController : ControllerBase
    {
        private readonly IFrequentAddressService _frequentAddressService;
        private readonly ILogger<FrequentAddressController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public FrequentAddressController(
            IFrequentAddressService frequentAddressService,
            ILogger<FrequentAddressController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _frequentAddressService = frequentAddressService;
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

        [HttpGet("origin")]
        public async Task<IActionResult> GetOrigins()
        {
            try
            {
                var origins = await _frequentAddressService.GetAccessibleOriginsAsync(CurrentUserId);
                return Ok(origins);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در دریافت آدرس‌های مبدا");
            }
        }

        [HttpGet("destination")]
        public async Task<IActionResult> GetDestinations()
        {
            try
            {
                var destinations = await _frequentAddressService.GetAccessibleDestinationsAsync(CurrentUserId);
                return Ok(destinations);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "خطا در دریافت آدرس‌های مقصد");
            }
        }
    }
}