using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Notifications.Queries
{
    public class GetNotificationQuery : IRequest<NotificationDto>
    {
        public Guid NotificationId { get; set; }
    }

    public class GetNotificationHandler : IRequestHandler<GetNotificationQuery, NotificationDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<GetNotificationHandler> _logger;

        public GetNotificationHandler(INotificationRepository notificationRepository, ILogger<GetNotificationHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<NotificationDto> Handle(GetNotificationQuery request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
                throw new NotFoundException("Notification", request.NotificationId);

            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type ?? "System",
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedDate ?? notification.CreatedAt,
                SenderId = null
            };
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
            if (string.IsNullOrEmpty(userId))
            {
                return new UnreadCountDto { Count = 0 };
            }

            var unreadNotifications = await _notificationRepository.GetUnreadNotificationsAsync(userId, cancellationToken);
            var count = unreadNotifications?.Count() ?? 0;

            _logger.LogInformation("User {UserId} has {Count} unread notifications", userId, count);

            return new UnreadCountDto { Count = count };
        }
    }
}
