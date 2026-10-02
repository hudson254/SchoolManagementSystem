using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Notifications;
using SMS.Notifications.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Notifications.Services
{
    /// <summary>
    /// Real-time fan-out for notifications that have ALREADY been persisted.
    /// <para>
    /// <b>This class is deliberately not a writer.</b> The previous revision built a
    /// <c>Notification</c> entity in memory, pushed it over SignalR, and then dropped
    /// it - so "notifications" were invisible to the notification history, the unread
    /// badge and every read-state endpoint, and vanished entirely when no client was
    /// connected. The type also had no callers: <c>INotificationService</c> was
    /// registered in DI but injected nowhere.
    /// </para>
    /// <para>
    /// The authoritative write path is <c>CreateNotificationCommand</c> (MediatR ->
    /// <c>INotificationRepository</c>), which persists first and is transactionally
    /// correct. This service is now only the PUSH step that runs after a successful
    /// write, so "delivered live" and "recorded in history" can no longer disagree.
    /// </para>
    /// <para>
    /// <b>Transport limits (LAN-only deployment).</b> SignalR here is a live channel to
    /// ALREADY-CONNECTED clients only. There is no external push gateway (FCM, APNs,
    /// Web Push), so a closed or suspended client receives nothing until it next loads
    /// the app and re-reads the persisted history. This is by design and is documented
    /// as such rather than papered over.
    /// </para>
    /// </summary>
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

        /// <summary>
        /// Client method name for a newly created notification. The frontend listens on
        /// this exact name.
        /// </summary>
        public const string ReceiveNotificationMethod = "ReceiveNotification";

        /// <summary>
        /// Pushes an already-persisted notification to its owner's live connections.
        /// Returns without throwing if the hub is unreachable: the notification is
        /// already durable, so a transport failure must not surface as a business error.
        /// </summary>
        public async Task SendNotificationAsync(string userId, string title, string message, string? type = null, string? referenceId = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            // The payload is rebuilt rather than accepted from the caller: the push must
            // never be the only place a notification exists, and the shape the client
            // receives has to match what /notifications returns.
            var normalisedType = NotificationTypes.Normalize(type);
            var notification = new Notification
            {
                UserId = userId,
                Title = NotificationCatalog.NormalizeTitle(title),
                Message = NotificationCatalog.NormalizeMessage(message),
                Type = normalisedType,
                ReferenceId = referenceId,
                Priority = NotificationTypes.DefaultPriorityFor(normalisedType),
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            await PushAsync(NotificationHub.GroupForUser(userId), notification);

            _logger.LogInformation("Notification pushed to user {UserId}: {Title}", userId, notification.Title);
        }

        /// <summary>Pushes one notification to each of the given users' live connections.</summary>
        public async Task BroadcastNotificationAsync(string title, string message, IEnumerable<string> userIds, string? type = null)
        {
            if (userIds == null) return;

            var normalisedType = NotificationTypes.Normalize(type);
            var notification = new Notification
            {
                Title = NotificationCatalog.NormalizeTitle(title),
                Message = NotificationCatalog.NormalizeMessage(message),
                Type = normalisedType,
                Priority = NotificationTypes.DefaultPriorityFor(normalisedType),
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            var targets = userIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(NotificationHub.GroupForUser)
                .ToList();

            foreach (var group in targets)
            {
                await PushAsync(group, notification);
            }
        }

        /// <summary>
        /// Pushes to every live connection holding <paramref name="role"/>.
        /// Note this is a live-only convenience: the durable record is written by
        /// <c>SendNotificationToRoleCommand</c>, which resolves the role to concrete
        /// users first. A group push alone would leave no history for anyone who was
        /// not connected at the time, which is why the command - not this method - is
        /// what the HTTP endpoint uses.
        /// </summary>
        public async Task SendRoleNotificationAsync(string title, string message, string role, string? type = null)
        {
            if (string.IsNullOrWhiteSpace(role)) return;

            var normalisedType = NotificationTypes.Normalize(type);
            var notification = new Notification
            {
                Title = NotificationCatalog.NormalizeTitle(title),
                Message = NotificationCatalog.NormalizeMessage(message),
                Type = normalisedType,
                Priority = NotificationTypes.DefaultPriorityFor(normalisedType),
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            await PushAsync(NotificationHub.GroupForRole(role), notification);

            _logger.LogInformation("Role notification pushed to role {Role}: {Title}", role, notification.Title);
        }

        /// <summary>
        /// Password reset notifications are delivered in-app only: this deployment is
        /// air-gapped and has no SMTP relay. The user sees the outcome in the
        /// notification centre and the API returns the outcome directly.
        /// </summary>
        public async Task SendPasswordResetNotificationAsync(string userId, string email, string resetLink)
        {
            await SendNotificationAsync(
                userId,
                "Password Reset Requested",
                "Your password reset request has been submitted. An administrator will review it shortly.",
                SMS.Domain.Notifications.NotificationTypes.Security);

            _logger.LogInformation(
                "Password reset notification (in-app only) sent to user {UserId}", userId);
        }

        /// <summary>Email verification is likewise in-app only on this deployment.</summary>
        public async Task SendVerificationNotificationAsync(string userId, string email, string verificationLink)
        {
            await SendNotificationAsync(
                userId,
                "Email Verification Unavailable",
                "Email verification is not available on this deployment. Please contact your administrator.",
                SMS.Domain.Notifications.NotificationTypes.Security);

            _logger.LogInformation(
                "Verification notification (in-app only) sent to user {UserId}", userId);
        }

        /// <summary>
        /// Sends to a SignalR group, swallowing transport faults. A failed push is not a
        /// failed notification: the row is already committed and the client will pick it
        /// up on its next history read.
        /// </summary>
        private async Task PushAsync(string group, Notification notification)
        {
            try
            {
                await _hubContext.Clients.Group(group).SendAsync(ReceiveNotificationMethod, notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Live push to group {Group} failed; the notification remains available in history",
                    group);
            }
        }
    }
}

