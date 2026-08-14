using Microsoft.Extensions.Logging;
using SMS.Certificates.Domain.Interfaces;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Certificates.Application.Services;

/// <summary>
/// Application service for certificate generation and management
/// </summary>
public class CertificateService
{
    private readonly ICertificateEligibilityService _eligibilityService;
    private readonly ICertificateNumberGenerator _numberGenerator;
    private readonly ICertificatePdfGenerator _pdfGenerator;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificateTemplateRepository _templateRepository;
    private readonly ICertificateAuditLogRepository _auditLogRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        ICertificateEligibilityService eligibilityService,
        ICertificateNumberGenerator numberGenerator,
        ICertificatePdfGenerator pdfGenerator,
        ICertificateRepository certificateRepository,
        ICertificateTemplateRepository templateRepository,
        ICertificateAuditLogRepository auditLogRepository,
        IFileStorageService fileStorageService,
        IStudentRepository studentRepository,
        ICourseOfferingRepository courseOfferingRepository,
        IQrCodeService qrCodeService,
        ILogger<CertificateService> logger)
    {
        _eligibilityService = eligibilityService;
        _numberGenerator = numberGenerator;
        _pdfGenerator = pdfGenerator;
        _certificateRepository = certificateRepository;
        _templateRepository = templateRepository;
        _auditLogRepository = auditLogRepository;
        _fileStorageService = fileStorageService;
        _studentRepository = studentRepository;
        _courseOfferingRepository = courseOfferingRepository;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    /// <summary>
    /// Generate certificate for a student
    /// </summary>
    public async Task<Certificate?> GenerateCertificateAsync(
        Guid studentId,
        Guid courseOfferingId,
        Guid? templateId = null,
        Guid? userId = null,
        string? userRole = null,
        string? ipAddress = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check eligibility
            var eligibility = await _eligibilityService.CheckEligibilityAsync(studentId, courseOfferingId, cancellationToken);
            if (!eligibility.IsEligible)
            {
                _logger.LogWarning("Student {StudentId} is not eligible for certificate in course offering {CourseOfferingId}: {Reasons}",
                    studentId, courseOfferingId, string.Join(", ", eligibility.IneligibilityReasons));
                return null;
            }

            // Get template
            var template = templateId.HasValue
                ? await _templateRepository.GetByIdAsync(templateId.Value, cancellationToken)
                : await _templateRepository.GetDefaultTemplateAsync(cancellationToken);

            if (template == null)
            {
                _logger.LogError("No certificate template found for certificate generation");
                throw new InvalidOperationException("No certificate template available");
            }

            // Get course offering details
            var courseOffering = await GetCourseOfferingDetailsAsync(courseOfferingId, cancellationToken);
            var student = await GetStudentDetailsAsync(studentId, cancellationToken);

            // Generate certificate number
            var year = DateTime.UtcNow.Year;
            var certificateNumber = await _numberGenerator.GenerateCertificateNumberAsync(courseOffering.OfferingCode, year, cancellationToken);

            // Generate verification token
            var verificationToken = GenerateSecureToken();

            // Generate QR code
            var qrCodeBytes = await _qrCodeService.GenerateQrCodeAsync(
                $"https://school.edu/verify?token={verificationToken}", 10);
            var qrCodePath = Path.Combine(
                Path.GetTempPath(),
                $"qr_{certificateNumber}_{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(qrCodePath, qrCodeBytes, cancellationToken);

            // Create certificate entity
            var certificate = new Certificate
            {
                CertificateNumber = certificateNumber,
                StudentId = studentId,
                CourseOfferingId = courseOfferingId,
                TemplateId = template.Id,
                TemplateVersion = template.Version,
                FinalGrade = eligibility.FinalGrade,
                Classification = eligibility.Classification,
                VerificationToken = verificationToken,
                Status = CertificateStatus.Issued,
                IssueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save certificate
            certificate = await _certificateRepository.AddAsync(certificate, cancellationToken);

            // Generate PDF
            var pdfRequest = new CertificateGenerationRequest
            {
                CertificateNumber = certificateNumber,
                StudentName = $"{student.FirstName} {student.LastName}",
                CourseName = courseOffering.Course.Name,
                CourseOffering = $"{courseOffering.StartDate:MMMM yyyy} – {courseOffering.EndDate:MMMM yyyy}",
                CourseCode = courseOffering.Course.Code,
                StartDate = courseOffering.StartDate,
                CompletionDate = courseOffering.EndDate,
                CourseDuration = CalculateDuration(courseOffering.StartDate, courseOffering.EndDate),
                FinalGrade = eligibility.FinalGrade,
                Classification = eligibility.Classification,
                DateAwarded = DateTime.UtcNow,
                Institution = "School Management System",
                TemplatePdfPath = template.FilePath,
                FieldMappings = template.FieldMappings,
                QrCodePath = qrCodePath,
                LogoPath = template.LogoPath,
                WatermarkPath = template.WatermarkPath,
                Signatures = new List<DigitalSignatureRequest>()
            };

            var pdfPath = await _pdfGenerator.GenerateCertificatePdfAsync(pdfRequest, cancellationToken);

            // Store PDF
            var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
            var storedPdfPath = await _fileStorageService.SaveFileAsync(
                pdfBytes,
                $"certificates/{certificateNumber}.pdf",
                "certificates");

            // Update certificate with PDF path
            certificate.PdfPath = storedPdfPath;
            certificate.QrCodePath = qrCodePath;
            await _certificateRepository.UpdateAsync(certificate, cancellationToken);

            // Log audit
            await LogCertificateActionAsync(
                certificate.Id,
                certificateNumber,
                "Generated",
                userId,
                userRole,
                ipAddress,
                sessionId,
                "Success",
                cancellationToken);

            // Send notification to student
            // TODO: Implement notification

            _logger.LogInformation("Generated certificate {CertificateNumber} for student {StudentId}",
                certificateNumber, studentId);

            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate for student {StudentId} in course offering {CourseOfferingId}",
                studentId, courseOfferingId);
            throw;
        }
    }

    /// <summary>
    /// Revoke a certificate
    /// </summary>
    public async Task RevokeCertificateAsync(
        Guid certificateId,
        string reason,
        Guid? userId = null,
        string? userRole = null,
        string? ipAddress = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = await _certificateRepository.GetByIdAsync(certificateId, cancellationToken);
            if (certificate == null)
            {
                throw new ArgumentException("Certificate not found", nameof(certificateId));
            }

            certificate.Status = CertificateStatus.Revoked;
            certificate.RevokedAt = DateTime.UtcNow;
            certificate.RevocationReason = reason;
            certificate.RevokedBy = userId;
            certificate.UpdatedAt = DateTime.UtcNow;

            await _certificateRepository.UpdateAsync(certificate, cancellationToken);

            await LogCertificateActionAsync(
                certificateId,
                certificate.CertificateNumber,
                "Revoked",
                userId,
                userRole,
                ipAddress,
                sessionId,
                "Success",
                cancellationToken);

            _logger.LogInformation("Revoked certificate {CertificateNumber}. Reason: {Reason}",
                certificate.CertificateNumber, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking certificate {CertificateId}", certificateId);
            throw;
        }
    }

    /// <summary>
    /// Regenerate a certificate (creates new version)
    /// </summary>
    public async Task<Certificate?> RegenerateCertificateAsync(
        Guid certificateId,
        string reason,
        Guid? userId = null,
        string? userRole = null,
        string? ipAddress = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existingCertificate = await _certificateRepository.GetByIdAsync(certificateId, cancellationToken);
            if (existingCertificate == null)
            {
                throw new ArgumentException("Certificate not found", nameof(certificateId));
            }

            // Mark old certificate as superseded
            existingCertificate.Status = CertificateStatus.Superseded;
            existingCertificate.UpdatedAt = DateTime.UtcNow;
            await _certificateRepository.UpdateAsync(existingCertificate, cancellationToken);

            // Generate new certificate
            var newCertificate = await GenerateCertificateAsync(
                existingCertificate.StudentId,
                existingCertificate.CourseOfferingId,
                existingCertificate.TemplateId,
                userId,
                userRole,
                ipAddress,
                sessionId,
                cancellationToken);

            if (newCertificate != null)
            {
                newCertificate.Version = existingCertificate.Version + 1;
                newCertificate.SupersedesCertificateId = existingCertificate.Id;
                await _certificateRepository.UpdateAsync(newCertificate, cancellationToken);

                await LogCertificateActionAsync(
                    newCertificate.Id,
                    newCertificate.CertificateNumber,
                    "Regenerated",
                    userId,
                    userRole,
                    ipAddress,
                    sessionId,
                    "Success",
                    cancellationToken);
            }

            return newCertificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating certificate {CertificateId}", certificateId);
            throw;
        }
    }

    #region Helper Methods

    private async Task LogCertificateActionAsync(
        Guid certificateId,
        string certificateNumber,
        string action,
        Guid? userId,
        string? userRole,
        string? ipAddress,
        string? sessionId,
        string outcome,
        CancellationToken cancellationToken)
    {
        try
        {
            var auditLog = new CertificateAuditLog
            {
                CertificateId = certificateId,
                CertificateNumber = certificateNumber,
                Action = action,
                UserId = userId,
                UserRole = userRole ?? "System",
                IpAddress = ipAddress,
                SessionId = sessionId,
                Outcome = outcome,
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging certificate action {Action} for certificate {CertificateNumber}",
                action, certificateNumber);
        }
    }

    private string GenerateSecureToken()
    {
        // Generate cryptographically secure random token
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string CalculateDuration(DateTime startDate, DateTime endDate)
    {
        var duration = endDate - startDate;
        var months = (int)Math.Round(duration.TotalDays / 30.0);
        return $"{months} month(s)";
    }

    private async Task<CourseOffering> GetCourseOfferingDetailsAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var courseOffering = await _courseOfferingRepository.GetByIdAsync(courseOfferingId, cancellationToken);
        if (courseOffering == null)
        {
            throw new ArgumentException($"Course offering {courseOfferingId} not found", nameof(courseOfferingId));
        }
        return courseOffering;
    }

    private async Task<Student> GetStudentDetailsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            throw new ArgumentException($"Student {studentId} not found", nameof(studentId));
        }
        return student;
    }

    #endregion
}
