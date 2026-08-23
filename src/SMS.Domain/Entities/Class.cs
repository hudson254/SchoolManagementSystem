using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Class : BaseEntity, ITenantAwareEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public Guid UnitId { get; set; }

        [Required]
        public Guid LecturerId { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        public int MaxCapacity { get; set; } = 50;
        public int CurrentEnrollment { get; set; } = 0;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [MaxLength(20)]
        public string? ScheduleDay { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Unit? Unit { get; set; }
        public virtual Lecturer? Lecturer { get; set; }
        public virtual Semester? Semester { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Timetable> TimetableEntries { get; set; } = new List<Timetable>();
    }
}
