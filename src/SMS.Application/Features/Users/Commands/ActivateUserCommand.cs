using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class ActivateUserCommand : IRequest<MediatR.Unit>
    {
        public Guid UserId { get; set; }
    }

    public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<ActivateUserCommandHandler> _logger;

        public ActivateUserCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<ActivateUserCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            user.IsActive = true;
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            await _userManager.UpdateUserAsync(user);

            await _auditService.LogAsync("Activate", "User", $"User activated: {user.Email}");

            _logger.LogInformation("User activated: {Email}", user.Email);

            return MediatR.Unit.Value;
        }
    }
}

