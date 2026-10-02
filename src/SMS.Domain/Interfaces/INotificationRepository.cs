using SMS.Domain.Entities;
using SMS.Domain.Notifications;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetNotificationsByUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(string type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Server-side paging over ONE user's notification history.
        /// <para>
        /// This is the only method the API should use to list notifications: it pages
        /// in the database and returns the total in the same round-trip, so a user with
        /// thousands of notifications can never force an unbounded materialisation.
        /// </para>
        /// <para>
        /// <paramref name="isRead"/> null = all, true = read only, false = unread only.
        /// When <paramref name="unreadOnly"/> is set, read notifications are excluded
        /// even if <paramref name="isRead"/> is null.
        /// </para>
        /// </summary>
        Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedForUserAsync(
            string userId,
            int page,
            int pageSize,
            bool? isRead,
            bool unreadOnly = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// True when the user has at least one UNREAD notification whose priority is
        /// <c>Important</c> or <c>Critical</c>.
        /// <para>
        /// This is a dedicated COUNT rather than a client-side scan because the
        /// notification list is ordered newest-first: inspecting only the newest unread
        /// row would report "no critical" whenever a routine notification arrived after
        /// an unresolved critical one.
        /// </para>
        /// </summary>
        Task<bool> HasUnreadAtOrAbovePriorityAsync(string userId, string minimumPriority, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches a notification ONLY if it belongs to <paramref name="userId"/>.
        /// This is the ownership-enforcing read. A notification belonging to another
        /// user is reported as <c>null</c> rather than as a forbidden error, so the
        /// endpoint cannot be used to probe for the existence of another user's
        /// notification ids.
        /// </summary>
        Task<Notification?> GetForUserAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a notification read ONLY if it belongs to <paramref name="userId"/>.
        /// Returns false when the notification does not exist, is soft-deleted, or
        /// belongs to somebody else - the three cases are deliberately indistinguishable.
        /// Idempotent: re-marking an already-read notification returns true and does
        /// not move <c>ReadAt</c>.
        /// </summary>
        Task<bool> MarkAsReadForUserAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes a notification ONLY if it belongs to <paramref name="userId"/>.</summary>
        Task<bool> DeleteForUserAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default);
    }
}
