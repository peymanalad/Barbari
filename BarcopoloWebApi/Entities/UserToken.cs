using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Entities
{
    public class UserToken
    {
        public long Id { get; set; }

        [Required]
        public long PersonId { get; set; }

        [Required, MaxLength(512)]
        public string TokenHash { get; set; }

        public DateTime TokenExp { get; set; }

        [Required, MaxLength(512)]
        public string RefreshTokenHash { get; set; }

        public DateTime RefreshTokenExp { get; set; }

        [MaxLength(100)]
        public string MobileModel { get; set; }

        [JsonIgnore]
        public virtual Person Person { get; set; }


        public bool IsTokenExpired() => DateTime.UtcNow >= TokenExp;

        public bool IsRefreshTokenExpired() => DateTime.UtcNow >= RefreshTokenExp;

        public bool IsActive() => !IsTokenExpired() && !IsRefreshTokenExpired();
    }
}