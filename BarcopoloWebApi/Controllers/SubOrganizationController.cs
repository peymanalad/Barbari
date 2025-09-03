using BarcopoloWebApi.DTOs.SubOrganization;
using BarcopoloWebApi.Services.SubOrganization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SubOrganizationController : ControllerBase
    {
        private readonly ISubOrganizationService _subOrganizationService;
        private readonly ILogger<SubOrganizationController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public SubOrganizationController(
            ISubOrganizationService subOrganizationService,
            ILogger<SubOrganizationController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _subOrganizationService = subOrganizationService;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        private long CurrentUserId =>
            long.TryParse(_contextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value, out var id)
                ? id
                : throw new UnauthorizedAccessException("UserId claim not found");

        private IActionResult HandleError(Exception ex, string message, object? data = null)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message, data });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubOrganizationDto dto)
        {
            _logger.LogInformation("User {UserId} creating sub-organization for Org {OrgId}", CurrentUserId, dto.OrganizationId);
            var result = await _subOrganizationService.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("User {UserId} retrieving sub-organization {Id}", CurrentUserId, id);
            var result = await _subOrganizationService.GetByIdAsync(id, CurrentUserId);
            return result != null
                ? Ok(result)
                : NotFound(new { error = "شعبه یافت نشد", id });
        }

        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetByOrganization(long organizationId)
        {
            _logger.LogInformation("User {UserId} retrieving sub-orgs for Org {OrgId}", CurrentUserId, organizationId);
            var result = await _subOrganizationService.GetByOrganizationIdAsync(organizationId, CurrentUserId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateSubOrganizationDto dto)
        {
            _logger.LogInformation("User {UserId} updating sub-organization {Id}", CurrentUserId, id);
            var result = await _subOrganizationService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("User {UserId} deleting sub-organization {Id}", CurrentUserId, id);
            var result = await _subOrganizationService.DeleteAsync(id, CurrentUserId);
            return result
                ? NoContent()
                : NotFound(new { error = "شعبه یافت نشد", id });
        }
    }
}
