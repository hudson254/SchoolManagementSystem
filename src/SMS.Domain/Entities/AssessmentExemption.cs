using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Records an exemption from a specific assessment for a student.
    /// </summary>
    public class AssessmentExemption : BaseEntity, ITenantAwareEntity
    {
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? GrantedBy { get; set; }
        public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
    }
}
