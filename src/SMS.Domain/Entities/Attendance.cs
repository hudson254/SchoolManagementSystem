using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class Attendance : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public bool IsPresent { get; set; }
        public string Status { get; set; } = string.Empty; // Present, Absent, Late, Excused
        public string? Remarks { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; }
    }
}
