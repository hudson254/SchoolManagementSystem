using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;
using SMS.Domain.Interfaces;

namespace SMS.Certificates.Application.Services;

/// <summary>
/// Service for bulk certificate generation for historical students
/// </summary>
public class BulkCertificateService
{
    private readonly ICertificateEligibilityService _eligibilityService;
    private readonly CertificateService _certificateService;
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ILogger<BulkCertificateService> _logger;

    public BulkCertificateService(
        ICertificateEligibilityService eligibilityService,
        CertificateService certificateService,
        ICourseOfferingRepository courseOfferingRepository,
        IEnrollmentRepository enrollmentRepository,
        ICertificateRepository certificateRepository,
        ILogger<BulkCertificateService> logger)
    {
        _eligibilityService = eligibilityService;
        _certificateService = certificateService;
        _courseOfferingRepository = courseOfferingRepository;
        _enrollmentRepository = enrollmentRepository;
        _certificateRepository = certificateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generate certificates for all eligible students in a course offering
    /// </summary>
    public async Task<BulkGenerationResult> GenerateForCourseOfferingAsync(
        Guid courseOfferingId,
        Guid? userId = null,
        string? userRole = null,
        string? ipAddress = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkGenerationResult { CourseOfferingId = courseOfferingId };
        var courseOffering = await _courseOfferingRepository.GetByIdAsync(courseOfferingId, cancellationToken);

        if (courseOffering == null)
        {
            result.Errors.Add($"Course offering {courseOfferingId} not found");
            return result;
        }

        // Get all enrollments for this course offering
        var enrollments = await _enrollmentRepository.GetEnrollmentsByCourseAsync(courseOfferingId);
        var eligibleStudents = await _eligibilityService.GetEligibleStudentsAsync(courseOfferingId, cancellationToken);
        var eligibleSet = eligibleStudents.ToHashSet();

        foreach (var enrollment in enrollments)
        {
            try
            {
                // Check if certificate already exists
                var existingCertificates = await _certificateRepository.GetByStudentIdAsync(enrollment.StudentId, cancellationToken);
                var existing = existingCertificates.FirstOrDefault(c => c.CourseOfferingId == courseOfferingId);

                if (existing != null && (existing.Status == CertificateStatus.Issued || existing.Status == CertificateStatus.Superseded))
                {
                    result.Skipped.Add($"Student {enrollment.StudentId} already has a certificate for this course");
                    continue;
                }

                if (!eligibleSet.Contains(enrollment.StudentId))
                {
                    result.Skipped.Add($"Student {enrollment.StudentId} is not eligible for a certificate");
                    continue;
                }

                var certificate = await _certificateService.GenerateCertificateAsync(
                    enrollment.StudentId,
                    courseOfferingId,
                    userId: userId,
                    userRole: userRole,
                    ipAddress: ipAddress,
                    sessionId: sessionId,
                    cancellationToken: cancellationToken);

                if (certificate != null)
                {
                    result.Generated.Add(certificate.CertificateNumber);
                }
                else
                {
                    result.Errors.Add($"Failed to generate certificate for student {enrollment.StudentId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk generation error for student {StudentId} in offering {CourseOfferingId}",
                    enrollment.StudentId, courseOfferingId);
                result.Errors.Add($"Error generating for student {enrollment.StudentId}: {ex.Message}");
            }
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Generate certificates for all eligible students across all completed course offerings
    /// </summary>
    public async Task<BulkGenerationResult> GenerateForAllCompletedOfferingsAsync(
        Guid? userId = null,
        string? userRole = null,
        string? ipAddress = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkGenerationResult();
        var allOfferings = await _courseOfferingRepository.GetAllAsync(cancellationToken);
        var completedOfferings = allOfferings.Where(o => o.Status == SMS.Domain.Enums.CourseOfferingStatus.Completed);

        foreach (var offering in completedOfferings)
        {
            var offeringResult = await GenerateForCourseOfferingAsync(
                offering.Id, userId, userRole, ipAddress, sessionId, cancellationToken);

            result.Generated.AddRange(offeringResult.Generated);
            result.Skipped.AddRange(offeringResult.Skipped);
            result.Errors.AddRange(offeringResult.Errors);
            result.Warnings.AddRange(offeringResult.Warnings);
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }
}

/// <summary>
/// Result of a bulk certificate generation operation
/// </summary>
public class BulkGenerationResult
{
    public Guid? CourseOfferingId { get; set; }
    [JsonPropertyName("generatedCertificates")]
    public List<string> Generated { get; set; } = new();
    [JsonPropertyName("skippedStudents")]
    public List<string> Skipped { get; set; } = new();
    [JsonPropertyName("errorMessages")]
    public List<string> Errors { get; set; } = new();
    [JsonPropertyName("warningMessages")]
    public List<string> Warnings { get; set; } = new();
    public DateTime CompletedAt { get; set; }

    // Computed summary properties for API consumers
    public int totalProcessed => Generated.Count + Skipped.Count + Errors.Count + Warnings.Count;
    public int generated => Generated.Count;
    public int skipped => Skipped.Count;
    public int errors => Errors.Count;
    public int warnings => Warnings.Count;
    public string[] details => Generated
        .Select(c => $"Generated: {c}")
        .Concat(Skipped.Select(s => $"Skipped: {s}"))
        .Concat(Errors.Select(e => $"Error: {e}"))
        .Concat(Warnings.Select(w => $"Warning: {w}"))
        .ToArray();
}
