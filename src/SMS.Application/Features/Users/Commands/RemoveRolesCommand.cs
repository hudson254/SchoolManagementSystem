using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class RemoveRolesCommand : IRequest<MediatR.Unit>
    {
        public Guid UserId { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }

    public class RemoveRolesCommandHandler : IRequestHandler<RemoveRolesCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<RemoveRolesCommandHandler> _logger;

        public RemoveRolesCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<RemoveRolesCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(RemoveRolesCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            foreach (var role in request.Roles)
            {
                await _userManager.RemoveRoleAsync(request.UserId.ToString(), role);
            }

            await _auditService.LogAsync("RemoveRoles", "User", $"Roles removed from user {user.Email}: {string.Join(", ", request.Roles)}");

            _logger.LogInformation("Roles removed from user {Email}: {Roles}", user.Email, string.Join(", ", request.Roles));

            return MediatR.Unit.Value;
        }
    }
}

