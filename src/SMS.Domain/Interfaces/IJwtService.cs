using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IJwtService
    {
        // Existing methods...
        string GenerateToken(string userId, string username, IEnumerable<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
        
        // Add this method
        string GenerateAccessToken(string userId, string username, IEnumerable<string> roles);
        
        // Async methods
        Task<string> GenerateAccessTokenAsync(User user, IEnumerable<string> roles);
        Task<string> GenerateRefreshTokenAsync(string userId);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}