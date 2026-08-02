using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Programme entity representing academic programmes
    /// </summary>
    public class Programme : BaseEntity
    {
        /// <summary>
        /// Programme name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Programme code
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Programme description
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Duration in months
        /// </summary>
        public int Duration { get; set; } = 48;

        /// <summary>
        /// Total credits required
        /// </summary>
        public int TotalCredits { get; set; }

        /// <summary>
        /// Whether the programme is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Course ID
        /// </summary>
        [Required]
        public Guid CourseId { get; set; }

        /// <summary>
        /// Navigation property for course
        /// </summary>
        public virtual Course Course { get; set; } = null!;

        /// <summary>
        /// Navigation property for students
        /// </summary>
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

        /// <summary>
        /// Navigation property for programme units
        /// </summary>
        public virtual ICollection<ProgrammeUnit> ProgrammeUnits { get; set; } = new List<ProgrammeUnit>();
    }
}