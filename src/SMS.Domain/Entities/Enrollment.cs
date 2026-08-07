using SMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class Enrollment : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid? AcademicYearId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;

        // Additional property used by handlers
        public DateTime? DropDate { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Course Course { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual AcademicYear AcademicYear { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
