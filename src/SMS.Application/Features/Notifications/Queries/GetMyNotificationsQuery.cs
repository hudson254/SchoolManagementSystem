using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace SMS.Application.Features.Notifications.Queries
{
    /// <summary>
    /// Lists the CALLER'S notification history.
    /// <para>
    /// The recipient is always resolved from the authenticated principal. There is no
    /// client-supplied UserId on the query contract for this endpoint: an earlier
    /// revision exposed one, which would have let any authenticated user enumerate
    /// another user's notifications by passing their id.
    /// </para>
    /// </summary>
    public class GetMyNotificationsQuery : IRequest<PagedResult<NotificationDto>>
    {
        /// <summary>1-based page number. Clamped by the handler.</summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size. Clamped to [1, 100] by the handler so a single request can never
        /// ask the database for an unbounded page.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>null = all, true = read only, false = unread only.</summary>
        public bool? IsRead { get; set; }
    }

    public class GetMyNotificationsHandler : IRequestHandler<GetMyNotificationsQuery, PagedResult<NotificationDto>>
    {
        /// <summary>Maximum page size accepted from a client.</summary>
        public const int MaxPageSize = 100;

        private readonly INotificationRepository _notificationRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<GetMyNotificationsHandler> _logger;

        public GetMyNotificationsHandler(
            INotificationRepository notificationRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<GetMyNotificationsHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<PagedResult<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService?.UserId;

            // No authenticated principal => no notifications. Never "all notifications".
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Rejected notification history request with no authenticated principal");
                return new PagedResult<NotificationDto>
                {
                    Items = new System.Collections.Generic.List<NotificationDto>(),
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 0
                };
            }

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1
                ? 20
                : (request.PageSize > MaxPageSize ? MaxPageSize : request.PageSize);

            // Paged IN THE DATABASE. The previous implementation loaded every row for
            // the user and then Skip/Take'd in memory, so a user with a long history
            // forced an unbounded read on every page load.
            var (items, totalCount) = await _notificationRepository.GetPagedForUserAsync(
                userId, page, pageSize, request.IsRead, unreadOnly: false, cancellationToken);

            return new PagedResult<NotificationDto>
            {
                Items = NotificationMapper.ToDtoList(items),
                TotalCount = totalCount,
                Page = page,
                // PageSize must be set explicitly: PagedResult computes TotalPages from
                // it, and leaving it at the default of 10 produced a page count that
                // disagreed with the items actually returned.
                PageSize = pageSize
            };
        }
    }
}
