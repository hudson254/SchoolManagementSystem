using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Timetable : BaseEntity
    {
        [Required]
        public Guid ClassId { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        [Required]
        [MaxLength(20)]
        public string DayOfWeek { get; set; } = string.Empty;

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [MaxLength(50)]
        public string? Venue { get; set; }

        [MaxLength(200)]
        public string? Topic { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Class? Class { get; set; }
        public virtual Semester? Semester { get; set; }
    }
}