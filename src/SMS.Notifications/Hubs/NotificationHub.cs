using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SMS.Notifications.Hubs
{
    /// <summary>
    /// Real-time notification delivery for LOGGED-IN users.
    /// <para>
    /// Scope of this transport - important for the documentation to be accurate: this
    /// hub only delivers notifications WHILE A CLIENT IS CONNECTED. It is not a push
    /// service. The deployment is LAN-only and air-gapped, so there is no Web Push /
    /// FCM endpoint behind it: a browser that is suspended, closed, or offline will
    /// receive nothing. Durability comes entirely from the persisted Notifications
    /// table, which the client re-reads on (re)connect and on window focus. That is
    /// why <c>CreateNotificationCommand</c> always persists BEFORE anything is pushed
    /// here - the push is an accelerator, never the system of record.
    /// </para>
    /// <para><b>Authorization.</b> The hub carries <c>[Authorize]</c>, and the endpoint
    /// mapping additionally calls <c>RequireAuthorization()</c>. Group membership is
    /// derived exclusively from the authenticated principal's claims; there is no
    /// client-supplied user id anywhere in this class. A previous revision exposed a
    /// <c>SubscribeToNotifications(userId)</c> method that let any caller join any
    /// user's group - that was a cross-user information-disclosure hole and it has been
    /// removed rather than merely documented.</para>
    /// </summary>
    [Authorize]
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

            if (string.IsNullOrEmpty(userId))
            {
                // Should be unreachable behind [Authorize]; fail closed rather than
                // silently joining no group and looking like a broken client.
                _logger.LogWarning(
                    "Rejected unauthenticated notification connection {ConnectionId}", connectionId);
                Context.Abort();
                return;
            }

            // Group membership is derived from the token, never from the payload.
            await Groups.AddToGroupAsync(connectionId, GroupForUser(userId));

            // Role groups let an administrator broadcast to a role group. Membership is
            // likewise taken from the claims the server already validated.
            foreach (var role in GetClaimRoles())
            {
                await Groups.AddToGroupAsync(connectionId, GroupForRole(role));
            }

            _logger.LogInformation(
                "Notification client connected: User={UserId}, Connection={ConnectionId}", userId, connectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            _logger.LogInformation(
                "Notification client disconnected: User={UserId}, Connection={ConnectionId}, Error={Error}",
                userId, connectionId, exception?.Message ?? "none");

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// SignalR group name for a user's private stream. Centralised so the hub and
        /// the publisher can never disagree about the name.
        /// </summary>
        public static string GroupForUser(string userId) => $"user_{userId}";

        /// <summary>SignalR group name for a role broadcast.</summary>
        public static string GroupForRole(string role) => $"role_{role}";

        /// <summary>
        /// Reads the role claims from the validated principal. Uses the standard
        /// ClaimTypes.Role mapping that the JWT bearer handler produces, with the
        /// short "role" claim type as a fallback.
        /// </summary>
        private IEnumerable<string> GetClaimRoles()
        {
            var principal = Context.User;
            if (principal?.Claims == null) return Array.Empty<string>();

            var roles = principal.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return roles.ToList();
        }
    }
}

