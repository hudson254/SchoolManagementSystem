using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class LoginCommand : IRequest<AuthResponseDto>
    {
        /// <summary>
        /// The email or username used for authentication.
        /// Maps from both "identifier" and "email" JSON properties for backward compatibility.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("identifier")]
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }

        /// <summary>
        /// Backward-compatible field that maps the legacy "email" JSON property.
        /// The handler uses Identifier primarily, falling back to Email if Identifier is empty.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            // Accept either Identifier (preferred) or Email (backward-compatible)
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Identifier) || !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("Email or username is required")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Identifier)
                        .MaximumLength(256).WithMessage("Email or username must not exceed 256 characters");
                    RuleFor(x => x.Email)
                        .MaximumLength(256).WithMessage("Email must not exceed 256 characters");
                });

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IUserManagerService _userManagerService;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;
        private readonly ILoginHistoryRepository _loginHistoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginCommandHandler> _logger;

        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LoginCommandHandler(
            IUserManagerService userManagerService,
            IJwtService jwtService,
            IAuditService auditService,
            ILoginHistoryRepository loginHistoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<LoginCommandHandler> logger)
        {
            _userManagerService = userManagerService;
            _jwtService = jwtService;
            _auditService = auditService;
            _loginHistoryRepository = loginHistoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            User? typedUser = null;
            string? failureReason = null;
            // Use Identifier primarily, fall back to Email for backward compatibility
            var identifier = (request.Identifier ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(identifier))
                identifier = (request.Email ?? string.Empty).Trim();

            try
            {
                // Determine if the identifier is an email or username
                bool isEmail = EmailRegex.IsMatch(identifier);

                // Find user by email or username
                User? user = null;
                if (isEmail)
                {
                    user = await _userManagerService.FindByEmailAsync(identifier);
                }
                else
                {
                    user = await _userManagerService.FindByUsernameAsync(identifier);
                }

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User with identifier not found");
                    failureReason = "User not found";
                    throw new UnauthorizedException("Invalid username/email or password");
                }

                typedUser = (User)user;

                // Check password
                var isPasswordValid = await _userManagerService.CheckPasswordAsync(typedUser, request.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login attempt failed: Invalid password for user {Identifier}", identifier);
                    failureReason = "Invalid password";
                    throw new UnauthorizedException("Invalid username/email or password");
                }

                // Check if user is active
                if (!typedUser.IsActive)
                {
                    failureReason = "Account locked";
                    throw new UnauthorizedException("Account is locked. Please contact support.");
                }

                // Get user roles
                var roles = await _userManagerService.GetRolesAsync(typedUser);
                var rolesList = roles?.ToList() ?? new List<string>();

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(typedUser.Id.ToString(), typedUser.Email ?? typedUser.UserName, rolesList);
                var refreshToken = await _userManagerService.GenerateRefreshTokenAsync(typedUser.Id.ToString());

                // RISK-27: Persist a successful login record for audit/security
                // reporting (GetUserActivityReport, LoginHistoryRepository).
                await _loginHistoryRepository.AddAsync(new LoginHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = typedUser.Id.ToString(),
                    LoginTime = DateTime.UtcNow,
                    IsSuccessful = true,
                    FailureReason = null
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log successful login
                await _auditService.LogAsync("Login", typedUser.Id.ToString(), $"User logged in successfully");

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
            catch (UnauthorizedException)
            {
                // RISK-27: Persist a failed login record (with reason when the
                // user exists) so administrators can detect brute-force
                // attempts and audit lockouts via GetFailedLoginsAsync.
                // Only log when the user exists (typedUser != null) - for
                // non-existent users, there's no user to log against.
                if (typedUser != null && failureReason != null)
                {
                    await _loginHistoryRepository.AddAsync(new LoginHistory
                    {
                        Id = Guid.NewGuid(),
                        UserId = typedUser?.Id.ToString() ?? "unknown",
                        LoginTime = DateTime.UtcNow,
                        IsSuccessful = false,
                        FailureReason = failureReason
                    }, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for identifier {Identifier}", identifier);
                throw;
            }
        }
    }
}
