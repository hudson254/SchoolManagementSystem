using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Queries
{
    public class GetUserQuery : IRequest<UserDto>
    {
        public Guid UserId { get; set; }
    }

    public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly ILogger<GetUserQueryHandler> _logger;

        public GetUserQueryHandler(
            IUserManagerService userManager,
            ILogger<GetUserQueryHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginDate ?? user.UpdatedAt,
                Organization = user.Organization ?? string.Empty,
                TenantId = user.TenantId,
                Roles = roles?.ToList() ?? new List<string>()
            };
        }
    }
}

