using BarcopoloWebApi.DTOs.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationCargoTypeController : ControllerBase
    {
        private readonly IOrganizationCargoTypeService _service;
        private readonly ILogger<OrganizationCargoTypeController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public OrganizationCargoTypeController(
            IOrganizationCargoTypeService service,
            ILogger<OrganizationCargoTypeController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _service = service;
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

        [HttpGet("{organizationId}")]
        public async Task<IActionResult> GetAll(long organizationId)
        {
            _logger.LogInformation("User {UserId} fetching cargo types for organization {OrgId}", CurrentUserId, organizationId);
            try
            {
                var list = await _service.GetAllAsync(organizationId, CurrentUserId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving cargo types", new { organizationId });
            }
        }

        [HttpPost("{organizationId}")]
        public async Task<IActionResult> Add(long organizationId, [FromBody] CreateOrganizationCargoTypeDto dto)
        {
            _logger.LogInformation("User {UserId} adding cargo type to organization {OrgId}", CurrentUserId, organizationId);
            try
            {
                var result = await _service.AddAsync(organizationId, dto, CurrentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error adding cargo type", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("User {UserId} deleting organization cargo type {Id}", CurrentUserId, id);
            try
            {
                var result = await _service.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "نوع بار سازمانی یافت نشد" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting cargo type", new { id });
            }
        }
    }
}
