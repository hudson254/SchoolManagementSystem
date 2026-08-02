using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class AssignRolesCommand : IRequest<MediatR.Unit>
    {
        public Guid UserId { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }

    public class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssignRolesCommandHandler> _logger;

        public AssignRolesCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<AssignRolesCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(AssignRolesCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            foreach (var role in request.Roles)
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            await _auditService.LogAsync("AssignRoles", "User", $"Roles assigned to user {user.Email}: {string.Join(", ", request.Roles)}");

            _logger.LogInformation("Roles assigned to user {Email}: {Roles}", user.Email, string.Join(", ", request.Roles));

            return MediatR.Unit.Value;
        }
    }
}

