using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// User entity extending ASP.NET Core Identity
    /// </summary>
    public class User : IdentityUser<Guid>, IBaseEntity
    {
        /// <summary>
        /// Tenant ID for multi-tenancy
        /// </summary>
        [Required]
        public Guid TenantId { get; set; }

        /// <summary>
        /// First name of the user
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the user
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Phone number (override Identity's phone number to include validation)
        /// </summary>
        [MaxLength(20)]
        public new string? PhoneNumber { get; set; }

        /// <summary>
        /// Organization the user belongs to
        /// </summary>
        [MaxLength(200)]
        public string? Organization { get; set; }

        /// <summary>
        /// Refresh token for JWT authentication
        /// </summary>
        [MaxLength(500)]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Refresh token expiry time
        /// </summary>
        public DateTime? RefreshTokenExpiryTime { get; set; }

        /// <summary>
        /// Whether the user account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether the user's email has been verified
        /// </summary>
        public bool IsEmailVerified { get; set; } = false;

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Last IP address used for login
        /// </summary>
        [MaxLength(45)]
        public string? LastLoginIP { get; set; }

        /// <summary>
        /// Whether the user has accepted terms and conditions
        /// </summary>
        public bool HasAcceptedTerms { get; set; } = false;

        /// <summary>
        /// Terms acceptance date
        /// </summary>
        public DateTime? TermsAcceptedDate { get; set; }

        // BaseEntity properties
        public string CreatedBy { get; set; } = "SYSTEM";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// Navigation property for tenant
        /// </summary>
        public virtual Tenant? Tenant { get; set; }

        /// <summary>
        /// Navigation property for student profile
        /// </summary>
        public virtual Student? Student { get; set; }

        /// <summary>
        /// Navigation property for lecturer profile
        /// </summary>
        public virtual Lecturer? Lecturer { get; set; }

        /// <summary>
        /// Navigation property for user roles
        /// </summary>
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        /// <summary>
        /// Navigation property for login history
        /// </summary>
        public virtual ICollection<LoginHistory> LoginHistory { get; set; } = new List<LoginHistory>();

        /// <summary>
        /// Navigation property for notifications
        /// </summary>
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        /// <summary>
        /// Navigation property for audit logs
        /// </summary>
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        /// <summary>
        /// Gets the full name of the user
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Updates the user's refresh token
        /// </summary>
        public void UpdateRefreshToken(string refreshToken, DateTime expiryTime)
        {
            RefreshToken = refreshToken;
            RefreshTokenExpiryTime = expiryTime;
        }

        /// <summary>
        /// Records a login attempt
        /// </summary>
        public void RecordLogin(string ipAddress)
        {
            LastLoginDate = DateTime.UtcNow;
            LastLoginIP = ipAddress;
        }
    }
}