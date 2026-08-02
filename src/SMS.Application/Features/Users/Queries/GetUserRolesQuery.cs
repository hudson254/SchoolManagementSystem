using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Queries
{
    public class GetUserRolesQuery : IRequest<IEnumerable<string>>
    {
        public Guid UserId { get; set; }
    }

    public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, IEnumerable<string>>
    {
        private readonly IUserManagerService _userManager;
        private readonly ILogger<GetUserRolesQueryHandler> _logger;

        public GetUserRolesQueryHandler(
            IUserManagerService userManager,
            ILogger<GetUserRolesQueryHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            var roles = await _userManager.GetRolesAsync(user);
            return roles ?? Enumerable.Empty<string>();
        }
    }
}

