using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Course entity representing academic courses offered
    /// </summary>
    public class Course : BaseEntity
    {
        /// <summary>
        /// Course name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Course code (e.g., BSCS, BBA)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Course description
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Duration in months
        /// </summary>
        public int Duration { get; set; } = 48;

        /// <summary>
        /// Total credits required for completion
        /// </summary>
        public int TotalCredits { get; set; }

        /// <summary>
        /// Whether the course is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Department ID
        /// </summary>
        [Required]
        public Guid DepartmentId { get; set; }

        /// <summary>
        /// Admission requirements
        /// </summary>
        [MaxLength(1000)]
        public string? AdmissionRequirements { get; set; }

        /// <summary>
        /// Course objectives
        /// </summary>
        [MaxLength(2000)]
        public string? Objectives { get; set; }

        /// <summary>
        /// Navigation property for department
        /// </summary>
        public virtual Department Department { get; set; } = null!;

        /// <summary>
        /// Navigation property for programmes
        /// </summary>
        public virtual ICollection<Programme> Programmes { get; set; } = new List<Programme>();

        /// <summary>
        /// Navigation property for units
        /// </summary>
        public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}