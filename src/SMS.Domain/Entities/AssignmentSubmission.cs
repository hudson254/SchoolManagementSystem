using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class AssignmentSubmission : BaseEntity, ITenantAwareEntity
    {
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string? SubmissionDate { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? Comments { get; set; }
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
        public string Status { get; set; } = "Submitted";
        public bool IsLate { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? GradedDate { get; set; }
        public string? GraderName { get; set; }

        // Navigation properties
        public virtual Assignment Assignment { get; set; }
        public virtual Student Student { get; set; }
    }
}
