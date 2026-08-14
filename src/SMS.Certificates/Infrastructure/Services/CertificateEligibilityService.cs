using Microsoft.Extensions.Logging;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Certificates.Infrastructure.Services;

/// <summary>
/// Implementation of certificate eligibility service
/// </summary>
public class CertificateEligibilityService : ICertificateEligibilityService
{
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificateRuleRepository _certificateRuleRepository;
    private readonly ILogger<CertificateEligibilityService> _logger;

    public CertificateEligibilityService(
        ICourseOfferingRepository courseOfferingRepository,
        IEnrollmentRepository enrollmentRepository,
        IGradeRepository gradeRepository,
        IAssignmentRepository assignmentRepository,
        ICertificateRepository certificateRepository,
        ICertificateRuleRepository certificateRuleRepository,
        ILogger<CertificateEligibilityService> logger)
    {
        _courseOfferingRepository = courseOfferingRepository;
        _enrollmentRepository = enrollmentRepository;
        _gradeRepository = gradeRepository;
        _assignmentRepository = assignmentRepository;
        _certificateRepository = certificateRepository;
        _certificateRuleRepository = certificateRuleRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<EligibilityResult> CheckEligibilityAsync(Guid studentId, Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new EligibilityResult
            {
                StudentId = studentId
            };

            // Check if course offering exists and is completed
            var courseOffering = await _courseOfferingRepository.GetByIdAsync(courseOfferingId, cancellationToken);
            if (courseOffering == null)
            {
                result.IsEligible = false;
                result.IneligibilityReasons.Add("Course offering not found");
                return result;
            }

            if (courseOffering.Status != CourseOfferingStatus.Completed)
            {
                result.IsEligible = false;
                result.IneligibilityReasons.Add("Course has not been marked as completed");
                return result;
            }

            // Check if student is enrolled
            var enrollment = await _enrollmentRepository.GetEnrollmentAsync(studentId, courseOfferingId, cancellationToken);
            if (enrollment == null)
            {
                result.IsEligible = false;
                result.IneligibilityReasons.Add("Student is not enrolled in this course offering");
                return result;
            }

            // Check if certificate already exists
            var existingCertificates = await _certificateRepository.GetByStudentIdAsync(studentId, cancellationToken);
            var existingCertificate = existingCertificates.FirstOrDefault(c => c.CourseOfferingId == courseOfferingId);
            if (existingCertificate != null && existingCertificate.Status == CertificateStatus.Issued)
            {
                result.IsEligible = false;
                result.IneligibilityReasons.Add("Certificate has already been issued for this course");
                return result;
            }

            // Check if final grade exists
            var grades = await _gradeRepository.GetStudentGradesAsync(studentId, cancellationToken);
            if (!grades.Any())
            {
                result.IsEligible = false;
                result.IneligibilityReasons.Add("No grades recorded for student");
                return result;
            }

            // Get final grade (assuming there's a final grade calculation)
            var finalGrade = grades.FirstOrDefault(g => !string.IsNullOrEmpty(g.LetterGrade));
            if (finalGrade == null)
            {
                result.IsEligible = false;
                result.IneligibilityReasons.Add("Final grade has not been calculated");
                return result;
            }

            var gradeValue = finalGrade.LetterGrade ?? finalGrade.GradeValue;
            result.FinalGrade = gradeValue;
            result.Classification = CalculateClassification(gradeValue);

            // Load the active configurable certificate rule
            var activeRule = await _certificateRuleRepository.GetActiveRuleAsync(cancellationToken);

            // Check if grade meets minimum requirement (configurable via CertificateRule)
            if (!IsPassingGrade(gradeValue, activeRule))
            {
                var required = activeRule?.MinimumPassingGradeLetter ?? "D";
                result.IsEligible = false;
                result.IneligibilityReasons.Add($"Final grade {gradeValue} does not meet minimum passing requirement (minimum {required})");
                return result;
            }

            // Check if all mandatory assessments are submitted (if configured)
            if (activeRule?.RequireAllMandatoryAssessments == true)
            {
                var hasIncompleteMandatory = await HasIncompleteMandatoryAssessmentsAsync(studentId, courseOfferingId, cancellationToken);
                if (hasIncompleteMandatory)
                {
                    result.IsEligible = false;
                    result.IneligibilityReasons.Add("Not all mandatory assessments have been completed");
                    return result;
                }
            }

            // Check for outstanding incomplete items (if configured)
            if (activeRule?.RequireNoOutstandingIncomplete == true)
            {
                var hasIncomplete = await HasOutstandingIncompleteAsync(studentId, courseOfferingId, cancellationToken);
                if (hasIncomplete)
                {
                    result.IsEligible = false;
                    result.IneligibilityReasons.Add("Student has outstanding incomplete academic requirements");
                    return result;
                }
            }

            // All checks passed
            result.IsEligible = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking eligibility for student {StudentId} in course offering {CourseOfferingId}",
                studentId, courseOfferingId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EligibilityResult>> CheckBulkEligibilityAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var enrollments = await _enrollmentRepository.GetEnrollmentsByCourseAsync(courseOfferingId);
            var results = new List<EligibilityResult>();

            foreach (var enrollment in enrollments)
            {
                var result = await CheckEligibilityAsync(enrollment.StudentId, courseOfferingId, cancellationToken);
                results.Add(result);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bulk eligibility for course offering {CourseOfferingId}", courseOfferingId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> GetEligibleStudentsAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await CheckBulkEligibilityAsync(courseOfferingId, cancellationToken);
            return results.Where(r => r.IsEligible).Select(r => r.StudentId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eligible students for course offering {CourseOfferingId}", courseOfferingId);
            throw;
        }
    }

    #region Helper Methods

    private bool IsPassingGrade(string? grade, CertificateRule? rule)
    {
        if (string.IsNullOrWhiteSpace(grade))
            return false;

        var normalized = grade.ToUpper();

        // If a minimum passing grade letter is configured, use it
        if (!string.IsNullOrWhiteSpace(rule?.MinimumPassingGradeLetter))
        {
            var minGrade = rule.MinimumPassingGradeLetter.ToUpper();
            var gradeOrder = new Dictionary<string, int>
            {
                ["A"] = 5,
                ["B"] = 4,
                ["C"] = 3,
                ["D"] = 2,
                ["P"] = 2,
                ["F"] = 1
            };

            if (gradeOrder.TryGetValue(normalized, out var gradeVal) &&
                gradeOrder.TryGetValue(minGrade, out var minVal))
            {
                return gradeVal >= minVal;
            }

            // Fall back to direct comparison for non-standard grades
            return string.Compare(normalized, minGrade, StringComparison.Ordinal) >= 0;
        }

        // Default passing grades (A, B, C, D, P are passing, F is failing)
        return normalized switch
        {
            "A" => true,
            "B" => true,
            "C" => true,
            "D" => true,
            "P" => true,
            _ => false
        };
    }

    private async Task<bool> HasIncompleteMandatoryAssessmentsAsync(Guid studentId, Guid courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            // Get assignments associated with the student
            var assignments = await _assignmentRepository.GetAssignmentsByStudentAsync(studentId);
            var activeAssignments = assignments.Where(a => a.IsActive).ToList();

            if (!activeAssignments.Any())
                return false;

            // Check if the student has submissions for all active assignments
            foreach (var assignment in activeAssignments)
            {
                var hasSubmissions = await _assignmentRepository.HasSubmissionsAsync(assignment.Id, cancellationToken);
                if (!hasSubmissions)
                {
                    return true; // Missing mandatory submission
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking mandatory assessments for student {StudentId} in offering {CourseOfferingId}", studentId, courseOfferingId);
            return false;
        }
    }

    private async Task<bool> HasOutstandingIncompleteAsync(Guid studentId, Guid courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            // Check if student has any incomplete grades
            var grades = await _gradeRepository.GetStudentGradesAsync(studentId, cancellationToken);
            if (!grades.Any())
                return true;

            // Check if there are any active assignments for the student with no submission
            var assignments = await _assignmentRepository.GetAssignmentsByStudentAsync(studentId);
            var activeAssignments = assignments.Where(a => a.IsActive).ToList();

            // If there are active assignments and student has no grades, they have outstanding work
            return activeAssignments.Any() && !grades.Any(g => !string.IsNullOrEmpty(g.LetterGrade));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking outstanding incomplete for student {StudentId} in offering {CourseOfferingId}", studentId, courseOfferingId);
            return false;
        }
    }

    private string? CalculateClassification(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade))
            return null;

        return grade.ToUpper() switch
        {
            "A" => "Distinction",
            "B" => "Merit",
            "C" => "Pass",
            "D" => "Pass",
            "P" => "Pass",
            _ => null
        };
    }

    #endregion
}
