using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.PasswordReset.Commands
{
    public class FulfillPasswordResetCommand : IRequest<MediatR.Unit>
    {
        public Guid RequestId { get; set; }
        public string AdminUserId { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
    }

    public class FulfillPasswordResetCommandHandler : IRequestHandler<FulfillPasswordResetCommand, MediatR.Unit>
    {
        private readonly IPasswordResetRequestRepository _repository;
        private readonly IUserManagerService _userManager;
        private readonly ILogger<FulfillPasswordResetCommandHandler> _logger;

        public FulfillPasswordResetCommandHandler(
            IPasswordResetRequestRepository repository,
            IUserManagerService userManager,
            ILogger<FulfillPasswordResetCommandHandler> logger)
        {
            _repository = repository;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(FulfillPasswordResetCommand request, CancellationToken cancellationToken)
        {
            var resetRequest = await _repository.GetByIdAsync(request.RequestId);
            if (resetRequest == null)
                throw new InvalidOperationException("Password reset request not found.");

            if (resetRequest.Status != PasswordResetRequestStatus.Pending)
                throw new InvalidOperationException($"Request is already {resetRequest.Status}.");

            if (string.IsNullOrEmpty(resetRequest.UserId))
                throw new InvalidOperationException("Cannot fulfill request: user not found.");

            // Load the user entity required by GeneratePasswordResetTokenAsync
            var user = await _userManager.FindByIdAsync(resetRequest.UserId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            // Generate a secure random temporary password
            var tempPassword = GenerateSecurePassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetSucceeded = await _userManager.ResetPasswordAsync(user.Email, resetToken, tempPassword);

            if (!resetSucceeded)
                throw new InvalidOperationException("Password reset failed.");

            // Revoke all refresh tokens for this user to prevent previously issued
            // refresh tokens from establishing new sessions after password reset.
            // This ensures that if an attacker had access to a refresh token before
            // the password was changed, it cannot be used to maintain access.
            await _userManager.RevokeAllRefreshTokensAsync(resetRequest.UserId);

            // Update request status
            resetRequest.Status = PasswordResetRequestStatus.Fulfilled;
            resetRequest.FulfilledByUserId = request.AdminUserId;
            resetRequest.FulfilledAt = DateTime.UtcNow;
            resetRequest.ResolutionNote = request.ResolutionNote;

            await _repository.UpdateAsync(resetRequest);

            _logger.LogInformation("Password reset request {RequestId} fulfilled by admin {AdminUserId} for user {UserId}. All refresh tokens revoked.",
                request.RequestId, request.AdminUserId, resetRequest.UserId);

            return MediatR.Unit.Value;
        }

        private string GenerateSecurePassword()
        {
            // Generate a 12-character secure random password
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var password = new char[12];
            for (int i = 0; i < password.Length; i++)
            {
                password[i] = chars[Random.Shared.Next(chars.Length)];
            }
            return new string(password);
        }
    }
}
