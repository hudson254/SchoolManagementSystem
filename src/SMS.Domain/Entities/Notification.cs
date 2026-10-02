using SMS.Domain.Common;
using SMS.Domain.Notifications;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// A persistent, user-scoped in-app notification.
    /// <para>
    /// Every notification is owned by exactly one <see cref="UserId"/>. Tenant
    /// scoping is inherited from <see cref="BaseEntity.TenantId"/> (stamped by
    /// <c>ApplicationDbContext.SaveChangesAsync</c> and policed by PostgreSQL RLS),
    /// so a row is only ever visible inside the tenant that produced it.
    /// </para>
    /// <para>
    /// <see cref="ActionUrl"/> is an APPLICATION-RELATIVE path only (for example
    /// <c>/assignments/&lt;guid&gt;</c>). It is validated on write by
    /// <see cref="NotificationCatalog.NormalizeActionUrl"/> so a notification can
    /// never carry an absolute URL, a scheme, or a protocol-relative redirect. It is
    /// a navigation hint ONLY - the target page and its API calls remain the
    /// authorization boundary, exactly as required for the rest of the system.
    /// </para>
    /// </summary>
    public class Notification : BaseEntity, ITenantAwareEntity
    {
        /// <summary>Owning user. Null is only ever used by seed/system rows.</summary>
        public string? UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Notification type, one of <see cref="NotificationTypes"/>. Free-form for
        /// backwards compatibility with pre-existing rows, but always written from
        /// the catalogue by new code.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>Identifier of the object this notification refers to, if any.</summary>
        public string? ReferenceId { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// One of <see cref="NotificationPriorities"/>. Drives the visual treatment
        /// in the notification centre and the ordering of the unread badge.
        /// </summary>
        public string Priority { get; set; } = NotificationPriorities.Normal;

        /// <summary>
        /// Optional application-relative navigation target (e.g. <c>/assignments/{id}</c>).
        /// See the class remarks for the security contract.
        /// </summary>
        public string? ActionUrl { get; set; }

        /// <summary>Optional expiry. Expired notifications stay in history but are flagged.</summary>
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
    }
}
