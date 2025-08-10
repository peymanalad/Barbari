using BarcopoloWebApi.DTOs;
using BarcopoloWebApi.DTOs.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly ILogger<MembershipController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public MembershipController(IMembershipService membershipService, ILogger<MembershipController> logger, IHttpContextAccessor contextAccessor)
        {
            _membershipService = membershipService;
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
        public async Task<IActionResult> Create([FromBody] CreateMembershipDto dto)
        {
            _logger.LogInformation("Adding membership to organization {OrganizationId} for person with NationalCode : {NationalCode}", dto.OrganizationId, dto.NationalCode);
            try
            {
                var membership = await _membershipService.AddAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = membership.Id }, membership);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating membership", dto);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var membership = await _membershipService.GetByIdAsync(id, CurrentUserId);
                return membership != null ? Ok(membership) : NotFound(new { error = "Membership not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving membership", new { id });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateMembershipDto dto)
        {
            _logger.LogInformation("Updating membership with Id {MembershipId}", id);
            try
            {
                var membership = await _membershipService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(membership);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating membership", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting membership with Id {MembershipId}", id);
            try
            {
                var result = await _membershipService.RemoveAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "Membership not found" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting membership", new { id });
            }
        }

        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetByOrganizationId(long organizationId)
        {
            _logger.LogInformation("Retrieving memberships for organization {OrganizationId}", organizationId);
            try
            {
                var memberships = await _membershipService.GetByOrganizationIdAsync(organizationId, CurrentUserId);
                return Ok(memberships);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving organization memberships", new { organizationId });
            }
        }
    }
}
