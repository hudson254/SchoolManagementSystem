using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Thin adapter over the existing in-app notification subsystem
    /// (<c>CreateNotificationCommand</c> / <c>NotificationService</c>).
    ///
    /// The OMS request engine must not introduce a second notification stack:
    /// every lifecycle transition routes through the existing notification
    /// persistence and hub via this abstraction. Failures here are logged and
    /// swallowed by the implementation so that a notification outage can never
    /// roll back an authoritative workflow transition.
    /// </summary>
    public interface IOmsRequestNotifier
    {
        /// <summary>
        /// Sends an in-app notification to a single user.
        /// </summary>
        /// <param name="userId">Recipient user identifier.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification body.</param>
        /// <param name="requestId">Related OMS request id (used as reference id).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task NotifyAsync(
            string? userId,
            string title,
            string message,
            Guid? requestId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an in-app notification to every active user holding any of the
        /// supplied roles. Used for queue notifications where the OMS request
        /// engine does not know (and must not know) which individual approver
        /// will pick the request up.
        /// </summary>
        /// <param name="roleNames">Role names defining the target queue.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification body.</param>
        /// <param name="requestId">Related OMS request id (used as reference id).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task NotifyRolesAsync(
            IEnumerable<string> roleNames,
            string title,
            string message,
            Guid? requestId,
            CancellationToken cancellationToken = default);
    }
}
