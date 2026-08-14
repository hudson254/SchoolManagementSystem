namespace SMS.Certificates.Domain.Entities;

/// <summary>
/// Represents an audit log entry for certificate operations
/// </summary>
public class CertificateAuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the certificate (null for verification attempts)
    /// </summary>
    public Guid? CertificateId { get; set; }

    /// <summary>
    /// Certificate number (denormalized for easier querying)
    /// </summary>
    public string? CertificateNumber { get; set; }

    /// <summary>
    /// Action performed (e.g., Generated, Viewed, Downloaded, Verified, Revoked)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// User who performed the action (null for public verification)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Role of the user (e.g., Student, Administrator, Coordinator)
    /// </summary>
    public string? UserRole { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Additional details in JSON format
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Outcome of the action (Success, Failed)
    /// </summary>
    public string Outcome { get; set; } = "Success";

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Navigation property to the certificate
    /// </summary>
    public virtual Certificate? Certificate { get; set; }
}
