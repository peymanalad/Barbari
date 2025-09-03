using BarcopoloWebApi.DTOs.Auth;
using BarcopoloWebApi.Extensions;
using BarcopoloWebApi.Services.Auth;
using BarcopoloWebApi.Services.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BarcopoloWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("AuthPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly UserTokenRepository _tokenRepo;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, UserTokenRepository tokenRepo)
        {
            _authService = authService;
            _logger = logger;
            _tokenRepo = tokenRepo;
        }

        private IActionResult HandleError(Exception ex, string message)
        {
            _logger.LogError(ex, message);
            return BadRequest(new { error = "An error occurred" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            _logger.LogInformation("Registration attempt for {PhoneNumber}", dto.PhoneNumber.MaskSensitive());
            try
            {
                var person = await _authService.RegisterAsync(dto);
                return Ok(new { message = "ثبت‌نام با موفقیت انجام شد", personId = person.PersonId });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Registration failed");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authService.LoginAsync(dto);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Login failed");
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            _logger.LogInformation("Refresh token attempt");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userToken = _tokenRepo.FindRefreshTokenAsync(dto.RefreshToken);
            if (userToken == null || userToken.Result.TokenExp < DateTime.UtcNow)
                return Unauthorized(new { error = "توکن نامعتبر یا منقضی شده است" });

            _tokenRepo.DeleteTokenAsync(dto.RefreshToken);
            var token = await _authService.CreateToken(userToken.Result.PersonId);

            return Ok(token);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return Ok(new { message = "با موفقیت خارج شدید" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            return result
                ? Ok(new { message = "رمز عبور با موفقیت تغییر کرد" })
                : BadRequest(new { error = "شماره موبایل یافت نشد" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var result = await _authService.ChangePasswordAsync(dto);
            return result
                ? Ok(new { message = "رمز عبور با موفقیت تغییر کرد" })
                : BadRequest(new { error = "رمز عبور قبلی اشتباه است یا کاربر یافت نشد" });
        }
    }
}
