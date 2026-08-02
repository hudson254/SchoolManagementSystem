using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Additional properties required by Application handlers
        public string? Organization { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid TenantId { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Lecturer Lecturer { get; set; }
        public virtual ICollection<AuditLog> AuditLogs { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
    }
}
