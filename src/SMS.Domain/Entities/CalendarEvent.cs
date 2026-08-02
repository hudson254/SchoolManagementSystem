using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class CalendarEvent : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(50)]
        public string? EventType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        public bool IsAllDay { get; set; } = false;
        public bool IsPublic { get; set; } = true;

        [MaxLength(50)]
        public string? Recurrence { get; set; }

        public Guid? AcademicYearId { get; set; }
        public Guid? SemesterId { get; set; }

        public virtual AcademicYear? AcademicYear { get; set; }
        public virtual Semester? Semester { get; set; }
    }
}