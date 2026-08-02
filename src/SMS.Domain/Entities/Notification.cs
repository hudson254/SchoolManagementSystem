using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class Notification : BaseEntity, ITenantAwareEntity
    {
        public string? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
    }
}
