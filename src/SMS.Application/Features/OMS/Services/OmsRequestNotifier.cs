using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Notifications.Commands;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Services
{
    /// <summary>
    /// Sends OMS request notifications through the existing in-app notification
    /// pipeline. No SMTP / Twilio / external gateway is used.
    /// </summary>
    public class OmsRequestNotifier : IOmsRequestNotifier
    {
        /// <summary>Notification type stored against the notification row.</summary>
        public const string NotificationType = "OmsRequest";

        private readonly ISender _sender;
        private readonly IUserManagerService _userManagerService;
        private readonly ILogger<OmsRequestNotifier> _logger;

        public OmsRequestNotifier(
            ISender sender,
            IUserManagerService userManagerService,
            ILogger<OmsRequestNotifier> logger)
        {
            _sender = sender;
            _userManagerService = userManagerService;
            _logger = logger;
        }

        public async Task NotifyAsync(
            string? userId,
            string title,
            string message,
            Guid? requestId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            try
            {
                await _sender.Send(new CreateNotificationCommand
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = NotificationType,
                    ReferenceId = requestId?.ToString()
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A notification failure must never abort the authoritative
                // workflow transition that triggered it.
                _logger.LogWarning(
                    ex,
                    "Failed to dispatch OMS request notification '{Title}' to user {UserId} for request {RequestId}",
                    title, userId, requestId);
            }
        }

        public async Task NotifyRolesAsync(
            IEnumerable<string> roleNames,
            string title,
            string message,
            Guid? requestId,
            CancellationToken cancellationToken = default)
        {
            if (roleNames == null)
                return;

            // Distinct, non-empty roles only - callers commonly pass an
            // overlapping set of roles (e.g. Admin + Administrator).
            var roles = roleNames
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (roles.Count == 0)
                return;

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roles)
            {
                try
                {
                    var users = await _userManagerService.GetUsersByRoleAsync(role);
                    if (users == null)
                        continue;

                    foreach (var user in users)
                    {
                        if (!string.IsNullOrWhiteSpace(user?.Id))
                            recipients.Add(user.Id);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Role resolution is best-effort: a single unknown role must
                    // not prevent the remaining queues from being notified.
                    _logger.LogWarning(ex, "Failed to resolve users for role {Role} while dispatching OMS request notification", role);
                }
            }

            foreach (var recipient in recipients)
            {
                await NotifyAsync(recipient, title, message, requestId, cancellationToken);
            }
        }
    }
}
