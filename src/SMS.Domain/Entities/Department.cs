using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Department entity representing academic departments
    /// </summary>
    public class Department : BaseEntity
    {
        /// <summary>
        /// Department name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Department code (e.g., CS, BA, ENG)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Department description
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Whether the department is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Head of department name
        /// </summary>
        [MaxLength(100)]
        public string? HeadOfDepartment { get; set; }

        /// <summary>
        /// Navigation property for courses
        /// </summary>
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

        /// <summary>
        /// Navigation property for programmes
        /// </summary>
        public virtual ICollection<Programme> Programmes { get; set; } = new List<Programme>();
    }
}