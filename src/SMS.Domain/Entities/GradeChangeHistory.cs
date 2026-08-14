using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Immutable record of every grade change event.
    /// </summary>
    public class GradeChangeHistory : BaseEntity, ITenantAwareEntity
    {
        public Guid? AssessmentId { get; set; }
        public Guid? StudentAssessmentMarkId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }

        public decimal PreviousScore { get; set; }
        public decimal NewScore { get; set; }
        public string? PreviousGradeLetter { get; set; }
        public string? NewGradeLetter { get; set; }

        public GradeChangeReason ChangeReason { get; set; }
        public string? Reason { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
        public string? ChangeDetailsJson { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual StudentAssessmentMark StudentAssessmentMark { get; set; }
        public virtual Student Student { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
    }
}
