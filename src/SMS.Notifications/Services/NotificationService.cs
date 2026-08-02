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

        /// <summary>
        /// Email notifications are not supported — SMTP has been fully removed
        /// from the system per owner requirement (isolated LAN deployment).
        /// </summary>
        public Task SendEmailNotificationAsync(string userId, string email, string subject, string body)
        {
            _logger.LogWarning("Email notification requested but SMTP is disabled. User: {UserId}, Email: {Email}, Subject: {Subject}", userId, email, subject);
            return Task.CompletedTask;
        }

        public async Task SendSmsNotificationAsync(string userId, string phoneNumber, string message)
        {
            try
            {
                // SMS service is currently a stub (logs only). Keep the call
                // site intact in case a real implementation is added later.
                // Re-activate by injecting ISmsService back into the ctor.
                _logger.LogInformation("SMS notification to {PhoneNumber} for user {UserId}: {Message}", phoneNumber, userId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS notification to {PhoneNumber} for user {UserId}", phoneNumber, userId);
                throw;
            }
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
        /// Templated email notifications are not supported — SMTP has been fully
        /// removed from the system per owner requirement.
        /// </summary>
        public Task SendTemplatedEmailAsync(string userId, string email, string templateName, Dictionary<string, string> templateData)
        {
            _logger.LogWarning("Templated email notification requested but SMTP is disabled. User: {UserId}, Email: {Email}, Template: {Template}", userId, email, templateName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Password reset notifications are no longer sent via email. Password
        /// resets are now admin-mediated (see PasswordResetRequest entity).
        /// </summary>
        public async Task SendPasswordResetNotificationAsync(string userId, string email, string resetLink)
        {
            // Email path removed. Send an in-app notification instead so the
            // user sees the request status in the UI.
            await SendNotificationAsync(userId, "Password Reset", "Your password reset request has been submitted. An administrator will review it shortly.", "Security");

            _logger.LogInformation("Password reset notification (in-app only) sent to user {UserId}", userId);
        }

        /// <summary>
        /// Email verification notifications are not supported — SMTP has been
        /// fully removed from the system per owner requirement.
        /// </summary>
        public async Task SendVerificationNotificationAsync(string userId, string email, string verificationLink)
        {
            // Email path removed. Send an in-app notification instead.
            await SendNotificationAsync(userId, "Email Verification", "Email verification is not available on this deployment. Contact your administrator.", "Security");

            _logger.LogInformation("Verification notification (in-app only) sent to user {UserId}", userId);
        }
    }
}

