using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents an immutable audit log record capturing all sensitive user actions.
    /// Audit records are append-only and cannot be modified or deleted.
    /// </summary>
    public class AuditLog : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// Gets or sets the user ID who performed the action.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the username of the user who performed the action.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the user's role at the time of the action.
        /// </summary>
        public string? UserRole { get; set; }

        /// <summary>
        /// Gets or sets the action performed (e.g., "Login", "UserCreated", "GradeModified").
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the entity affected (e.g., "User", "Grade", "Enrollment").
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the specific record affected.
        /// </summary>
        public string? EntityId { get; set; }

        /// <summary>
        /// Gets or sets the previous values before the change (JSON serialized).
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// Gets or sets the new values after the change (JSON serialized).
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// Gets or sets the source IP address of the request.
        /// </summary>
        public string? IPAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent / device information.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for request tracing.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets whether the action was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the failure reason if the action failed.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the audit event (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional details about the action.
        /// </summary>
        public string? Details { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
    }
}
