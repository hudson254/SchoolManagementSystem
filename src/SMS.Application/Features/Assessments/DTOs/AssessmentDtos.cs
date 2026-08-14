using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Application.Features.Assessments.DTOs
{
    public class AssessmentTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AssessmentTypeCategory Category { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystemDefined { get; set; }
        public int? SortOrder { get; set; }
    }

    public class AssessmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid AssessmentTypeId { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? AssessmentTemplateId { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public decimal Weight { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public bool AllowLateSubmission { get; set; }
        public decimal LatePenaltyPercent { get; set; }
        public int? GracePeriodDays { get; set; }
        public bool IsOnlineSubmission { get; set; }
        public Guid? LinkedAssignmentId { get; set; }
        public bool IsExemptable { get; set; } = true;
        public bool IsMandatory { get; set; }
        public bool RequiresModeration { get; set; }
        public bool IsAnonymousMarking { get; set; }
        public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;
        public ResultPublicationStatus PublicationStatus { get; set; } = ResultPublicationStatus.Draft;
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.NotRequired;
        public bool IsWeightLocked { get; set; }
        public DateTime? WeightLockedDate { get; set; }
        public string? WeightLockedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SortOrder { get; set; }
        public string? FeedbackTemplate { get; set; }
    }

    public class StudentAssessmentMarkDto
    {
        public Guid Id { get; set; }
        public Guid AssessmentId { get; set; }
        public string? AssessmentName { get; set; }
        public Guid StudentId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Percentage { get; set; }
        public decimal WeightedScore { get; set; }
        public decimal Weight { get; set; }
        public bool IsDraft { get; set; } = true;
        public bool IsModerated { get; set; }
        public DateTime? ModeratedDate { get; set; }
        public string? ModeratedBy { get; set; }
        public string? ModerationComment { get; set; }
        public decimal? OriginalMark { get; set; }
        public decimal? RevisedMark { get; set; }
        public MarkEntrySource EntrySource { get; set; } = MarkEntrySource.ManualEntry;
        public string? ImportBatchReference { get; set; }
        public bool IsExempt { get; set; }
        public string? ExemptionReason { get; set; }
        public string? Feedback { get; set; }
        public bool FeedbackPublished { get; set; }
        public string? GradedBy { get; set; }
        public DateTime? GradedDate { get; set; }
        public ResultPublicationStatus PublicationStatus { get; set; } = ResultPublicationStatus.Draft;
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.NotRequired;
    }

    public class StudentResultDto
    {
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public List<StudentAssessmentMarkDto> AssessmentMarks { get; set; } = new();
        public decimal FinalScore { get; set; }
        public decimal TotalWeight { get; set; }
        public string? FinalGrade { get; set; }
        public string? GradeDescription { get; set; }
        public string? GradeColor { get; set; }
        public bool IsPassed { get; set; }
        public Guid? GradingScaleVersionId { get; set; }
        public bool IsEligibleForCertificate { get; set; }
        public bool IsPublished { get; set; }
        public ResultPublicationStatus PublicationStatus { get; set; } = ResultPublicationStatus.Draft;
    }

    public class WeightValidationResult
    {
        public bool IsValid { get; set; } = true;
        public decimal TotalWeight { get; set; }
        public List<AssessmentWeightDto> Weights { get; set; } = new();
        public List<WeightValidationError> Errors { get; set; } = new();
    }

    public class AssessmentWeightDto
    {
        public Guid AssessmentId { get; set; }
        public string AssessmentName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
    }

    public class WeightValidationError
    {
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
    }

    public class BulkMarkImportResult
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public Guid? ImportBatchId { get; set; }
    }
}
