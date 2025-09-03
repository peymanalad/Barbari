using BarcopoloWebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FrequentAddressController : ControllerBase
{
    private readonly IFrequentAddressService _frequentAddressService;
    private readonly ILogger<MembershipController> _logger;

    public FrequentAddressController(IFrequentAddressService frequentAddressService, ILogger<MembershipController> logger)
    {
        _frequentAddressService = frequentAddressService;
        _logger = logger;
    }

    [HttpPost("origins")]
    public async Task<ActionResult<List<FrequentAddressDto>>> GetOrigins([FromBody] FrequentAddressScope scope)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _frequentAddressService.GetOriginsAsync(currentUserId, scope);
        return Ok(result);
    }

    [HttpPost("destinations")]
    public async Task<ActionResult<List<FrequentAddressDto>>> GetDestinations([FromBody] FrequentAddressScope scope)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _frequentAddressService.GetDestinationsAsync(currentUserId, scope);
        return Ok(result);
    }

    [HttpGet("frequent")]
    public async Task<IActionResult> GetFrequentAddresses(
        [FromQuery] FrequentAddressType type,
        [FromQuery] bool isForOrganization,
        [FromQuery] long? organizationId,
        [FromQuery] long? branchId)
    {
        var list = await _frequentAddressService.GetFrequentAddressesAsync(
            GetCurrentUserId(), type, isForOrganization, organizationId, branchId);
        return Ok(list);
    }
    
    private IActionResult HandleError(Exception ex, string message, object? data = null)
    {
        _logger.LogError(ex, message);
        return BadRequest(new { error = ex.Message, data });
    }



    private long GetCurrentUserId()
    {
        return long.Parse(User.FindFirst("sub")?.Value ?? "0");
    }
}