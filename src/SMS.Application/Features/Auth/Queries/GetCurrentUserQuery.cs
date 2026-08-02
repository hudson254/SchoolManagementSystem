using MediatR;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;

namespace SMS.Application.Features.Auth.Queries
{
    public class GetCurrentUserQuery : IRequest<UserProfileDto>
    {
        public Guid UserId { get; set; }
    }

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly ILogger<GetCurrentUserQueryHandler> _logger;

        public GetCurrentUserQueryHandler(
            IUserManagerService userManager,
            ILogger<GetCurrentUserQueryHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", request.UserId);
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Organization = user.Organization,
                Roles = roles.ToList(),
                IsEmailVerified = user.IsEmailVerified,
                LastLoginDate = user.LastLoginDate,
                LastLoginIP = user.LastLoginIP,
                CreatedDate = user.CreatedDate,
                TenantId = user.TenantId
            };
        }
    }
}