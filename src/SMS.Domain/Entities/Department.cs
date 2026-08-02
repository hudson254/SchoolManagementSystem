using SMS.Domain.Common;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class Department : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
        public virtual ICollection<Lecturer> Lecturers { get; set; } = new List<Lecturer>();
    }
}
