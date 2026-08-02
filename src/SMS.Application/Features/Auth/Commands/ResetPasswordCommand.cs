using MediatR;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Auth.Commands
{
    public class ResetPasswordCommand : IRequest<bool>
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
    {
        private readonly IUserManagerService _userManager;

        public ResetPasswordCommandHandler(IUserManagerService userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var result = await _userManager.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
            return result;
        }
    }
}

