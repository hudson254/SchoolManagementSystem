using SMS.Application.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace SMS.Application.Features.Notifications.Commands
{
    public class CreateNotificationCommand : IRequest<NotificationDto>
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? ReferenceId { get; set; }

        /// <summary>
        /// Optional application-relative navigation target. Sanitised on write.</summary>
        public string? ActionUrl { get; set; }

        /// <summary>One of NotificationPriorities. Defaults to the type's usual priority.</summary>
        public string? Priority { get; set; }

        /// <summary>Optional expiry. The row stays in history after it passes.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Suppress the real-time SignalR push for this one notification. Used by bulk
        /// fan-out, where the dispatcher pushes per recipient anyway and a per-row push
        /// would multiply the work.
        /// </summary>
        public bool SuppressRealtimePush { get; set; }
    }

    /// <summary>
    /// Real-time push port, implemented in SMS.Notifications over SignalR.
    /// <para>
    /// Declared in the Application layer and resolved optionally so that the
    /// notification WRITE path does not take a hard dependency on the transport: a
    /// failed push must never fail a persisted notification. Only the push needs this;
    /// history, unread counts and read state are served entirely from the database.
    /// </para>
    /// </summary>
    public interface INotificationRealtimePublisher
    {
        /// <summary>
        /// Best-effort live delivery to one user's connected clients.
        /// <para>
        /// The recipient is passed explicitly rather than read from ambient state: the
        /// notification DTO deliberately does not expose <c>UserId</c>, so a caller
        /// cannot accidentally push a payload to the wrong group, and the publisher
        /// holds no mutable per-send state.
        /// </para>
        /// </summary>
        Task PublishAsync(string recipientUserId, NotificationDto notification, CancellationToken cancellationToken = default);
    }

    public class CreateNotificationHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;

        // Optional: resolved through the constructor so the write path keeps working
        // in unit tests and in any host that has not wired the transport.
        private readonly INotificationRealtimePublisher? _realtimePublisher;
        private readonly ILogger<CreateNotificationHandler> _logger;

        public CreateNotificationHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<CreateNotificationHandler> logger,
            INotificationRealtimePublisher? realtimePublisher = null)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
            _realtimePublisher = realtimePublisher;
        }

        public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            // Fail closed on an empty recipient rather than creating an orphan row.
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                _logger.LogWarning(
                    "Discarded notification '{Title}': no recipient was resolved",
                    request.Title);
                throw new SMS.Application.Exceptions.ValidationException("A notification requires a recipient user id");
            }

            var type = NotificationTypes.Normalize(request.Type);
            var priority = string.IsNullOrWhiteSpace(request.Priority)
                ? NotificationTypes.DefaultPriorityFor(type)
                : NotificationPriorities.Normalize(request.Priority);

            var notification = new Notification
            {
                UserId = request.UserId.Trim(),
                Title = NotificationCatalog.NormalizeTitle(request.Title),
                Message = NotificationCatalog.NormalizeMessage(request.Message),
                Type = type,
                ReferenceId = request.ReferenceId,
                // Sanitised here as well as in the dispatcher: this handler is the last
                // gate before the database, so the unsafe-value rules cannot be bypassed
                // by calling the command directly.
                ActionUrl = NotificationCatalog.NormalizeActionUrl(request.ActionUrl),
                Priority = priority,
                ExpiresAt = request.ExpiresAt,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification, cancellationToken);

            // TenantId and audit columns (CreatedBy/ModifiedBy) are stamped by
            // ApplicationDbContext.SaveChangesAsync, and PostgreSQL RLS rejects any
            // INSERT whose tenant_id does not match the request's resolved tenant. That
            // is what stops a caller from addressing a notification into another tenant.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Notification created for user {UserId}: {Title} (Type={Type}, Priority={Priority})",
                notification.UserId, notification.Title, notification.Type, notification.Priority);

            var dto = NotificationMapper.ToDto(notification);

            // Push AFTER the row is durable. The notification is already recorded, so a
            // transport failure here is logged and ignored - it must never surface as a
            // failed business operation, and the client will still see it in history.
            if (_realtimePublisher != null && !request.SuppressRealtimePush)
            {
                try
                {
                    await _realtimePublisher.PublishAsync(notification.UserId!, dto, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Live push failed for notification {NotificationId} (user {UserId}); it remains in history",
                        notification.Id, notification.UserId);
                }
            }

            return dto;
        }
    }
}
