using SMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SMS.Application.Features.Notifications.Commands
{
    public class MarkNotificationAsReadCommand : IRequest<MediatR.Unit>
    {
        public Guid NotificationId { get; set; }
    }

    public class MarkNotificationAsReadHandler : IRequestHandler<MarkNotificationAsReadCommand, MediatR.Unit>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkNotificationAsReadHandler> _logger;

        public MarkNotificationAsReadHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ILogger<MarkNotificationAsReadHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            await _notificationRepository.MarkAsReadAsync(request.NotificationId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Notification {NotificationId} marked as read", request.NotificationId);
            return MediatR.Unit.Value;
        }
    }

    public class MarkAllNotificationsAsReadCommand : IRequest<MediatR.Unit>
    {
        public string? UserId { get; set; }
    }

    public class MarkAllNotificationsAsReadHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, MediatR.Unit>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkAllNotificationsAsReadHandler> _logger;

        public MarkAllNotificationsAsReadHandler(
            INotificationRepository notificationRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<MarkAllNotificationsAsReadHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var userId = request.UserId ?? _currentUserService?.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
            }
            return MediatR.Unit.Value;
        }
    }

    public class DeleteNotificationCommand : IRequest<MediatR.Unit>
    {
        public Guid NotificationId { get; set; }
    }

    public class DeleteNotificationHandler : IRequestHandler<DeleteNotificationCommand, MediatR.Unit>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteNotificationHandler> _logger;

        public DeleteNotificationHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteNotificationHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification != null)
            {
                await _notificationRepository.DeleteAsync(notification, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Notification {NotificationId} deleted", request.NotificationId);
            }
            return MediatR.Unit.Value;
        }
    }

    public class BroadcastNotificationCommand : IRequest<MediatR.Unit>
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public List<string> UserIds { get; set; } = new();
    }

    public class BroadcastNotificationHandler : IRequestHandler<BroadcastNotificationCommand, MediatR.Unit>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BroadcastNotificationHandler> _logger;

        public BroadcastNotificationHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ILogger<BroadcastNotificationHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(BroadcastNotificationCommand request, CancellationToken cancellationToken)
        {
            var notifications = request.UserIds.Select(userId => new Domain.Entities.Notification
            {
                UserId = userId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type ?? "Broadcast",
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            }).ToList();

            await _notificationRepository.AddRangeAsync(notifications, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Broadcast notification '{Title}' sent to {Count} users", request.Title, request.UserIds.Count);
            return MediatR.Unit.Value;
        }
    }

    public class SendNotificationToRoleCommand : IRequest<MediatR.Unit>
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Type { get; set; }
    }

    public class SendNotificationToRoleHandler : IRequestHandler<SendNotificationToRoleCommand, MediatR.Unit>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserManagerService _userManagerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SendNotificationToRoleHandler> _logger;

        public SendNotificationToRoleHandler(
            INotificationRepository notificationRepository,
            IUserManagerService userManagerService,
            IUnitOfWork unitOfWork,
            ILogger<SendNotificationToRoleHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _userManagerService = userManagerService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(SendNotificationToRoleCommand request, CancellationToken cancellationToken)
        {
            var usersInRole = await _userManagerService.GetUsersByRoleAsync(request.Role);
            var userIds = usersInRole?.Select(u => u.Id).ToList() ?? new List<string>();

            if (userIds.Count == 0)
            {
                _logger.LogWarning("No users found in role {Role} for notification", request.Role);
                return MediatR.Unit.Value;
            }

            var notifications = userIds.Select(userId => new Domain.Entities.Notification
            {
                UserId = userId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type ?? "Role",
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            }).ToList();

            await _notificationRepository.AddRangeAsync(notifications, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role notification '{Title}' sent to {Count} users in role {Role}", request.Title, userIds.Count, request.Role);
            return MediatR.Unit.Value;
        }
    }
}
