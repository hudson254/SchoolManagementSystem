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
            // Validate refresh token
            var isValid = await _jwtService.ValidateRefreshTokenAsync(request.RefreshToken);
            if (!isValid)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            // Extract user ID from the expired access token
            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException("Invalid access token");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("User not found or inactive");
            }

            var roles = await _userManager.GetUserRolesAsync(userId);
            var newAccessToken = await _jwtService.GenerateAccessTokenAsync(user, roles);
            var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync(userId);

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

