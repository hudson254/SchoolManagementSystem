using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Auth.Commands
{
    /// <summary>
    /// Handles the "forgot password" flow by creating a <see cref="PasswordResetRequest"/>
    /// that an administrator can later fulfill. SMTP/email has been fully removed from
    /// the system; password resets are now admin-mediated on the isolated LAN.
    /// </summary>
    public class ForgotPasswordCommand : IRequest<bool>
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Optional note from the requester (e.g., "locked out", "forgot password").
        /// </summary>
        public string? Note { get; set; }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
    {
        private readonly IUserManagerService _userManager;
        private readonly IPasswordResetRequestRepository _resetRequestRepository;
        private readonly IAuditService _auditService;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        public ForgotPasswordCommandHandler(
            IUserManagerService userManager,
            IPasswordResetRequestRepository resetRequestRepository,
            IAuditService auditService,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _userManager = userManager;
            _resetRequestRepository = resetRequestRepository;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            // Find user by email (if present). We intentionally do NOT reveal
            // whether the email exists, to prevent account enumeration.
            string? userId = null;
            var user = await _userManager.GetUserByEmailAsync(request.Email);
            if (user != null)
            {
                userId = user.Id;
            }

            // Create a password reset request for admin fulfillment.
            var resetRequest = new PasswordResetRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId, // null if user not found — prevents enumeration
                RequestedEmail = request.Email,
                Note = request.Note,
                Status = PasswordResetRequestStatus.Pending
            };

            await _resetRequestRepository.AddAsync(resetRequest);

            await _auditService.LogAsync("PasswordResetRequested", userId ?? "unknown", $"Password reset requested for {request.Email}");

            _logger.LogInformation("Password reset request created for {Email} (request id: {RequestId})", request.Email, resetRequest.Id);

            return true;
        }
    }
}
