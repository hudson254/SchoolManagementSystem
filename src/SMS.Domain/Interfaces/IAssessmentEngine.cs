using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Centralized Assessment Engine - the single authority for all grading operations.
    /// No module should independently calculate grades.
    /// </summary>
    public interface IAssessmentEngine
    {
        // Weight Validation
        Task<bool> ValidateWeightTotalAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default);
        Task<decimal> GetTotalWeightAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default);

        // Mark Calculation
        Task<decimal> CalculateWeightedScoreAsync(decimal mark, decimal maxScore, decimal weight);
        Task<StudentAssessmentMark> CalculateAndSaveMarkAsync(Guid assessmentId, Guid studentId, decimal mark, CancellationToken ct = default);

/// <summary>
        /// Persists a draft mark (visible only to staff) without triggering result
        /// recalculations. The same mark-range and duplicate-prevention rules apply.
        /// </summary>
        Task<StudentAssessmentMark> SaveDraftMarkAsync(Guid assessmentId, Guid studentId, decimal mark, string? feedback = null, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing mark. For non-draft (published/moderation-visible) marks a
        /// reason is mandatory. Records an immutable GradeChangeHistory entry, recalculates
        /// the unit result, grade and certificate eligibility, and audits the change.
        /// </summary>
        Task<StudentAssessmentMark> UpdateMarkAsync(Guid markId, decimal newScore, string? reason = null, string? feedback = null, bool isDraft = false, string? changedBy = null, CancellationToken ct = default);
        // Final Score Calculation
        Task<UnitResult> CalculateFinalUnitScoreAsync(Guid studentId, Guid unitId, Guid? courseOfferingId, CancellationToken ct = default);
        Task<decimal> CalculateFinalPercentageAsync(IEnumerable<StudentAssessmentMark> marks, IEnumerable<Assessment> assessments);

        // Grade Assignment
        Task<(string GradeLetter, string Description, decimal? GpaPoints)> AssignGradeAsync(decimal percentage, CancellationToken ct = default);
        Task<GradeBand?> FindGradeBandAsync(decimal percentage, CancellationToken ct = default);

        // Certificate Eligibility
        Task<StudentCertificateEligibility> EvaluateCertificateEligibilityAsync(Guid studentId, CancellationToken ct = default);

        // Publication
        Task PublishUnitResultsAsync(Guid unitId, Guid? courseOfferingId, string publishedBy, CancellationToken ct = default);
        Task ApproveUnitResultsAsync(Guid unitId, Guid? courseOfferingId, string approvedBy, CancellationToken ct = default);

        // Recalculation
        Task RecalculateAfterGradeChangeAsync(Guid studentId, Guid unitId, CancellationToken ct = default);

        // Bulk Operations
        Task<IEnumerable<UnitResult>> CalculateAllUnitResultsAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default);
    }
}
