using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;

namespace SMS.Application.Features.Auth.Commands
{
    public class RegisterCommand : IRequest<AuthResponseDto>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .MaximumLength(20);

            RuleFor(x => x.Role)
                .Must(role => new[] { "Student", "Lecturer", "Receptionist" }.Contains(role))
                .When(x => !string.IsNullOrEmpty(x.Role))
                .WithMessage("Invalid role. Valid roles: Student, Lecturer, Receptionist");
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            IUserManagerService userManager,
            IJwtService jwtService,
            IEmailService emailService,
            IAuditService auditService,
            ILogger<RegisterCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException("User", "Email", request.Email);
            }

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                Organization = request.Organization,
                IsActive = true,
                IsEmailVerified = false
            };

            var createResult = await _userManager.CreateUserAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new ValidationException($"User creation failed: {errors}");
            }

            await _userManager.AddToRoleAsync(user, request.Role);

            await _auditService.LogAsync("User", "Register", user.Id, null, $"Email: {request.Email}, Role: {request.Role}");

            var verificationToken = await _userManager.GenerateEmailVerificationTokenAsync(user);
            await _emailService.SendVerificationEmailAsync(
                request.Email,
                request.FirstName,
                verificationToken,
                user.Id);

            _logger.LogInformation("User registered: {Email} with role {Role}", request.Email, request.Role);

            var roles = new List<string> { request.Role };
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
            await _userManager.UpdateUserAsync(user);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                Permissions = new List<string>(),
                TenantId = user.TenantId,
                RequiresEmailVerification = true,
                ExpiresIn = 3600
            };
        }
    }
}