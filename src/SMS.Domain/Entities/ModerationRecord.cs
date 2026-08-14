using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Records a moderation review of assessment marks.
    /// </summary>
    public class ModerationRecord : BaseEntity, ITenantAwareEntity
    {
        public Guid AssessmentId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? UnitId { get; set; }
        public ModerationStatus Status { get; set; } = ModerationStatus.PendingReview;
        public string? Comments { get; set; }
        public string? ModeratedBy { get; set; }
        public DateTime? ModeratedDate { get; set; }
        public string? ReturnedReason { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
        public virtual Unit Unit { get; set; }
    }
}
