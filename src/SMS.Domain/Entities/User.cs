using Microsoft.AspNetCore.Identity;
using SMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class User : IdentityUser, ITenantAwareEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Title { get; set; }
        public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        public string DisplayName => BuildDisplayName();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        private string BuildDisplayName()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Title))
                parts.Add(Title);
            parts.Add(FirstName);
            if (!string.IsNullOrWhiteSpace(MiddleName))
                parts.Add(MiddleName);
            parts.Add(LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }

        public DateTime? LastLoginAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? RefreshToken { get; set; }
        public string? RefreshTokenHash { get; set; }
        public Guid? RefreshTokenFamilyId { get; set; }
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
