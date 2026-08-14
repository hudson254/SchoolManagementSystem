using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Notifications.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string? type = null, string? referenceId = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type ?? "System",
                ReferenceId = referenceId,
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            // Send real-time notification via SignalR
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
        }

        public async Task BroadcastNotificationAsync(string title, string message, IEnumerable<string> userIds, string? type = null)
        {
            var tasks = userIds.Select(userId => SendNotificationAsync(userId, title, message, type));
            await Task.WhenAll(tasks);
            _logger.LogInformation("Broadcast notification sent to {Count} users", userIds.Count());
        }

        public async Task SendRoleNotificationAsync(string title, string message, string role, string? type = null)
        {
            // Send to SignalR group for the role
            await _hubContext.Clients.Group($"role_{role}").SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Type = type ?? "System",
                CreatedDate = DateTime.UtcNow
            });

            _logger.LogInformation("Role notification sent to role {Role}: {Title}", role, title);
        }

        /// <summary>
        /// Password reset notifications are now delivered in-app only.
        /// </summary>
        public async Task SendPasswordResetNotificationAsync(string userId, string email, string resetLink)
        {
            // Email path removed. Send an in-app notification instead so the
            // user sees the request status in the UI.
            await SendNotificationAsync(userId, "Password Reset", "Your password reset request has been submitted. An administrator will review it shortly.", "Security");

            _logger.LogInformation("Password reset notification (in-app only) sent to user {UserId}", userId);
        }

        public async Task SendVerificationNotificationAsync(string userId, string email, string verificationLink)
        {
            // Email path removed. Send an in-app notification instead.
            await SendNotificationAsync(userId, "Email Verification", "Email verification is not available on this deployment. Contact your administrator.", "Security");

            _logger.LogInformation("Verification notification (in-app only) sent to user {UserId}", userId);
        }
    }
}

