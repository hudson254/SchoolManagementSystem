using MediatR;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Auth.Commands
{
    public class VerifyEmailCommand : IRequest<bool>
    {
        public string UserId { get; set; }
        public string Token { get; set; }
    }

    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, bool>
    {
        private readonly IUserManagerService _userManager;

        public VerifyEmailCommandHandler(IUserManagerService userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            var result = await _userManager.VerifyEmailAsync(request.UserId, request.Token);
            return result;
        }
    }
}

