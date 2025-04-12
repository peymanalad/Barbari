using BarcopoloWebApi.DTOs.Auth;
using BarcopoloWebApi.DTOs.Token;

namespace BarcopoloWebApi.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResultDto> RegisterAsync(RegisterDto dto);
        Task<LoginResultDto> LoginAsync(LoginDto dto);
        Task LogoutAsync();
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<bool> ChangePasswordAsync(ChangePasswordDto dto);

        Task<LoginDataDto> CreateToken(long personId);
    }
}