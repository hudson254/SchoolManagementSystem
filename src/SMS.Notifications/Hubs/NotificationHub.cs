using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SMS.Notifications.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Notification client connected: User={UserId}, Connection={ConnectionId}", userId, connectionId);

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(connectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Notification client disconnected: User={UserId}, Connection={ConnectionId}", userId, connectionId);

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(connectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task MarkAsRead(string notificationId)
        {
            await Clients.Caller.SendAsync("NotificationRead", notificationId);
        }

        public async Task SubscribeToNotifications(string userId)
        {
            var connectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(connectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} subscribed to notifications on connection {ConnectionId}", userId, connectionId);
        }
    }
}

