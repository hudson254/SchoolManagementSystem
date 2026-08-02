using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class ResetUserPasswordCommand : IRequest<MediatR.Unit>
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
    {
        public ResetUserPasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters");
        }
    }

    public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<ResetUserPasswordCommandHandler> _logger;

        public ResetUserPasswordCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<ResetUserPasswordCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            if (token == null)
                throw new ExternalServiceException("Failed to generate password reset token");

            var result = await _userManager.ResetPasswordAsync(user.Email!, token, request.NewPassword);
            if (!result)
                throw new ExternalServiceException("Failed to reset password");

            await _auditService.LogAsync("ResetPassword", "User", $"Password reset for user: {user.Email}");

            _logger.LogInformation("Password reset for user: {Email}", user.Email);

            return MediatR.Unit.Value;
        }
    }
}

