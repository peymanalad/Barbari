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

        public async Task SaveTokenAsync(UserToken token)
        {
            _context.Tokens.Add(token);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<UserToken?> FindRefreshTokenAsync(string refreshToken) 
        {
            var hashed = SecurityHelper.GetSha256Hash(refreshToken);
            return await _context.Tokens
                .Include(t => t.Person)
                .SingleOrDefaultAsync(t => t.RefreshTokenHash == hashed)
                .ConfigureAwait(false);
        }

        public async Task DeleteTokenAsync(string refreshToken)
        {
            var token = await FindRefreshTokenAsync(refreshToken).ConfigureAwait(false);
            if (token != null)
            {
                _context.Tokens.Remove(token);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<bool> CheckExistTokenAsync(string jwtToken)
        {
            var hashed = SecurityHelper.GetSha256Hash(jwtToken);
            return await _context.Tokens.AnyAsync(t => t.TokenHash == hashed).ConfigureAwait(false);
        }
    }
}