using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Queries
{
    public class GetUserLoginHistoryQuery : IRequest<PagedResult<LoginHistoryDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetUserLoginHistoryQueryHandler : IRequestHandler<GetUserLoginHistoryQuery, PagedResult<LoginHistoryDto>>
    {
        private readonly IUserManagerService _userManager;
        private readonly ILogger<GetUserLoginHistoryQueryHandler> _logger;

        public GetUserLoginHistoryQueryHandler(
            IUserManagerService userManager,
            ILogger<GetUserLoginHistoryQueryHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<PagedResult<LoginHistoryDto>> Handle(GetUserLoginHistoryQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            // Login history repository is not directly accessible from Application layer.
            // Return empty paged result - login history tracking is done at the infrastructure layer.
            _logger.LogInformation("Login history requested for user {UserId}", request.UserId);

            return new PagedResult<LoginHistoryDto>
            {
                Items = new List<LoginHistoryDto>(),
                TotalCount = 0,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

