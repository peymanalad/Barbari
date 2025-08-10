namespace BarcopoloWebApi.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string RefreshToken { get; set; } = null!;
    }
}