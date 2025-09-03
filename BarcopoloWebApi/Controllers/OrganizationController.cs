using BarcopoloWebApi.DTOs;
using BarcopoloWebApi.DTOs.Organization;
using BarcopoloWebApi.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _orgService;
        private readonly ILogger<OrganizationController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public OrganizationController(IOrganizationService orgService, ILogger<OrganizationController> logger, IHttpContextAccessor contextAccessor)
        {
            _orgService = orgService;
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
        {
            _logger.LogInformation("Creating organization '{OrgName}' by user {UserId}", dto.Name, CurrentUserId);
            var org = await _orgService.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = org.Id }, org);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var org = await _orgService.GetByIdAsync(id, CurrentUserId);
            return org != null ? Ok(org) : NotFound(new { message = "سازمان یافت نشد" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orgs = await _orgService.GetAllAsync(CurrentUserId);
            return Ok(orgs);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOrganizationDto dto)
        {
            _logger.LogInformation("Updating organization {OrgId} by user {UserId}", id, CurrentUserId);
            var updated = await _orgService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting organization {OrgId} by user {UserId}", id, CurrentUserId);
            var result = await _orgService.DeleteAsync(id, CurrentUserId);
            return result ? NoContent() : NotFound(new { message = "سازمان یافت نشد" });
        }
    }
}
