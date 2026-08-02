using SMS.Application.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
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
    }

    public class CreateNotificationHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<CreateNotificationHandler> _logger;

        public CreateNotificationHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<CreateNotificationHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type ?? "System",
                ReferenceId = request.ReferenceId,
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notification created for user {UserId}: {Title}", request.UserId, request.Title);

            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type ?? "System",
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedDate ?? DateTime.UtcNow,
                SenderId = null
            };
        }
    }
}
