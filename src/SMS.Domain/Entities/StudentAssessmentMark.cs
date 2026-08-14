using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Tracks a student's mark for a specific assessment.
    /// </summary>
    public class StudentAssessmentMark : BaseEntity, ITenantAwareEntity
    {
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? CourseOfferingId { get; set; }

        /// <summary>Raw mark before weighting (0-MaxScore).</summary>
        public decimal Mark { get; set; }

        /// <summary>0-100 normalized percentage.</summary>
        public decimal Percentage { get; set; }

        /// <summary>Weighted contribution to final score (Percentage * Weight / 100).</summary>
        public decimal WeightedScore { get; set; }

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

        public Guid? GradedBy { get; set; }
        public DateTime? GradedDate { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
        public virtual Enrollment Enrollment { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
    }
}
