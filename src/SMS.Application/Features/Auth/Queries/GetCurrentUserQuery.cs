using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Auth.Queries
{
    public class GetCurrentUserQuery : IRequest<UserProfileDto>
    {
        public System.Guid UserId { get; set; }
    }

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
    {
        private readonly IUserManagerService _userManagerService;
        private readonly ILogger<GetCurrentUserQueryHandler> _logger;

        public GetCurrentUserQueryHandler(
            IUserManagerService userManagerService,
            ILogger<GetCurrentUserQueryHandler> logger)
        {
            _userManagerService = userManagerService;
            _logger = logger;
        }

        public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var userId = request.UserId.ToString();

            // Get user from UserManager
            var user = await _userManagerService.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("GetCurrentUser: User {UserId} not found", userId);
                throw new NotFoundException("User", request.UserId);
            }

            // Get user roles
            var roles = await _userManagerService.GetRolesAsync(user);
            var rolesList = roles?.ToList() ?? new List<string>();

            // Build profile DTO
            var profile = new UserProfileDto
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName ?? string.Empty,
                MiddleName = user.MiddleName,
                LastName = user.LastName ?? string.Empty,
                Title = user.Title,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                CreatedDate = user.CreatedDate ?? user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                LastLoginDate = user.LastLoginDate,
                LastLoginIP = user.LastLoginIP,
                Organization = user.Organization,
                TenantId = user.TenantId,
                Roles = rolesList
            };

            _logger.LogInformation("Retrieved current user profile for {UserId}", userId);

            return profile;
        }
    }
}
