using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using BarcopoloWebApi.Services.Person;
using BarcopoloWebApi.Services.Token;
using Microsoft.Extensions.Logging;

namespace BarcopoloWebApi.Security
{
    public class TokenValidate : ITokenValidator
    {
        private readonly IPersonService _personService;
        private readonly UserTokenRepository _userTokenRepository;
        private readonly ILogger<TokenValidate> _logger;

        public TokenValidate(IPersonService personService, UserTokenRepository userTokenRepository, ILogger<TokenValidate> logger)
        {
            _personService = personService;
            _userTokenRepository = userTokenRepository;
            _logger = logger;
        }

        public async Task ExecuteAsync(TokenValidatedContext context)
        {
            var identity = context.Principal?.Identity as ClaimsIdentity;

            if (identity?.Claims == null || !identity.Claims.Any())
            {
                context.Fail("Claims not found.");
                _logger.LogWarning("Token validation failed: no claims found.");
                return;
            }

            var userIdClaim = identity.FindFirst("UserId");
            if (userIdClaim == null)
            {
                context.Fail("UserId claim not found.");
                _logger.LogWarning("Token validation failed: UserId claim missing.");
                return;
            }

            if (!long.TryParse(userIdClaim.Value, out var userId))
            {
                context.Fail("Invalid UserId format.");
                _logger.LogWarning("Token validation failed: invalid UserId format.");
                return;
            }

            var user = await _personService.GetEntityByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                context.Fail("User not found or inactive.");
                _logger.LogWarning("Token validation failed: user not found or inactive (UserId: {UserId})", userId);
                return;
            }

            var token = context.HttpContext.Request.Headers["Authorization"]
                .ToString()
                .Replace("Bearer ", string.Empty);

            if (!await _userTokenRepository.CheckExistTokenAsync(token))
            {
                context.Fail("Token is not registered.");
                _logger.LogWarning("Token validation failed: token not found in database for UserId {UserId}", userId);
                return;
            }

            _logger.LogInformation("Token validated successfully for user {UserId}", userId);
        }
    }
}
