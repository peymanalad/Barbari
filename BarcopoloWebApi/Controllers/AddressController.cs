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
            try
            {
                var result = await _addressService.CreateAsync(dto, CurrentUserId);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }

            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطای غیرمنتظره‌ای رخ داده است.", detail = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await _addressService.GetByIdAsync(id, CurrentUserId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطای غیرمنتظره‌ای رخ داده است.", detail = ex.Message });
            }
        }

        [HttpGet("person/{personId}")]
        public async Task<IActionResult> GetByPersonId(long personId)
        {
            try
            {
                var result = await _addressService.GetByPersonIdAsync(personId, CurrentUserId);
                return Ok(result);
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطای غیرمنتظره‌ای رخ داده است.", detail = ex.Message });
            }
        }

        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetByOrganizationId(long organizationId)
        {
            try
            {
                var result = await _addressService.GetByOrganizationIdAsync(organizationId, CurrentUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطای غیرمنتظره‌ای رخ داده است.", detail = ex.Message });
            }
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranchId(long branchId)
        {
            try
            {
                var result = await _addressService.GetByBranchIdAsync(branchId, CurrentUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطای غیرمنتظره‌ای رخ داده است.", detail = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAddressDto dto)
        {
            try
            {
                var result = await _addressService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطای غیرمنتظره‌ای رخ داده است.", detail = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _addressService.DeleteAsync(id, CurrentUserId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطای غیرمنتظره‌ای رخ داده است.", detail = ex.Message });
            }
        }
    }
}
