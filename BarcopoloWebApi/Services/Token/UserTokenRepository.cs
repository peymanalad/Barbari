using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BarcopoloWebApi.Services.Token
{
    public class UserTokenRepository
    {
        private readonly DataBaseContext _context;

        public UserTokenRepository(DataBaseContext context)
        {
            _context = context;
        }

        public void SaveToken(UserToken token)
        {
            _context.Tokens.Add(token);
            _context.SaveChanges();
        }

        public UserToken? FindRefreshToken(string refreshToken)
        {
            var hashed = SecurityHelper.GetSha256Hash(refreshToken);
            return _context.Tokens
                .Include(t => t.Person)
                .SingleOrDefault(t => t.RefreshTokenHash == hashed);
        }

        public void DeleteToken(string refreshToken)
        {
            var token = FindRefreshToken(refreshToken);
            if (token != null)
            {
                _context.Tokens.Remove(token);
                _context.SaveChanges();
            }
        }

        public bool CheckExistToken(string jwtToken)
        {
            var hashed = SecurityHelper.GetSha256Hash(jwtToken);
            return _context.Tokens.Any(t => t.TokenHash == hashed);
        }
    }
}