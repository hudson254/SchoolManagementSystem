using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Queries
{
    public class GetUsersQuery : IRequest<PagedResult<UserDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        private readonly IUserManagerService _userManager;
        private readonly ILogger<GetUsersQueryHandler> _logger;

        public GetUsersQueryHandler(
            IUserManagerService userManager,
            ILogger<GetUsersQueryHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var allUsers = await _userManager.GetAllUsersAsync();
            var query = allUsers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    (u.FirstName ?? "").ToLower().Contains(term) ||
                    (u.LastName ?? "").ToLower().Contains(term) ||
                    (u.Email ?? "").ToLower().Contains(term) ||
                    (u.UserName ?? "").ToLower().Contains(term));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var roleUsers = await _userManager.GetUsersByRoleAsync(request.Role);
                var roleUserIds = roleUsers.Select(u => u.Id).ToHashSet();
                query = query.Where(u => roleUserIds.Contains(u.Id));
            }

            var totalCount = query.Count();

            var users = query
                .OrderBy(u => u.FirstName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var items = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserDto
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
                    LastLoginAt = user.LastLoginDate,
                    Organization = user.Organization ?? string.Empty,
                    TenantId = user.TenantId,
                    Roles = roles?.ToList() ?? new List<string>()
                });
            }

            return new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

