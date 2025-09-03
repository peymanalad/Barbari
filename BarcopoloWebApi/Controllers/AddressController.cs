using BarcopoloWebApi.DTOs.Address;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;
        private readonly IHttpContextAccessor _contextAccessor;

        public AddressController(IAddressService addressService,IHttpContextAccessor contextAccessor)
        {
            _addressService = addressService;
            _contextAccessor = contextAccessor;
        }

        private long CurrentUserId =>
            long.Parse(_contextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressDto dto)
        {
            var result = await _addressService.CreateAsync(dto, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _addressService.GetByIdAsync(id, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("person/{personId}")]
        public async Task<IActionResult> GetByPersonId(long personId)
        {
            var result = await _addressService.GetByPersonIdAsync(personId, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetByOrganizationId(long organizationId)
        {
            var result = await _addressService.GetByOrganizationIdAsync(organizationId, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranchId(long branchId)
        {
            var result = await _addressService.GetByBranchIdAsync(branchId, CurrentUserId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAddressDto dto)
        {
            var result = await _addressService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _addressService.DeleteAsync(id, CurrentUserId);
            return Ok(result);
        }
    }
}
