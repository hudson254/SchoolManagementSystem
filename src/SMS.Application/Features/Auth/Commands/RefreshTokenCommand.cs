using MediatR;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Auth.Commands
{
    public class RefreshTokenCommand : IRequest<AuthResponseDto>
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
    {
        private readonly IJwtService _jwtService;
        private readonly IUserManagerService _userManager;

        public RefreshTokenCommandHandler(IJwtService jwtService, IUserManagerService userManager)
        {
            _jwtService = jwtService;
            _userManager = userManager;
        }

        public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new UnauthorizedException("Invalid refresh request");
            }

            // Extract user ID from the expired access token first, so we can
            // validate the refresh token against the stored value on the user
            // record (RISK-02 fix). Previously validation only checked that the
            // refresh token was a 64-byte base64 string, allowing an attacker
            // to forge any validly-shaped token.
            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? principal.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException("Invalid access token");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("User not found or inactive");
            }

            // Validate the presented refresh token against the stored token +
            // expiry on the user record. This closes the forgery vulnerability.
            var isValid = await _userManager.ValidateRefreshTokenAsync(userId, request.RefreshToken);
            if (!isValid)
            {
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            var roles = await _userManager.GetUserRolesAsync(userId);
            var newAccessToken = await _jwtService.GenerateAccessTokenAsync(user, roles);

            // Rotate the refresh token: issue a new one and persist it to the
            // user record (invalidating the old one), limiting the replay
            // window for any stolen token.
            var newRefreshToken = await _userManager.GenerateRefreshTokenAsync(userId);

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 3600,
                UserId = userId,
                Email = user.Email,
                FullName = user.FullName ?? string.Empty,
                Roles = roles.ToList()
            };
        }
    }
}
