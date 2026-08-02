using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class LoginHistory : BaseEntity, ITenantAwareEntity
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime? LogoutTime { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccessful { get; set; } = true;
        public string? FailureReason { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
    }
}
