using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Notifications.Commands;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Services
{
    /// <summary>
    /// Default <see cref="INotificationDispatcher"/>.
    /// <para>
    /// Every method here is fault-isolating by design. A business handler calls the
    /// dispatcher AFTER it has committed its own transaction; if notification
    /// creation then throws, the authoritative state change has already happened and
    /// MUST NOT be rolled back. So failures are logged and swallowed. The one
    /// exception is cancellation caused by a genuinely cancelled request, which is
    /// rethrown so the request pipeline can shut down cleanly.
    /// </para>
    /// </summary>
    public class NotificationDispatcher : INotificationDispatcher
    {
        /// <summary>
        /// Upper bound on recipients resolved for a single fan-out. Role membership is
        /// bounded by the tenant size, but an explicit id list arrives from application
        /// code and must not be able to enqueue an unbounded batch.
        /// </summary>
        public const int MaxRecipients = 5000;

        private readonly ISender _sender;
        private readonly IUserManagerService _userManagerService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(
            ISender sender,
            IUserManagerService userManagerService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ILogger<NotificationDispatcher> logger)
        {
            _sender = sender;
            _userManagerService = userManagerService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public Task NotifyUserAsync(
            string? userId,
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return Task.CompletedTask;

            return NotifyUsersAsync(
                new[] { userId }, title, message, type, referenceId, actionUrl, priority,
                expiresAt, cancellationToken);
        }

        public async Task NotifyUsersAsync(
            IEnumerable<string>? userIds,
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default)
        {
            if (userIds == null) return;

            var normalisedType = NotificationTypes.Normalize(type);
            var normalisedActionUrl = NotificationCatalog.NormalizeActionUrl(actionUrl);
            var normalisedPriority = string.IsNullOrWhiteSpace(priority)
                ? NotificationTypes.DefaultPriorityFor(normalisedType)
                : NotificationPriorities.Normalize(priority);
            var safeTitle = NotificationCatalog.NormalizeTitle(title);
            var safeMessage = NotificationCatalog.NormalizeMessage(message);

            if (safeTitle.Length == 0) return; // nothing meaningful to say

            // Distinct, non-blank recipients only. A user listed twice (two roles, or
            // duplicate ids in the source list) must receive exactly one copy.
            var recipients = userIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxRecipients)
                .ToList();

            if (recipients.Count == 0) return;

            // Fan out sequentially rather than with Task.WhenAll: each Send performs its
            // own SaveChanges on the shared scoped DbContext, and EF Core DbContext is
            // NOT thread-safe. Bounded sequential fan-out keeps that invariant intact.
            var sent = 0;
            foreach (var userId in recipients)
            {
                try
                {
                    await _sender.Send(new CreateNotificationCommand
                    {
                        UserId = userId,
                        Title = safeTitle,
                        Message = safeMessage,
                        Type = normalisedType,
                        ReferenceId = referenceId,
                        ActionUrl = normalisedActionUrl,
                        Priority = normalisedPriority,
                        ExpiresAt = expiresAt
                    }, cancellationToken);
                    sent++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Real request cancellation: let it propagate.
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to dispatch notification '{Title}' ({Type}) to user {UserId}; the triggering operation continues",
                        safeTitle, normalisedType, userId);
                }
            }

            _logger.LogInformation(
                "Dispatched notification '{Title}' ({Type}, {Priority}) to {Sent}/{Requested} recipients",
                safeTitle, normalisedType, normalisedPriority, sent, recipients.Count);
        }

        public async Task NotifyRolesAsync(
            IEnumerable<string>? roles,
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            CancellationToken cancellationToken = default)
        {
            if (roles == null) return;

            // Callers routinely pass overlapping role sets (Admin + Administrator).
            var distinctRoles = roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctRoles.Count == 0) return;

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in distinctRoles)
            {
                if (cancellationToken.IsCancellationRequested) return;

                try
                {
                    var users = await _userManagerService.GetUsersByRoleAsync(role);
                    if (users == null) continue;

                    foreach (var user in users)
                    {
                        if (!string.IsNullOrWhiteSpace(user?.Id))
                            recipients.Add(user.Id);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Best-effort resolution: an unknown/unreachable role must not stop
                    // the remaining roles from being notified.
                    _logger.LogWarning(ex,
                        "Failed to resolve users for role {Role} while dispatching notification '{Title}'",
                        role, title);
                }
            }

            await NotifyUsersAsync(
                recipients, title, message, type, referenceId, actionUrl, priority,
                expiresAt: null, cancellationToken: cancellationToken);
        }

        public async Task NotifyCurrentUserAsync(
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUser?.UserId;
            if (string.IsNullOrWhiteSpace(userId)) return;

            await NotifyUserAsync(
                userId, title, message, type, referenceId, actionUrl, priority,
                expiresAt: null, cancellationToken: cancellationToken);
        }
    }
}