using SMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class Lecturer : BaseEntity, ITenantAwareEntity
    {
        public string? UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Department Department { get; set; }
        public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
    }
}
