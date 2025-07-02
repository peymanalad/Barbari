using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FrequentAddressController : ControllerBase
{
    private readonly IFrequentAddressService _frequentAddressService;

    public FrequentAddressController(IFrequentAddressService frequentAddressService)
    {
        _frequentAddressService = frequentAddressService;
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

    private long GetCurrentUserId()
    {
        return long.Parse(User.FindFirst("sub")?.Value ?? "0");
    }
}