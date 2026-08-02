using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;


namespace SMS.Application.Features.Auth.Commands
{
    public class RegisterCommand : IRequest<AuthResponseDto>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required");
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        /// <summary>
        /// The only role ever assigned during public self-registration.
        /// Client-supplied role values are deliberately ignored to prevent
        /// privilege escalation (previously an attacker could register
        /// with Role="Administrator").
        /// </summary>
        public const string DefaultSelfRegistrationRole = "Student";

        private readonly IUserManagerService _userManagerService;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            IUserManagerService userManagerService,
            IJwtService jwtService,
            IAuditService auditService,
            ILogger<RegisterCommandHandler> logger)
        {
            _userManagerService = userManagerService;
            _jwtService = jwtService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Validate passwords match
            if (request.Password != request.ConfirmPassword)
            {
                throw new SMS.Application.Exceptions.ValidationException("Passwords do not match");
            }

            // Check if user already exists
            var existingUser = await _userManagerService.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException("User with this email already exists");
            }

            // Create user with the fixed low-privilege default role.
            // The role is a server-side constant and never derived from request
            // input, closing the role-escalation vulnerability.
            var user = await _userManagerService.CreateUserAsync(
                request.Email,
                request.Email,
                request.Password,
                DefaultSelfRegistrationRole);

            if (user == null)
            {
                throw new ExternalServiceException("User creation service returned null");
            }

            var typedUser = (User)user;

            // Get user roles
            var roles = await _userManagerService.GetRolesAsync(typedUser);
            var rolesList = roles?.ToList() ?? new List<string>();

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(typedUser.Id.ToString(), typedUser.Email ?? typedUser.UserName, rolesList);
            var refreshToken = await _userManagerService.GenerateRefreshTokenAsync(typedUser.Id.ToString());

            // Log successful registration
            await _auditService.LogAsync("Register", typedUser.Id.ToString(), $"User registered successfully with role {DefaultSelfRegistrationRole}");

            _logger.LogInformation("User registered successfully: {Email} with role {Role}", request.Email, DefaultSelfRegistrationRole);

            // Return response
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,
                TokenType = "Bearer",
                UserId = typedUser.Id.ToString(),
                Email = typedUser.Email,
                Username = typedUser.UserName,
                Roles = rolesList
            };
        }
    }
}
