public class LoginResultDto
{
    public long PersonId { get; set; }
    public string FullName { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpireAt { get; set; }
}