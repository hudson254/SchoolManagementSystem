using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SMS.Application.Features.Notifications.Queries
{
    public class GetMyNotificationsQuery : IRequest<PagedResult<NotificationDto>>
    {
        public string? UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? IsRead { get; set; }
    }

    public class GetMyNotificationsHandler : IRequestHandler<GetMyNotificationsQuery, PagedResult<NotificationDto>>
    {
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
            var userId = request.UserId ?? _currentUserService?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new PagedResult<NotificationDto>();
            }

            IEnumerable<Domain.Entities.Notification> notifications;
            if (request.IsRead == false)
            {
                notifications = await _notificationRepository.GetUnreadNotificationsAsync(userId, cancellationToken);
            }
            else
            {
                notifications = await _notificationRepository.GetNotificationsByUserAsync(userId, cancellationToken);
            }

            var list = notifications.ToList();
            var totalCount = list.Count;

            var pagedItems = list
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type ?? "System",
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedDate ?? n.CreatedAt,
                    SenderId = null
                })
                .ToList();

            return new PagedResult<NotificationDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = request.Page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}
