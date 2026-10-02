using SMS.Domain.Notifications;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// The single write path for in-app notifications.
    /// <para>
    /// Business handlers depend on THIS rather than on the repository or MediatR's
    /// <c>CreateNotificationCommand</c> directly, for three reasons:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Recipient resolution lives in one place.</b> Role, user and
    /// self-resolution all funnel through here so targeting cannot drift between
    /// call sites.</item>
    /// <item><b>A notification can never fail the business operation.</b> Every method
    /// swallows and logs notification faults. Raising a notification is a
    /// side-effect of a state transition that has already been committed; it must
    /// never roll back the transition.</item>
    /// <item><b>Tenant + catalogue normalisation are enforced once</b>, so no caller can
    /// write an unsafe action URL, an unknown type, or a notification outside the
    /// caller's tenant.</item>
    /// </list>
    /// </summary>
    public interface INotificationDispatcher
    {
        /// <summary>
        /// Raises a notification for one user. <paramref name="userId"/> is required;
        /// a null/blank recipient is a no-op.
        /// </summary>
        Task NotifyUserAsync(
            string? userId,
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Raises the same notification for every user holding any of
        /// <paramref name="roles"/>. Role names are de-duplicated case-insensitively;
        /// recipients are resolved from the live role membership and then de-duplicated
        /// so a user holding two of the roles receives exactly one copy.
        /// </summary>
        Task NotifyRolesAsync(
            IEnumerable<string>? roles,
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Raises the same notification for an explicit recipient set (used by
        /// broadcast-style events such as "all students enrolled in unit X").
        /// </summary>
        Task NotifyUsersAsync(
            IEnumerable<string>? userIds,
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Convenience overload: raises a notification addressed to the caller.
        /// </summary>
        Task NotifyCurrentUserAsync(
            string title,
            string message,
            string? type = null,
            string? referenceId = null,
            string? actionUrl = null,
            string? priority = null,
            CancellationToken cancellationToken = default);
    }
}