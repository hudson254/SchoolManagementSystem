using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Final computed result for a student in a unit.
    /// </summary>
    public class UnitResult : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid? GradingScaleVersionId { get; set; }

        public decimal FinalPercentage { get; set; }
        public string GradeLetter { get; set; } = string.Empty;
        public string GradeDescription { get; set; } = string.Empty;
        public decimal? GpaPoints { get; set; }

        public ResultPublicationStatus PublicationStatus { get; set; } = ResultPublicationStatus.Draft;
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.NotRequired;

        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? PublishedBy { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? ApprovedBy { get; set; }

        public bool IsRecalculated { get; set; }
        public DateTime? LastCalculatedDate { get; set; }
        public string? LastCalculatedBy { get; set; }

        public virtual Student Student { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual Enrollment Enrollment { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual GradingScale GradingScaleVersion { get; set; }
    }
}
