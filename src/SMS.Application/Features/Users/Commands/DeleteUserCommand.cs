using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class DeleteUserCommand : IRequest<MediatR.Unit>
    {
        public Guid UserId { get; set; }
    }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<DeleteUserCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            await _userManager.DeleteUserAsync(request.UserId.ToString());

            await _auditService.LogAsync("Delete", "User", $"User deleted: {user.Email}");

            _logger.LogInformation("User deleted: {Email} ({Id})", user.Email, request.UserId);

            return MediatR.Unit.Value;
        }
    }
}

