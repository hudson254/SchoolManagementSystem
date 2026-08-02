using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class Timetable : BaseEntity, ITenantAwareEntity
    {
        public Guid ClassId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? RoomId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string DayOfWeek { get; set; }
        public string RoomNumber { get; set; }
        public bool IsActive { get; set; } = true;
        //public string TenantId { get; set; }

        // Navigation properties
        public virtual Unit Unit { get; set; }
        public virtual Lecturer Lecturer { get; set; }
        public virtual Room Room { get; set; }
    }
}
