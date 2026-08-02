using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;

namespace SMS.Application.Features.Auth.Commands
{
    public class LoginCommand : IRequest<AuthResponseDto>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserManagerService userManager,
            IJwtService jwtService,
            IAuditService auditService,
            ILogger<LoginCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: User not found - {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Login attempt failed: Invalid password - {Email}", request.Email);
                await _auditService.LogAsync("User", "LoginFailed", user.Id, null, $"Email: {request.Email}");
                throw new UnauthorizedException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt failed: Account inactive - {Email}", request.Email);
                throw new UnauthorizedException("Your account has been deactivated. Please contact support.");
            }

            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login attempt failed: Email not verified - {Email}", request.Email);
                throw new UnauthorizedException("Please verify your email address before logging in.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _userManager.GetPermissionsAsync(user);

            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
            await _userManager.UpdateUserAsync(user);

            await _auditService.LogAsync("User", "Login", user.Id, null, $"Email: {request.Email}");

            _logger.LogInformation("User logged in: {Email}", request.Email);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
                TenantId = user.TenantId,
                RequiresEmailVerification = false,
                ExpiresIn = 3600
            };
        }
    }
}