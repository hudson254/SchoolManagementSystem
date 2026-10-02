using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;
using System;

namespace SMS.Application.Features.Notifications.Queries
{
    /// <summary>
    /// Fetches ONE notification belonging to the caller.
    /// <para>
    /// Security note: this handler previously resolved the notification by primary key
    /// alone (<c>GetByIdAsync</c>), which let ANY authenticated user read ANY
    /// notification in the tenant by passing its id. It now resolves through the
    /// ownership-enforcing <c>GetForUserAsync</c>, and a notification belonging to
    /// somebody else is reported as 404 rather than 403 so the endpoint cannot be used
    /// to probe for the existence of another user's notification ids.
    /// </para>
    /// </summary>
    public class GetNotificationQuery : IRequest<NotificationDto>
    {
        public Guid NotificationId { get; set; }
    }

    public class GetNotificationHandler : IRequestHandler<GetNotificationQuery, NotificationDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<GetNotificationHandler> _logger;

        public GetNotificationHandler(
            INotificationRepository notificationRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<GetNotificationHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<NotificationDto> Handle(GetNotificationQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService?.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new NotFoundException("Notification", request.NotificationId);

            var notification = await _notificationRepository.GetForUserAsync(
                request.NotificationId, userId, cancellationToken);

            // Null covers all three indistinguishable cases: does not exist, soft-deleted,
            // or belongs to another user. All are reported as 404 on purpose.
            if (notification == null)
            {
                _logger.LogWarning(
                    "Notification {NotificationId} was requested by user {UserId} but is not visible to them",
                    request.NotificationId, userId);
                throw new NotFoundException("Notification", request.NotificationId);
            }

            return NotificationMapper.ToDto(notification);
        }
    }

    public class GetUnreadNotificationCountQuery : IRequest<UnreadCountDto> { }

    public class GetUnreadNotificationCountHandler : IRequestHandler<GetUnreadNotificationCountQuery, UnreadCountDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<GetUnreadNotificationCountHandler> _logger;

        public GetUnreadNotificationCountHandler(
            INotificationRepository notificationRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<GetUnreadNotificationCountHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<UnreadCountDto> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService?.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                return new UnreadCountDto { Count = 0, HasCritical = false };

            // COUNT in the database. The previous implementation materialised every
            // unread row into memory purely to call .Count() on the resulting list,
            // which is O(history) on a request the header fires on every page load.
            var count = await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

            // A dedicated EXISTS lets the bell emphasise unresolved Important/Critical
            // items without the client downloading the history, and without the
            // "only look at the newest row" blind spot.
            var hasCritical = await _notificationRepository.HasUnreadAtOrAbovePriorityAsync(
                userId, NotificationPriorities.Important, cancellationToken);

            return new UnreadCountDto { Count = count, HasCritical = hasCritical };
        }
    }
}
