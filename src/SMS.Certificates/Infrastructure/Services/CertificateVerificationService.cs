using Microsoft.Extensions.Logging;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;
using SMS.Domain.Interfaces;

namespace SMS.Certificates.Infrastructure.Services;

/// <summary>
/// Implementation of certificate verification service
/// </summary>
public class CertificateVerificationService : ICertificateVerificationService
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificateAuditLogRepository _auditLogRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly ILogger<CertificateVerificationService> _logger;

    public CertificateVerificationService(
        ICertificateRepository certificateRepository,
        ICertificateAuditLogRepository auditLogRepository,
        IStudentRepository studentRepository,
        ICourseOfferingRepository courseOfferingRepository,
        ILogger<CertificateVerificationService> logger)
    {
        _certificateRepository = certificateRepository;
        _auditLogRepository = auditLogRepository;
        _studentRepository = studentRepository;
        _courseOfferingRepository = courseOfferingRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<VerificationResult> VerifyByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = await _certificateRepository.GetByCertificateNumberAsync(certificateNumber, cancellationToken);

            // Log verification attempt
            await LogVerificationAttempt(null, certificateNumber, certificate != null, cancellationToken);

            if (certificate == null)
            {
                return new VerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Certificate not found or invalid."
                };
            }

            return await BuildVerificationResultAsync(certificate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate by number {CertificateNumber}", certificateNumber);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VerificationResult> VerifyByTokenAsync(string verificationToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = await _certificateRepository.GetByVerificationTokenAsync(verificationToken, cancellationToken);

            // Log verification attempt
            await LogVerificationAttempt(certificate?.Id, certificate?.CertificateNumber, certificate != null, cancellationToken);

            if (certificate == null)
            {
                return new VerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Certificate not found or invalid."
                };
            }

            return await BuildVerificationResultAsync(certificate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate by token");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VerificationResult> VerifyByQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default)
    {
        try
        {
            // QR code data could be either a verification URL or token
            // Extract token from URL if needed
            var verificationToken = ExtractTokenFromQrCode(qrCodeData);

            return await VerifyByTokenAsync(verificationToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate by QR code");
            return new VerificationResult
            {
                IsValid = false,
                ErrorMessage = "Certificate not found or invalid."
            };
        }
    }

    private async Task<VerificationResult> BuildVerificationResultAsync(Certificate certificate, CancellationToken cancellationToken)
    {
        // Verify certificate is still valid
        if (!certificate.IsValid())
        {
            var status = certificate.Status == CertificateStatus.Revoked ? "Revoked" :
                         certificate.Status == CertificateStatus.Expired ? "Expired" :
                         "Invalid";

            return new VerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Certificate not found or invalid."
            };
        }

        // Return certificate details (without sensitive information)
        return new VerificationResult
        {
            IsValid = true,
            Certificate = new CertificateDetails
            {
                CertificateNumber = certificate.CertificateNumber,
                StudentName = await GetStudentNameAsync(certificate.StudentId, cancellationToken),
                CourseName = await GetCourseNameAsync(certificate.CourseOfferingId, cancellationToken),
                CourseOffering = await GetCourseOfferingNameAsync(certificate.CourseOfferingId, cancellationToken),
                CompletionDate = certificate.IssueDate,
                IssueDate = certificate.IssueDate,
                Institution = "School Management System", // Make configurable
                FinalGrade = certificate.FinalGrade,
                Classification = certificate.Classification,
                Status = certificate.Status.ToString(),
                VerifiedAt = DateTime.UtcNow
            }
        };
    }

    private async Task LogVerificationAttempt(Guid? certificateId, string? certificateNumber, bool success, CancellationToken cancellationToken)
    {
        try
        {
            var auditLog = new CertificateAuditLog
            {
                CertificateId = certificateId,
                CertificateNumber = certificateNumber,
                Action = "Verified",
                UserId = null, // Public verification
                UserRole = "Public",
                IpAddress = null, // Will be populated by middleware if available
                SessionId = null,
                Outcome = success ? "Success" : "Failed",
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging verification attempt for certificate {CertificateNumber}", certificateNumber);
            // Don't throw - verification should continue even if logging fails
        }
    }

    private string ExtractTokenFromQrCode(string qrCodeData)
    {
        // If QR code contains a URL, extract the token from it
        // Example: https://school.edu/verify?token=abc123
        if (qrCodeData.Contains("token=", StringComparison.OrdinalIgnoreCase))
        {
            var parts = qrCodeData.Split("token=", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                // Remove any additional query parameters
                var token = parts[1].Split('&')[0];
                return Uri.UnescapeDataString(token);
            }
        }

        // If not a URL, assume it's the token itself
        return qrCodeData;
    }

    #region Helper Methods

    private async Task<string> GetStudentNameAsync(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(studentId, cancellationToken);
            if (student == null)
                return "Unknown Student";

            var name = $"{student.FirstName} {student.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? "Unknown Student" : name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student name for {StudentId}", studentId);
            return "Unknown Student";
        }
    }

    private async Task<string> GetCourseNameAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            var courseOffering = await _courseOfferingRepository.GetWithDetailsAsync(courseOfferingId, cancellationToken);
            if (courseOffering?.Course == null)
                return "Unknown Course";

            return courseOffering.Course.Name ?? "Unknown Course";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course name for offering {CourseOfferingId}", courseOfferingId);
            return "Unknown Course";
        }
    }

    private async Task<string> GetCourseOfferingNameAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            var courseOffering = await _courseOfferingRepository.GetWithDetailsAsync(courseOfferingId, cancellationToken);
            if (courseOffering == null)
                return "Unknown Course Offering";

            return courseOffering.OfferingCode ?? "Unknown Course Offering";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course offering name for {CourseOfferingId}", courseOfferingId);
            return "Unknown Course Offering";
        }
    }

    #endregion
}
