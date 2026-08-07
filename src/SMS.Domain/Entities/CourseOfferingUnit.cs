using SMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// A unit snapshot for a specific course offering. This allows the unit
    /// structure (name, credits, order, learning outcomes, assessment
    /// weighting) to be modified per offering without affecting the course
    /// template or historical offerings.
    /// </summary>
    [Table("course_offering_units")]
    public class CourseOfferingUnit : BaseEntity, ITenantAwareEntity
    {
        [Required]
        public Guid CourseOfferingId { get; set; }

        /// <summary>
        /// The template unit this offering unit is based on. Nullable to allow
        /// brand-new units added directly to an offering.
        /// </summary>
        public Guid? UnitId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int Credits { get; set; }

        public int ContactHours { get; set; }

        /// <summary>
        /// Display order of the unit within the offering.
        /// </summary>
        public int Order { get; set; }

        [MaxLength(2000)]
        public string? LearningOutcomes { get; set; }

        [MaxLength(2000)]
        public string? AssessmentMethods { get; set; }

        /// <summary>
        /// Assessment weighting configuration (e.g. JSON or key-value pairs).
        /// </summary>
        [MaxLength(2000)]
        public string? AssessmentWeighting { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual CourseOffering CourseOffering { get; set; } = null!;
        public virtual Unit Unit { get; set; } = null!;
    }
}
