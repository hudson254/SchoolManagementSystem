using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Auth.Commands
{
    public class ChangePasswordCommand : IRequest<MediatR.Unit>
    {
        public System.Guid UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(8).WithMessage("New password must be at least 8 characters")
                .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManagerService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(
            IUserManagerService userManagerService,
            IAuditService auditService,
            ILogger<ChangePasswordCommandHandler> logger)
        {
            _userManagerService = userManagerService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = request.UserId.ToString();

            // Verify user exists
            var user = await _userManagerService.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", request.UserId);
            }

            // Verify current password is correct
            var isCurrentPasswordValid = await _userManagerService.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Change password failed: Current password is incorrect for user {UserId}", userId);
                throw new SMS.Application.Exceptions.ValidationException("Current password is incorrect");
            }

            // Change password via UserManager
            var result = await _userManagerService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            if (!result)
            {
                _logger.LogError("Change password failed for user {UserId}", userId);
                throw new SMS.Application.Exceptions.ValidationException("Failed to change password. Please try again.");
            }

            // Audit log
            await _auditService.LogAsync("ChangePassword", "User", userId);

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            return MediatR.Unit.Value;
        }
    }
}
