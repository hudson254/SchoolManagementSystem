using SMS.Certificates.Domain.Enums;

namespace SMS.Certificates.Domain.Entities;

/// <summary>
/// Represents a certificate issued to a student
/// </summary>
public class Certificate
{
    /// <summary>
    /// Unique identifier for the certificate
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable certificate number (e.g., SMS-2026-DIT-000001)
    /// </summary>
    public string CertificateNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the student
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Foreign key to the course offering
    /// </summary>
    public Guid CourseOfferingId { get; set; }

    /// <summary>
    /// Foreign key to the certificate template used
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Version of the template used when this certificate was generated
    /// </summary>
    public string TemplateVersion { get; set; } = "1.0";

    /// <summary>
    /// Current status of the certificate
    /// </summary>
    public CertificateStatus Status { get; set; } = CertificateStatus.Pending;

    /// <summary>
    /// Type of certificate
    /// </summary>
    public CertificateType Type { get; set; }

    /// <summary>
    /// Student's final grade
    /// </summary>
    public string? FinalGrade { get; set; }

    /// <summary>
    /// Award classification (e.g., Distinction, Merit, Pass)
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Date the certificate was issued
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Date the certificate expires (null for non-expiring certificates)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Cryptographically secure verification token
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// Public verification URL
    /// </summary>
    public string VerificationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path to the generated QR code image
    /// </summary>
    public string? QrCodePath { get; set; }

    /// <summary>
    /// Path to the generated PDF certificate
    /// </summary>
    public string PdfPath { get; set; } = string.Empty;

    /// <summary>
    /// Cryptographic hash of the certificate for integrity verification
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Certificate version number (increments on regeneration)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Parent certificate ID if this is a regenerated version
    /// </summary>
    public Guid? ParentCertificateId { get; set; }

    /// <summary>
    /// ID of the certificate this certificate supersedes (for regenerated versions)
    /// </summary>
    public Guid? SupersedesCertificateId { get; set; }

    /// <summary>
    /// Reason for revocation (if applicable)
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Date the certificate was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// User who revoked the certificate
    /// </summary>
    public Guid? RevokedBy { get; set; }

    /// <summary>
    /// Timestamp when the certificate was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the certificate
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the certificate was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who last updated the certificate
    /// </summary>
    public Guid UpdatedBy { get; set; }

    /// <summary>
    /// Navigation property to the parent certificate (if regenerated)
    /// </summary>
    public virtual Certificate? ParentCertificate { get; set; }

    /// <summary>
    /// Navigation property to child certificates (regenerated versions)
    /// </summary>
    public virtual ICollection<Certificate> ChildCertificates { get; set; } = new List<Certificate>();

    /// <summary>
    /// Navigation property to the certificate template used
    /// </summary>
    public virtual CertificateTemplate? Template { get; set; }

    /// <summary>
    /// Checks if the certificate is currently valid
    /// </summary>
    public bool IsValid()
    {
        if (Status != CertificateStatus.Issued)
            return false;

        if (ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Revokes the certificate
    /// </summary>
    public void Revoke(string reason, Guid revokedBy)
    {
        if (Status == CertificateStatus.Revoked)
            throw new InvalidOperationException("Certificate is already revoked");

        Status = CertificateStatus.Revoked;
        RevocationReason = reason;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Supersedes this certificate with a newer version
    /// </summary>
    public void Supersede()
    {
        Status = CertificateStatus.Superseded;
        UpdatedAt = DateTime.UtcNow;
    }
}
