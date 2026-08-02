using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;

namespace SMS.Application.Features.Auth.Commands
{
    public class RefreshTokenCommand : IRequest<AuthResponseDto>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required");
        }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IUserManagerService userManager,
            IJwtService jwtService,
            IAuditService auditService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByRefreshTokenAsync(request.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token invalid or expired");
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _userManager.GetPermissionsAsync(user);

            var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.UpdateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
            await _userManager.UpdateUserAsync(user);

            await _auditService.LogAsync("User", "RefreshToken", user.Id, null, "Token refreshed");

            _logger.LogInformation("Refresh token renewed for user: {Email}", user.Email);

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
                TenantId = user.TenantId,
                RequiresEmailVerification = !user.IsEmailVerified,
                ExpiresIn = 3600
            };
        }
    }
}