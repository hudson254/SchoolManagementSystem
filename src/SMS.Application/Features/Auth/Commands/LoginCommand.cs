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
                .EmailAddress().WithMessage("A valid email is required");

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

            try
            {
                // Find user by email
                var user = await _userManagerService.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Login attempt failed: User with email {request.Email} not found");
                    failureReason = "User not found";
                    throw new UnauthorizedException("Invalid email or password");
                }

                typedUser = (User)user;

                // Check password
                var isPasswordValid = await _userManagerService.CheckPasswordAsync(typedUser, request.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning($"Login attempt failed: Invalid password for user {request.Email}");
                    failureReason = "Invalid password";
                    throw new UnauthorizedException("Invalid email or password");
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
                if (typedUser != null || failureReason != null)
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
                _logger.LogError(ex, $"Error during login for user {request.Email}");
                throw;
            }
        }
    }
}
