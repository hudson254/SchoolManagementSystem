using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class DeactivateUserCommand : IRequest<MediatR.Unit>
    {
        public Guid UserId { get; set; }
    }

    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeactivateUserCommandHandler> _logger;

        public DeactivateUserCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<DeactivateUserCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            await _userManager.UpdateUserAsync(user);

            await _auditService.LogAsync("Deactivate", "User", $"User deactivated: {user.Email}");

            _logger.LogInformation("User deactivated: {Email}", user.Email);

            return MediatR.Unit.Value;
        }
    }
}

