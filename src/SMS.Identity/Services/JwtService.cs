using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Identity.Models;
using Microsoft.Extensions.Logging;

namespace SMS.Identity.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public string GenerateToken(string userId, string username, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                // Use the standard JWT "sub" claim so controllers can read
                // User.FindFirst("sub") (AuthController /me, logout, etc).
                // MapInboundClaims=false is configured in Program.cs, so the
                // claim keeps its short name and is not remapped.
                new Claim(JwtRegisteredClaimNames.Sub, userId),

                // Also emit the legacy NameIdentifier URI for backward
                // compatibility with any code that still reads it.
                new Claim(ClaimTypes.NameIdentifier, userId),

                new Claim(JwtRegisteredClaimNames.Name, username),

                // Standard "role" claim name so [Authorize(Roles="...")]
                // and User.IsInRole(...) work with RoleClaimType="role".
                new Claim("role", "Student"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateAccessToken(string userId, string username, IEnumerable<string> roles)
        {
            return GenerateToken(userId, username, roles);
        }

        public async Task<string> GenerateAccessTokenAsync(User user, IEnumerable<string> roles)
        {
            return await Task.Run(() => GenerateToken(user.Id, user.UserName ?? user.Email, roles));
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            return await Task.Run(() => GenerateRefreshToken());
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            return await Task.Run(() => ValidateToken(token));
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            // Validate that the refresh token is a valid base64 string of appropriate length
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            try
            {
                var data = Convert.FromBase64String(refreshToken);
                return data.Length == 64;
            }
            catch
            {
                return false;
            }
        }

        public Task RevokeRefreshTokenAsync(string refreshToken)
        {
            // In a production system, this would mark the refresh token as revoked in a store
            _logger.LogInformation("Refresh token revocation requested");
            return Task.CompletedTask;
        }
    }
}
