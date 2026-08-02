using MediatR;

namespace SMS.Application.Features.Auth.Commands
{
    public class LogoutCommand : IRequest<MediatR.Unit>
    {
        public System.Guid UserId { get; set; }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, MediatR.Unit>
    {
        public System.Threading.Tasks.Task<MediatR.Unit> Handle(LogoutCommand request, System.Threading.CancellationToken cancellationToken)
        {
            // Logout logic - typically invalidate refresh token
            return System.Threading.Tasks.Task.FromResult(MediatR.Unit.Value);
        }
    }
}
