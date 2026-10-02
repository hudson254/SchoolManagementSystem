using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context, ILogger<NotificationRepository> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// Upper bound on a single page of notification history. The API clamps to
        /// this too, but the repository enforces it as well so that no future caller
        /// can turn this into an unbounded read.
        /// </summary>
        public const int MaxPageSize = 100;

        /// <summary>
        /// Base query for one user's notifications.
        /// <para>
        /// Tenant isolation is inherited from the global query filter that
        /// <c>ApplicationDbContext</c> applies to every <c>ITenantAwareEntity</c>,
        /// and Notification implements that interface. This method therefore cannot
        /// see another tenant's rows even before PostgreSQL RLS is considered. On top
        /// of that, the <c>UserId</c> predicate is the OWNERSHIP boundary: a
        /// notification is only ever returned to the user it was addressed to.
        /// </para>
        /// </summary>
        private IQueryable<Notification> OwnedBy(string userId)
        {
            // Fail closed: with no user id there are no rows, never "all rows".
            if (string.IsNullOrWhiteSpace(userId))
                return _dbSet.Where(_ => false);

            return _dbSet.Where(n => n.UserId == userId && !n.IsDeleted);
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await OwnedBy(userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await OwnedBy(userId)
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return 0;

            // COUNT in the database - never materialise the unread rows just to count them.
            return await OwnedBy(userId).CountAsync(n => !n.IsRead, cancellationToken);
        }

        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        {
            // NOTE: intentionally unowned. This predates the ownership-enforcing API
            // and is retained only for internal/system callers that already hold the
            // identifier (e.g. background jobs). The HTTP surface uses
            // MarkAsReadForUserAsync instead - see the remarks there.
            var notification = await _dbSet.FindAsync(new object[] { notificationId }, cancellationToken);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                _dbSet.Update(notification);
            }
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var unread = await OwnedBy(userId)
                .Where(n => !n.IsRead)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var notification in unread)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(string type, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(n => n.Type == type && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedForUserAsync(
            string userId,
            int page,
            int pageSize,
            bool? isRead,
            bool unreadOnly = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return (Array.Empty<Notification>(), 0);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > MaxPageSize ? MaxPageSize : pageSize);

            var query = OwnedBy(userId);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);
            else if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var total = await query.CountAsync(cancellationToken);

            // Newest first, with a deterministic tiebreaker so that rows sharing a
            // timestamp cannot shuffle between pages and cause the UI to skip or
            // repeat an entry.
            var items = await query
                .OrderByDescending(n => n.CreatedDate)
                .ThenByDescending(n => n.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<bool> HasUnreadAtOrAbovePriorityAsync(string userId, string minimumPriority, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var minimum = NotificationPriorities.Normalize(minimumPriority);
            var minimumRank = NotificationPriorities.Rank(minimum);

            // Priorities are persisted as the four catalogue strings, so the set of
            // "at or above" values is enumerable up-front and pushed into the query
            // rather than evaluated per row.
            var qualifying = NotificationPriorities.All
                .Where(p => NotificationPriorities.Rank(p) >= minimumRank)
                .ToList();

            return await OwnedBy(userId).AnyAsync(
                n => !n.IsRead && n.Priority != null && qualifying.Contains(n.Priority),
                cancellationToken);
        }

        public async Task<Notification?> GetForUserAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await OwnedBy(userId)
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        }

        public async Task<bool> MarkAsReadForUserAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default)
        {
            var notification = await GetForUserAsync(notificationId, userId, cancellationToken);
            if (notification == null) return false;

            // Idempotent: only move ReadAt the first time it becomes read.
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        public async Task<bool> DeleteForUserAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default)
        {
            var notification = await GetForUserAsync(notificationId, userId, cancellationToken);
            if (notification == null) return false;

            // Soft delete so the notification stays auditable and a later RLS/
            // history review can still see that it existed.
            notification.IsDeleted = true;
            notification.DeletedAt = DateTime.UtcNow;
            notification.DeletedDate = DateTime.UtcNow;
            notification.DeletedBy = userId;
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
