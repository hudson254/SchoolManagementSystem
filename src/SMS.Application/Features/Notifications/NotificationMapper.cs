using SMS.Domain.Entities;
using SMS.Domain.Notifications;

namespace SMS.Application.Features.Notifications
{
    /// <summary>
    /// Single entity → DTO projection for notifications.
    /// <para>
    /// Every notification read path goes through here so the wire contract (the
    /// CreatedAt/CreatedDate and ReadAt/ReadDate aliases, the priority normalisation
    /// and the computed IsExpired flag) cannot drift between the list endpoint, the
    /// get-by-id endpoint and the SignalR push.
    /// </para>
    /// </summary>
    public static class NotificationMapper
    {
        /// <summary>Maps one persisted notification to its API representation.</summary>
        public static NotificationDto ToDto(Notification notification)
        {
            // Prefer CreatedDate (what the UI has always read) but fall back to CreatedAt
            // for rows written before the two were populated together.
            var created = notification.CreatedDate ?? notification.CreatedAt;

            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title ?? string.Empty,
                Message = notification.Message ?? string.Empty,
                Type = NotificationTypes.Normalize(notification.Type),
                IsRead = notification.IsRead,
                CreatedAt = created,
                CreatedDate = created,
                ReadAt = notification.ReadAt,
                ReadDate = notification.ReadAt,
                Priority = NotificationPriorities.Normalize(notification.Priority),
                // Re-sanitised on the way OUT as well. The column is sanitised on write,
                // but doing it again on read means a row written by any other path (a
                // seed, a manual fix, an older release) still cannot smuggle an
                // absolute URL out to the browser.
                ActionUrl = NotificationCatalog.NormalizeActionUrl(notification.ActionUrl),
                ExpiresAt = notification.ExpiresAt,
                IsExpired = notification.ExpiresAt.HasValue && notification.ExpiresAt.Value <= DateTime.UtcNow,
                ReferenceId = notification.ReferenceId,
                SenderId = null,
                SenderName = string.Empty
            };
        }

        /// <summary>Maps a sequence of notifications.</summary>
        public static List<NotificationDto> ToDtoList(IEnumerable<Notification> notifications)
        {
            var list = new List<NotificationDto>();
            if (notifications == null) return list;

            foreach (var n in notifications)
            {
                if (n != null) list.Add(ToDto(n));
            }

            return list;
        }
    }
}