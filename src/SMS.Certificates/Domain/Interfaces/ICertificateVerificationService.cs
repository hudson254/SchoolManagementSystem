namespace SMS.Certificates.Domain.Interfaces;

/// <summary>
/// Service for verifying certificate authenticity
/// </summary>
public interface ICertificateVerificationService
{
    /// <summary>
    /// Verify a certificate by certificate number
    /// </summary>
    /// <param name="certificateNumber">Certificate number to verify</param>
    /// <returns>Verification result</returns>
    Task<VerificationResult> VerifyByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify a certificate by verification token
    /// </summary>
    /// <param name="verificationToken">Verification token to verify</param>
    /// <returns>Verification result</returns>
    Task<VerificationResult> VerifyByTokenAsync(string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify a certificate by QR code data
    /// </summary>
    /// <param name="qrCodeData">QR code data to verify</param>
    /// <returns>Verification result</returns>
    Task<VerificationResult> VerifyByQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a certificate verification
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// Whether the certificate is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Certificate details (null if invalid)
    /// </summary>
    public CertificateDetails? Certificate { get; set; }

    /// <summary>
    /// Error message (if invalid)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Certificate details for verification response
/// </summary>
public class CertificateDetails
{
    /// <summary>
    /// Certificate number
    /// </summary>
    public string CertificateNumber { get; set; } = string.Empty;

    /// <summary>
    /// Student full name
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Course offering
    /// </summary>
    public string CourseOffering { get; set; } = string.Empty;

    /// <summary>
    /// Completion date
    /// </summary>
    public DateTime CompletionDate { get; set; }

    /// <summary>
    /// Issue date
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Institution name
    /// </summary>
    public string Institution { get; set; } = string.Empty;

    /// <summary>
    /// Final grade
    /// </summary>
    public string? FinalGrade { get; set; }

    /// <summary>
    /// Classification
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Certificate status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Verification timestamp
    /// </summary>
    public DateTime VerifiedAt { get; set; }
}
