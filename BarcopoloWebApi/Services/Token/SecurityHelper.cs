using System.Security.Cryptography;
using System.Text;

namespace BarcopoloWebApi.Services.Token
{
    public class SecurityHelper
    {
        private readonly RandomNumberGenerator random = RandomNumberGenerator.Create();
        public static string GetSha256Hash(string value)
        {
            var byteValue = Encoding.UTF8.GetBytes(value);
            var byteHash = SHA256.HashData(byteValue);
            return Convert.ToBase64String(byteHash);
        }
    }
}
