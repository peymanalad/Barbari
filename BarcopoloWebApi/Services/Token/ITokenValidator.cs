using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BarcopoloWebApi.Services.Token
{
    public interface ITokenValidator
    {
        Task ExecuteAsync(TokenValidatedContext context);
    }
}