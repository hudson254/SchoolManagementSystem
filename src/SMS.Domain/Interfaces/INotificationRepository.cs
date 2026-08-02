using SMS.Domain.Entities;
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
    }
}
