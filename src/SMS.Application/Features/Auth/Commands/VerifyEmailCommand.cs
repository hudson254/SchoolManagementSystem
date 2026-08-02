using MediatR;
using FluentValidation;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;

namespace SMS.Application.Features.Auth.Commands
{
    public class VerifyEmailCommand : IRequest
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Verification token is required");
        }
    }

    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<VerifyEmailCommandHandler> _logger;

        public VerifyEmailCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<VerifyEmailCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", request.UserId);
            }

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"Email verification failed: {errors}");
            }

            user.IsEmailVerified = true;
            await _userManager.UpdateUserAsync(user);

            await _auditService.LogAsync("User", "VerifyEmail", user.Id, null, "Email verified");
            _logger.LogInformation("Email verified for user: {Email}", user.Email);
        }
    }
}