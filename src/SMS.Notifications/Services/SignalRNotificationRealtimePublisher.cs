using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.DTOs;
using SMS.Notifications.Hubs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Notifications.Services
{
    /// <summary>
    /// SignalR implementation of <see cref="INotificationRealtimePublisher"/>.
    /// <para>
    /// Sends to the connection's own <c>user_{id}</c> group, which the hub populates
    /// from the authenticated principal. There is no client-supplied destination: the
    /// recipient comes from the persisted notification the API just wrote.
    /// </para>
    /// <para>
    /// This is a LIVE-ONLY channel. The deployment is LAN-only and air-gapped, so no
    /// Web Push / FCM / APNs endpoint is involved and a browser that is not connected
    /// receives nothing. Delivery to an offline user happens when they next load the app
    /// and the client re-reads the notification history.
    /// </para>
    /// </summary>
    public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SignalRNotificationRealtimePublisher> _logger;

        public SignalRNotificationRealtimePublisher(
            IHubContext<NotificationHub> hubContext,
            ILogger<SignalRNotificationRealtimePublisher> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task PublishAsync(string recipientUserId, NotificationDto notification, CancellationToken cancellationToken = default)
        {
            if (notification == null) return;
            if (string.IsNullOrWhiteSpace(recipientUserId)) return;

            // The recipient comes from the persisted notification the API just wrote,
            // never from client input. The hub populates this group from the
            // authenticated principal, so a client cannot subscribe itself to somebody
            // else's stream.
            await _hubContext.Clients.Group(NotificationHub.GroupForUser(recipientUserId))
                .SendAsync(NotificationService.ReceiveNotificationMethod, notification, cancellationToken);
        }
    }
}