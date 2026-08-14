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
        private readonly IAuditService _auditService;

        public RefreshTokenCommandHandler(IJwtService jwtService, IUserManagerService userManager)
            : this(jwtService, userManager, null)
        {
        }

        public RefreshTokenCommandHandler(IJwtService jwtService, IUserManagerService userManager,
            IAuditService auditService = null)
        {
            _jwtService = jwtService;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new UnauthorizedException("Invalid refresh request");
            }

            // Extract user ID from the expired access token
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

            // Step 1: Validate the refresh token against the stored token
            var isValid = await _userManager.ValidateRefreshTokenAsync(userId, request.RefreshToken);
            if (!isValid)
            {
                // Step 2: Reuse detection - token might have been rotated
                // Check if this token was previously valid (reuse attempt)
                var wasPreviouslyValid = await _userManager.IsRefreshTokenReusedAsync(userId, request.RefreshToken);
                if (wasPreviouslyValid)
                {
                    // Token theft detected! Revoke the entire token family
                    await _userManager.RevokeRefreshTokenFamilyAsync(userId);

                    // Log security event
                    if (_auditService != null)
                    {
                        await _auditService.LogAsync("RefreshTokenReuseDetected", userId,
                            "Previously rotated refresh token was presented again - token family revoked");
                    }
                }

                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            var roles = await _userManager.GetUserRolesAsync(userId);
            var newAccessToken = await _jwtService.GenerateAccessTokenAsync(user, roles);

            // Rotate the refresh token: issue a new one and persist it,
            // tracking the old token hash for reuse detection.
            // Uses RotateRefreshTokenAsync which maintains the token family
            // relationship and stores the old token hash for reuse detection.
            var newRefreshToken = await _userManager.RotateRefreshTokenAsync(userId, request.RefreshToken);

            // Log successful refresh
            if (_auditService != null)
            {
                await _auditService.LogAsync("RefreshTokenIssued", userId, "Refresh token rotated successfully");
            }

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
