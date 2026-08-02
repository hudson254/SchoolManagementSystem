using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class Semester : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public int SemesterNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? AcademicYearId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsCurrent { get; set; }

        // Navigation properties
        public virtual AcademicYear AcademicYear { get; set; }
    }
}
