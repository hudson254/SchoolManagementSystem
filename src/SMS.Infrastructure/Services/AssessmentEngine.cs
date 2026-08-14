using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Centralized Assessment Engine - the single authority for all grading operations.
    /// Implements weight validation, mark calculation, final score computation,
    /// grade assignment, certificate eligibility, and publication workflows.
    /// </summary>
    public class AssessmentEngine : IAssessmentEngine
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IGradingScaleRepository _gradingScaleRepository;
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;
        private readonly IStudentCertificateEligibilityRepository _eligibilityRepository;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssessmentEngine> _logger;

        private const decimal RequiredWeightTotal = 100m;

        public AssessmentEngine(
            ApplicationDbContext dbContext,
            IAssessmentRepository assessmentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IGradingScaleRepository gradingScaleRepository,
            IUnitResultRepository unitResultRepository,
            ICertificateRuleRepository certificateRuleRepository,
            IStudentCertificateEligibilityRepository eligibilityRepository,
            IAuditService auditService,
            ILogger<AssessmentEngine> logger)
        {
            _dbContext = dbContext;
            _assessmentRepository = assessmentRepository;
            _markRepository = markRepository;
            _gradingScaleRepository = gradingScaleRepository;
            _unitResultRepository = unitResultRepository;
            _certificateRuleRepository = certificateRuleRepository;
            _eligibilityRepository = eligibilityRepository;
            _auditService = auditService;
            _logger = logger;
        }

        // ============================================================
        // WEIGHT VALIDATION
        // ============================================================

        public async Task<decimal> GetTotalWeightAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
            => await _assessmentRepository.GetTotalWeightForUnitAsync(unitId, courseOfferingId, ct);

        public async Task<bool> ValidateWeightTotalAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var total = await GetTotalWeightAsync(unitId, courseOfferingId, ct);
            return Math.Abs(total - RequiredWeightTotal) < 0.01m;
        }

        // ============================================================
        // MARK CALCULATION
        // ============================================================

        public Task<decimal> CalculateWeightedScoreAsync(decimal mark, decimal maxScore, decimal weight)
        {
            if (maxScore <= 0) throw new ArgumentException("MaxScore must be greater than zero", nameof(maxScore));
            if (weight < 0) throw new ArgumentException("Weight cannot be negative", nameof(weight));

            var percentage = (mark / maxScore) * 100m;
            var weightedScore = (percentage * weight) / 100m;
            return Task.FromResult(Math.Round(weightedScore, 2, MidpointRounding.AwayFromZero));
        }

        public async Task<StudentAssessmentMark> CalculateAndSaveMarkAsync(
            Guid assessmentId, Guid studentId, decimal mark, CancellationToken ct = default)
        {
            var assessment = await _assessmentRepository.GetByIdAsync(assessmentId, ct)
                ?? throw new InvalidOperationException($"Assessment {assessmentId} not found");

            if (mark < 0 || mark > assessment.MaxScore)
                throw new ArgumentOutOfRangeException(nameof(mark), $"Mark must be between 0 and {assessment.MaxScore}");

            // Prevent duplicate grading
            var existing = await _markRepository.GetByAssessmentAndStudentAsync(assessmentId, studentId, ct);
            if (existing != null && !existing.IsDraft)
                throw new InvalidOperationException($"Mark already exists for assessment {assessmentId} and student {studentId}");

            var percentage = (mark / assessment.MaxScore) * 100m;
            var weightedScore = await CalculateWeightedScoreAsync(mark, assessment.MaxScore, assessment.Weight);

            var markEntity = existing ?? new StudentAssessmentMark
            {
                AssessmentId = assessmentId,
                StudentId = studentId,
                CourseOfferingId = assessment.CourseOfferingId
            };

            markEntity.Mark = mark;
            markEntity.Percentage = Math.Round(percentage, 2);
            markEntity.WeightedScore = weightedScore;
            markEntity.IsDraft = false;
            markEntity.GradedDate = DateTime.UtcNow;
            markEntity.EntrySource = MarkEntrySource.ManualEntry;

            if (existing == null)
                await _markRepository.AddAsync(markEntity, ct);
            else
                await _markRepository.UpdateAsync(markEntity, ct);

            await _dbContext.SaveChangesAsync(ct);
            await _auditService.LogDataChangeAsync("StudentAssessmentMark", markEntity.Id.ToString(), "MarksEntered",
                $"Assessment: {assessmentId}, Student: {studentId}, Mark: {mark}, Percentage: {markEntity.Percentage}, WeightedScore: {markEntity.WeightedScore}");

            return markEntity;
        }

        // ============================================================
        // FINAL SCORE CALCULATION
        // ============================================================

        public async Task<decimal> CalculateFinalPercentageAsync(
            IEnumerable<StudentAssessmentMark> marks, IEnumerable<Assessment> assessments)
        {
            var assessmentDict = assessments.ToDictionary(a => a.Id);
            var totalWeightedScore = 0m;
            var totalWeight = 0m;

            foreach (var mark in marks)
            {
                if (mark.IsExempt) continue;
                if (!assessmentDict.TryGetValue(mark.AssessmentId, out var assessment)) continue;

                var percentage = (mark.Mark / assessment.MaxScore) * 100m;
                var weighted = (percentage * assessment.Weight) / 100m;
                totalWeightedScore += weighted;
                totalWeight += assessment.Weight;
            }

            if (totalWeight == 0) return 0m;

            // Normalize to 100% based on completed assessments
            return Math.Round((totalWeightedScore / totalWeight) * 100m, 2, MidpointRounding.AwayFromZero);
        }

        public async Task<UnitResult> CalculateFinalUnitScoreAsync(
            Guid studentId, Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var assessments = (await _assessmentRepository.GetByUnitAsync(unitId, ct)).Where(a => a.IsActive).ToList();
            var marks = (await _markRepository.GetByUnitAndStudentAsync(unitId, studentId, ct)).Where(m => !m.IsDraft).ToList();

            var finalPercentage = await CalculateFinalPercentageAsync(marks, assessments);
            var gradingScale = await _gradingScaleRepository.GetActiveVersionAsync(ct);
            var band = gradingScale?.Bands?
                .OrderByDescending(b => b.MinPercentage)
                .FirstOrDefault(b => finalPercentage >= b.MinPercentage && finalPercentage <= b.MaxPercentage);

            var unitResult = await _unitResultRepository.GetByStudentAndUnitAsync(studentId, unitId, ct)
                ?? new UnitResult { StudentId = studentId, UnitId = unitId, CourseOfferingId = courseOfferingId };

            unitResult.FinalPercentage = finalPercentage;
            unitResult.GradeLetter = band?.GradeLetter ?? "F";
            unitResult.GradeDescription = band?.Description ?? "Fail";
            unitResult.GpaPoints = band?.GpaPoints;
            unitResult.GradingScaleVersionId = gradingScale?.Id;
            unitResult.IsRecalculated = true;
            unitResult.LastCalculatedDate = DateTime.UtcNow;

            if (unitResult.Id == Guid.Empty)
                await _unitResultRepository.AddAsync(unitResult, ct);
            else
                await _unitResultRepository.UpdateAsync(unitResult, ct);

            await _dbContext.SaveChangesAsync(ct);
            await _auditService.LogDataChangeAsync("UnitResult", unitResult.Id.ToString(), "GradeRecalculated",
                $"Student: {studentId}, Unit: {unitId}, Final: {finalPercentage}%, Grade: {unitResult.GradeLetter}");

            return unitResult;
        }

        // ============================================================
        // GRADE ASSIGNMENT
        // ============================================================

        public async Task<GradeBand?> FindGradeBandAsync(decimal percentage, CancellationToken ct = default)
        {
            var scale = await _gradingScaleRepository.GetActiveVersionAsync(ct);
            return scale?.Bands?
                .OrderByDescending(b => b.MinPercentage)
                .FirstOrDefault(b => percentage >= b.MinPercentage && percentage <= b.MaxPercentage);
        }

        public async Task<(string GradeLetter, string Description, decimal? GpaPoints)> AssignGradeAsync(
            decimal percentage, CancellationToken ct = default)
        {
            var band = await FindGradeBandAsync(percentage, ct);
            return band != null
                ? (band.GradeLetter, band.Description, band.GpaPoints)
                : ("F", "Fail", 0m);
        }

        // ============================================================
        // CERTIFICATE ELIGIBILITY
        // ============================================================

        public async Task<StudentCertificateEligibility> EvaluateCertificateEligibilityAsync(
            Guid studentId, CancellationToken ct = default)
        {
            var rule = await _certificateRuleRepository.GetActiveRuleAsync(ct);
            var unitResults = (await _unitResultRepository.GetByStudentAsync(studentId, ct)).Where(r => r.IsPublished).ToList();
            var eligibility = await _eligibilityRepository.GetByStudentAsync(studentId, ct)
                ?? new StudentCertificateEligibility { StudentId = studentId };

            var overallPercentage = unitResults.Count > 0 ? unitResults.Average(r => r.FinalPercentage) : 0m;
            var hasOutstandingIncomplete = unitResults.Any(r => !r.IsPublished);
            var hasFailedRequiredUnits = unitResults.Any(r => r.FinalPercentage < (rule?.MinimumPassingPercentage ?? 50m));

            var isEligible = unitResults.Count > 0
                && (!rule?.RequireAllRequiredUnits ?? true || !hasFailedRequiredUnits)
                && (!rule?.RequireNoOutstandingIncomplete ?? true || !hasOutstandingIncomplete)
                && overallPercentage >= (rule?.MinimumPassingPercentage ?? 50m);

            eligibility.OverallPercentage = Math.Round(overallPercentage, 2);
            eligibility.OverallGradeLetter = unitResults.Count > 0
                ? (await AssignGradeAsync(overallPercentage, ct)).GradeLetter : "N/A";
            eligibility.HasOutstandingIncomplete = hasOutstandingIncomplete;
            eligibility.HasFailedRequiredUnits = hasFailedRequiredUnits;
            eligibility.Status = isEligible
                ? CertificateEligibilityStatus.Eligible
                : unitResults.Count == 0
                    ? CertificateEligibilityStatus.PendingCompletion
                    : CertificateEligibilityStatus.NotEligible;
            eligibility.EvaluatedDate = DateTime.UtcNow;
            eligibility.CertificateRuleId = rule?.Id;

            if (eligibility.Id == Guid.Empty)
                await _eligibilityRepository.AddAsync(eligibility, ct);
            else
                await _eligibilityRepository.UpdateAsync(eligibility, ct);

            await _dbContext.SaveChangesAsync(ct);
            await _auditService.LogDataChangeAsync("StudentCertificateEligibility", eligibility.Id.ToString(), "EligibilityUpdated",
                $"Student: {studentId}, Status: {eligibility.Status}, Overall: {eligibility.OverallPercentage}%");

            return eligibility;
        }

        // ============================================================
        // PUBLICATION WORKFLOW
        // ============================================================

        public async Task ApproveUnitResultsAsync(Guid unitId, Guid? courseOfferingId, string approvedBy, CancellationToken ct = default)
        {
            var results = (await _unitResultRepository.GetByUnitAsync(unitId, ct))
                .Where(r => r.PublicationStatus == ResultPublicationStatus.PendingReview)
                .ToList();

            foreach (var result in results)
            {
                result.PublicationStatus = ResultPublicationStatus.Approved;
                result.IsApproved = true;
                result.ApprovedDate = DateTime.UtcNow;
                result.ApprovedBy = approvedBy;
                await _unitResultRepository.UpdateAsync(result, ct);
            }

            await _dbContext.SaveChangesAsync(ct);
            await _auditService.LogActivityAsync("ResultsApproved", "UnitResult", unitId.ToString(),
                $"Unit: {unitId}, Approved by: {approvedBy}, Count: {results.Count}");
        }

        public async Task PublishUnitResultsAsync(Guid unitId, Guid? courseOfferingId, string publishedBy, CancellationToken ct = default)
        {
            var results = (await _unitResultRepository.GetByUnitAsync(unitId, ct))
                .Where(r => r.PublicationStatus == ResultPublicationStatus.Approved)
                .ToList();

            foreach (var result in results)
            {
                result.PublicationStatus = ResultPublicationStatus.Published;
                result.IsPublished = true;
                result.PublishedDate = DateTime.UtcNow;
                result.PublishedBy = publishedBy;
                await _unitResultRepository.UpdateAsync(result, ct);
            }

            await _dbContext.SaveChangesAsync(ct);
            await _auditService.LogActivityAsync("ResultsPublished", "UnitResult", unitId.ToString(),
                $"Unit: {unitId}, Published by: {publishedBy}, Count: {results.Count}");
        }

        // ============================================================
        // RECALCULATION
        // ============================================================

        public async Task RecalculateAfterGradeChangeAsync(Guid studentId, Guid unitId, CancellationToken ct = default)
        {
            var result = await CalculateFinalUnitScoreAsync(studentId, unitId, null, ct);
            await EvaluateCertificateEligibilityAsync(studentId, ct);
            await _auditService.LogActivityAsync("GradeRecalculatedAfterChange", "UnitResult", result.Id.ToString(),
                $"Student: {studentId}, Unit: {unitId}, New Final: {result.FinalPercentage}%");
        }

        // ============================================================
        // BULK CALCULATION
        // ============================================================

        public async Task<IEnumerable<UnitResult>> CalculateAllUnitResultsAsync(
            Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var students = courseOfferingId.HasValue
                ? await _dbContext.CourseOfferingEnrollments
                    .Where(e => e.CourseOfferingId == courseOfferingId.Value && !e.IsDeleted)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync(ct)
                : await _dbContext.Enrollments
                    .Where(e => !e.IsDeleted)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync(ct);

            var results = new List<UnitResult>();
            foreach (var studentId in students)
            {
                results.Add(await CalculateFinalUnitScoreAsync(studentId, unitId, courseOfferingId, ct));
            }

            await _auditService.LogActivityAsync("BulkResultCalculation", "UnitResult", unitId.ToString(),
                $"Unit: {unitId}, Students calculated: {results.Count}");
            return results;
        }
    }
}
