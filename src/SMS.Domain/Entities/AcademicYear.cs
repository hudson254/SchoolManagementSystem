using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Academic year entity representing the academic calendar year
    /// </summary>
    public class AcademicYear : BaseEntity
    {
        /// <summary>
        /// Academic year name (e.g., 2024-2025)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Academic year start date
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Academic year end date
        /// </summary>
        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Whether the academic year is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether it's the current academic year
        /// </summary>
        public bool IsCurrent { get; set; } = false;

        /// <summary>
        /// Navigation property for semesters
        /// </summary>
        public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();

        /// <summary>
        /// Navigation property for events
        /// </summary>
        public virtual ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
    }
}