using MediatR;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SMS.Application.Features.Auth.Commands
{
    public class ForgotPasswordCommand : IRequest<bool>
    {
        public string Email { get; set; }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
    {
        private readonly IUserManagerService _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        public ForgotPasswordCommandHandler(
            IUserManagerService userManager,
            IEmailService emailService,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                // Always return true to avoid user enumeration.
                return true;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"https://localhost:5001/reset-password?token={token}&email={request.Email}";

            try
            {
                await _emailService.SendPasswordResetEmailAsync(request.Email, resetLink);
            }
            catch (Exception ex)
            {
                // Email delivery failures must NOT break the forgot-password flow.
                // The endpoint always returns 204 regardless of SMTP availability
                // (e.g., in test/development environments without an SMTP server).
                _logger.LogWarning(ex, "Failed to send password reset email to {Email}", request.Email);
            }

            return true;
        }
    }
}

