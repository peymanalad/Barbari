using BarcopoloWebApi.DTOs;
using BarcopoloWebApi.DTOs.Address;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;
        private readonly ILogger<AddressController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public AddressController(IAddressService addressService, ILogger<AddressController> logger, IHttpContextAccessor contextAccessor)
        {
            _addressService = addressService;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        private long CurrentUserId => long.Parse(
            _contextAccessor.HttpContext.User.Claims.First(c => c.Type == "UserId").Value
        );

        private IActionResult HandleException(Exception ex, string message, object? data = null)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message, data });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressDto dto)
        {
            _logger.LogInformation("Creating address for person {PersonId}", dto.PersonId);
            try
            {
                var address = await _addressService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = address.Id }, address);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error creating address", dto);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("Fetching address with id {AddressId}", id);
            try
            {
                var address = await _addressService.GetByIdAsync(id, CurrentUserId);
                return Ok(address);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error fetching address", new { id });
            }
        }

        [HttpGet("person/{personId}")]
        public async Task<IActionResult> GetByPersonId(long personId)
        {
            _logger.LogInformation("Fetching addresses for person {PersonId}", personId);
            try
            {
                var addresses = await _addressService.GetByPersonIdAsync(personId, CurrentUserId);
                return Ok(addresses);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error fetching addresses by person", new { personId });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAddressDto dto)
        {
            _logger.LogInformation("Updating address with id {AddressId}", id);
            try
            {
                var updated = await _addressService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error updating address", new { id });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting address with id {AddressId}", id);
            try
            {
                var result = await _addressService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Address not found" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error deleting address", new { id });
            }
        }
    }
}
