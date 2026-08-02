using SMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class Course : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int Duration { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? ProgrammeId { get; set; }
        public Guid? SemesterId { get; set; }
        public bool IsActive { get; set; } = true;

        // Additional properties
        public int TotalCredits { get; set; }
        public string? AdmissionRequirements { get; set; }
        public string? Objectives { get; set; }

        // Navigation properties
        public virtual Department Department { get; set; }
        public virtual Programme Programme { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
        public virtual ICollection<Programme> Programmes { get; set; } = new List<Programme>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
