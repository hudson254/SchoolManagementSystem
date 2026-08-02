using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class Assignment : BaseEntity, ITenantAwareEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UnitId { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime DueDate { get; set; }
        public string? Instructions { get; set; }
        public string? Attachments { get; set; }
        public decimal Weight { get; set; } = 100;
        public bool IsGraded { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? SemesterId { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Additional properties required by Application handlers
        public string Status { get; set; } = "Draft";
        public DateTime? PublishedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public bool AllowLateSubmission { get; set; }
        public decimal LatePenaltyPercent { get; set; }
        public int? Week { get; set; }

        // Navigation properties
        public virtual Unit Unit { get; set; }
        public virtual Lecturer Lecturer { get; set; }
        public virtual Semester Semester { get; set; }
    }
}
