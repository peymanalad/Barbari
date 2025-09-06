using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Auth;
using BarcopoloWebApi.DTOs.Token;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Extensions;
using BarcopoloWebApi.Helper;
using BarcopoloWebApi.Services.Token;
using BarcopoloWebApi.Services.WalletManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BarcopoloWebApi.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly DataBaseContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserTokenRepository _tokenRepo;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<Entities.Person> _passwordHasher;
        private readonly IWalletService _walletService;

        public AuthService(
            DataBaseContext context,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor,
            UserTokenRepository tokenRepo,
            IConfiguration config,
            IPasswordHasher<Entities.Person> passwordHasher,
            IWalletService walletService)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _tokenRepo = tokenRepo;
            _config = config;
            _passwordHasher = passwordHasher;
            _walletService = walletService;
        }

        public async Task<LoginResultDto> RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation("Registering user {Phone}", dto.PhoneNumber.MaskSensitive());

            if (await _context.Persons.AnyAsync(p => p.PhoneNumber == dto.PhoneNumber))
                throw new Exception("شماره موبایل قبلاً ثبت شده است.");

            var person = new Entities.Person
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                NationalCode = dto.NationalCode,
                CreatedAt = TehranDateTime.Now,
                IsActive = true
            };

            person.PasswordHash = _passwordHasher.HashPassword(person, dto.Password);

            _context.Persons.Add(person);
            await _context.SaveChangesAsync();

            await _walletService.CreateWalletForPersonAsync(person.Id);

            _logger.LogInformation("User registered with ID {Id}", person.Id);

            var token = await CreateToken(person);
            return new LoginResultDto
            {
                PersonId = person.Id,
                FullName = person.GetFullName(),
                Token = token.Token,
                RefreshToken = token.RefreshToken,
                ExpireAt = TehranDateTime.Now.AddMinutes(int.Parse(_config["JwtConfig:expires"]))
            };
        }

        public async Task<LoginResultDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Login attempt for {Phone}", dto.PhoneNumber.MaskSensitive());

            var person = await _context.Persons.FirstOrDefaultAsync(p => p.PhoneNumber == dto.PhoneNumber);
            if (person == null)
                throw new Exception("نام کاربری یا رمز عبور اشتباه است.");

            var result = _passwordHasher.VerifyHashedPassword(person, person.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new Exception("نام کاربری یا رمز عبور اشتباه است.");

            if (!person.IsActive)
                throw new Exception("حساب کاربری غیرفعال است.");

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                person.PasswordHash = _passwordHasher.HashPassword(person, dto.Password);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Password rehashed for user {Id}", person.Id);
            }

            var token = await CreateToken(person);

            return new LoginResultDto
            {
                PersonId = person.Id,
                FullName = person.GetFullName(),
                Token = token.Token,
                RefreshToken = token.RefreshToken,
                ExpireAt = TehranDateTime.Now.AddMinutes(int.Parse(_config["JwtConfig:expires"]))
            };
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var person = await _context.Persons.FirstOrDefaultAsync(p => p.PhoneNumber == dto.PhoneNumber);
            if (person == null)
                return false;

            person.PasswordHash = _passwordHasher.HashPassword(person, dto.NewPassword);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset for user {Id}", person.Id);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var person = await _context.Persons.FirstOrDefaultAsync(p => p.PhoneNumber == dto.PhoneNumber);
            if (person == null)
                return false;

            var result = _passwordHasher.VerifyHashedPassword(person, person.PasswordHash, dto.OldPassword);
            if (result == PasswordVerificationResult.Failed)
                return false;

            person.PasswordHash = _passwordHasher.HashPassword(person, dto.NewPassword);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password changed for user {Id}", person.Id);
            return true;
        }

        public async Task<LoginDataDto> CreateToken(long personId)
        {
            var person = await _context.Persons.FindAsync(personId);
            if (person == null || !person.IsActive)
                throw new Exception("کاربر معتبر نیست.");

            return await CreateToken(person);
        }

        private async Task<LoginDataDto> CreateToken(Entities.Person person)
        {
            var claims = new List<Claim>
            {
                new Claim("UserId", person.Id.ToString()),
                new Claim("Name", person.GetFullName()),
                new Claim(ClaimTypes.Role, person.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtConfig:key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expire = TehranDateTime.Now.AddMinutes(int.Parse(_config["JwtConfig:expires"]));

            var jwt = new JwtSecurityToken(
                issuer: _config["JwtConfig:issuer"],
                audience: _config["JwtConfig:audience"],
                claims: claims,
                notBefore: TehranDateTime.Now,
                expires: expire,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
            var refreshToken = Guid.NewGuid().ToString();

            await _tokenRepo.SaveTokenAsync(new UserToken
            {
                PersonId = person.Id,
                TokenHash = SecurityHelper.GetSha256Hash(tokenString),
                TokenExp = expire,
                RefreshTokenHash = SecurityHelper.GetSha256Hash(refreshToken),
                RefreshTokenExp = TehranDateTime.Now.AddDays(30),
                MobileModel = ""
            });

            return new LoginDataDto
            {
                Token = tokenString,
                RefreshToken = refreshToken
            };
        }

        public async Task LogoutAsync()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
            if (!long.TryParse(userIdStr, out var userId))
                throw new Exception("کاربر یافت نشد.");

            var tokens = _context.Tokens.Where(t => t.PersonId == userId).ToList();
            _context.Tokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged out and tokens removed", userId);
        }
    }
}
