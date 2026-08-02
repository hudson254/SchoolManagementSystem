using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context, ILogger<NotificationRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, cancellationToken);
        }

        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        {
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
            var unread = await _dbSet.Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var notification in unread)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(string type, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(n => n.Type == type && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync(cancellationToken);
        }
    }
}
