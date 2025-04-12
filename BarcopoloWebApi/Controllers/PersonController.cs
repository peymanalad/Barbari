using BarcopoloWebApi.DTOs.Person;
using BarcopoloWebApi.Services.Person;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IPersonService _personService;
        private readonly ILogger<PersonController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public PersonController(IPersonService personService, IHttpContextAccessor contextAccessor, ILogger<PersonController> logger)
        {
            _personService = personService;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        private long CurrentUserId =>
            long.Parse(_contextAccessor.HttpContext?.User.Claims.First(c => c.Type == "UserId").Value ?? "0");

        private IActionResult HandleError(Exception ex, string message, object? data = null)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = ex.Message, data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("User {UserId} requested person list", CurrentUserId);
            try
            {
                var persons = await _personService.GetAllAsync(CurrentUserId);
                return Ok(persons);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving person list");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("User {UserId} requested person {PersonId}", CurrentUserId, id);
            try
            {
                var person = await _personService.GetByIdAsync(id, CurrentUserId);
                return person != null ? Ok(person) : NotFound(new { error = "شخص مورد نظر یافت نشد" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error retrieving person", new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePersonDto dto)
        {
            _logger.LogInformation("Creating new person by user {UserId}", CurrentUserId);
            try
            {
                var person = await _personService.CreateAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error creating person", dto);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePersonDto dto)
        {
            _logger.LogInformation("Updating person {PersonId} by user {UserId}", id, CurrentUserId);
            try
            {
                var person = await _personService.UpdateAsync(id, dto, CurrentUserId);
                return Ok(person);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error updating person", dto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting person {PersonId} by user {UserId}", id, CurrentUserId);
            try
            {
                var result = await _personService.DeleteAsync(id, CurrentUserId);
                return result ? NoContent() : NotFound(new { error = "شخص مورد نظر برای حذف یافت نشد" });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error deleting person", new { id });
            }
        }
    }
}
