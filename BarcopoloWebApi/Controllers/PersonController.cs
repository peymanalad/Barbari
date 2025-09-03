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
            var persons = await _personService.GetAllAsync(CurrentUserId);
            return Ok(persons);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("User {UserId} requested person {PersonId}", CurrentUserId, id);
            var person = await _personService.GetByIdAsync(id, CurrentUserId);
            return person != null ? Ok(person) : NotFound(new { error = "شخص مورد نظر یافت نشد" });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePersonDto dto)
        {
            _logger.LogInformation("Creating new person by user {UserId}", CurrentUserId);
            var person = await _personService.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePersonDto dto)
        {
            _logger.LogInformation("Updating person {PersonId} by user {UserId}", id, CurrentUserId);
            var person = await _personService.UpdateAsync(id, dto, CurrentUserId);
            return Ok(person);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Deleting person {PersonId} by user {UserId}", id, CurrentUserId);
            var result = await _personService.DeleteAsync(id, CurrentUserId);
            return result ? NoContent() : NotFound(new { error = "شخص مورد نظر برای حذف یافت نشد" });
        }
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(long id)
        {
            _logger.LogInformation("Activating person {PersonId} by user {UserId}", id, CurrentUserId);
            var result = await _personService.ActivateAsync(id, CurrentUserId);
            return result ? Ok(new { success = true }) : NotFound(new { error = "شخص مورد نظر برای فعال‌سازی یافت نشد" });
        }

        [HttpPost("check-existence")]
        public async Task<IActionResult> CheckExistenceByNationalCodeAsync([FromBody] PersonExistenceRequestDto dto)
        {
            var result = await _personService.CheckExistenceByNationalCodeAsync(dto, CurrentUserId);
            return Ok(result);
        }

        


    }
}
