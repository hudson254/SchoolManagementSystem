using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a single assessable component within a unit or course offering.
    /// Examples: Assignment 1, CAT, Final Examination, Practical, etc.
    /// </summary>
    public class Assessment : BaseEntity, ITenantAwareEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid AssessmentTypeId { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? AssessmentTemplateId { get; set; }

        /// <summary>Maximum possible score (default 100).</summary>
        public decimal MaxScore { get; set; } = 100;

        /// <summary>Weight percentage contribution to the final unit score (0-100).</summary>
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

        /// <summary>When set, weightings are locked and cannot be changed without admin override.</summary>
        public bool IsWeightLocked { get; set; }
        public DateTime? WeightLockedDate { get; set; }
        public string? WeightLockedBy { get; set; }

        public bool IsActive { get; set; } = true;
        public int? SortOrder { get; set; }

        /// <summary>Optional feedback template shown to students upon publication.</summary>
        public string? FeedbackTemplate { get; set; }

        // Audit
        public string? CreatedByLecturerId { get; set; }

        // Navigation properties
        public virtual Unit Unit { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual AssessmentType AssessmentType { get; set; }
        public virtual Lecturer Lecturer { get; set; }
        public virtual Assignment LinkedAssignment { get; set; }
        public virtual AssessmentTemplate AssessmentTemplate { get; set; }
        public virtual ICollection<StudentAssessmentMark> Marks { get; set; } = new List<StudentAssessmentMark>();
        public virtual ICollection<AssessmentExemption> Exemptions { get; set; } = new List<AssessmentExemption>();
        public virtual ICollection<GradeChangeHistory> ChangeHistory { get; set; } = new List<GradeChangeHistory>();
    }
}
