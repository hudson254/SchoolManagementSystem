using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class Grade : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? SemesterId { get; set; }
        public decimal Score { get; set; }
        public string? LetterGrade { get; set; }
        public string? Remarks { get; set; }
        public string? GradeValue { get; set; }

        // Additional properties required by handlers
        public DateTime? GradedDate { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual Enrollment Enrollment { get; set; }
        public virtual Semester Semester { get; set; }
    }
}
