using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Programme-Unit junction entity
    /// </summary>
    public class ProgrammeUnit : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// Programme ID
        /// </summary>
        [Required]
        public Guid ProgrammeId { get; set; }

        /// <summary>
        /// Unit ID
        /// </summary>
        [Required]
        public Guid UnitId { get; set; }

        /// <summary>
        /// Semester/Year when the unit is offered
        /// </summary>
        public int SemesterNumber { get; set; }

        /// <summary>
        /// Whether the unit is required or elective
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Navigation property for programme
        /// </summary>
        public virtual Programme Programme { get; set; } = null!;

        /// <summary>
        /// Navigation property for unit
        /// </summary>
        public virtual Unit Unit { get; set; } = null!;
    }
}