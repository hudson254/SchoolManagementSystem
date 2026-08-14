using SMS.Certificates.Domain.Enums;

namespace SMS.Certificates.Domain.Interfaces;

/// <summary>
/// Service for checking student eligibility for certificate generation
/// </summary>
public interface ICertificateEligibilityService
{
    /// <summary>
    /// Check if a student is eligible for a certificate for a specific course offering
    /// </summary>
    /// <param name="studentId">Student ID</param>
    /// <param name="courseOfferingId">Course offering ID</param>
    /// <returns>Eligibility check result</returns>
    Task<EligibilityResult> CheckEligibilityAsync(Guid studentId, Guid courseOfferingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if all students in a course offering are eligible for certificates
    /// </summary>
    /// <param name="courseOfferingId">Course offering ID</param>
    /// <returns>List of eligibility results for all students</returns>
    Task<IEnumerable<EligibilityResult>> CheckBulkEligibilityAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of students eligible for certificates in a course offering
    /// </summary>
    /// <param name="courseOfferingId">Course offering ID</param>
    /// <returns>List of eligible student IDs</returns>
    Task<IEnumerable<Guid>> GetEligibleStudentsAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an eligibility check
/// </summary>
public class EligibilityResult
{
    /// <summary>
    /// Student ID
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Whether the student is eligible
    /// </summary>
    public bool IsEligible { get; set; }

    /// <summary>
    /// List of reasons why the student is not eligible (empty if eligible)
    /// </summary>
    public List<string> IneligibilityReasons { get; set; } = new();

    /// <summary>
    /// Final grade (if available)
    /// </summary>
    public string? FinalGrade { get; set; }

    /// <summary>
    /// Classification (if available)
    /// </summary>
    public string? Classification { get; set; }
}
