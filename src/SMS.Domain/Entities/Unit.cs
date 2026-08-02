using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Unit/Module entity representing individual course units
    /// </summary>
    public class Unit : BaseEntity
    {
        /// <summary>
        /// Unit name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unit code (e.g., CSC101, BBA201)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Unit description
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Credit value
        /// </summary>
        public int Credits { get; set; } = 3;

        /// <summary>
        /// Contact hours per week
        /// </summary>
        public int ContactHours { get; set; } = 3;

        /// <summary>
        /// Whether the unit is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Course ID
        /// </summary>
        [Required]
        public Guid CourseId { get; set; }

        /// <summary>
        /// Prerequisite unit ID (if any)
        /// </summary>
        public Guid? PrerequisiteUnitId { get; set; }

        /// <summary>
        /// Learning outcomes
        /// </summary>
        [MaxLength(2000)]
        public string? LearningOutcomes { get; set; }

        /// <summary>
        /// Assessment methods
        /// </summary>
        [MaxLength(500)]
        public string? AssessmentMethods { get; set; }

        /// <summary>
        /// Recommended textbooks
        /// </summary>
        [MaxLength(500)]
        public string? RecommendedTextbooks { get; set; }

        /// <summary>
        /// Navigation property for course
        /// </summary>
        public virtual Course Course { get; set; } = null!;

        /// <summary>
        /// Navigation property for prerequisite
        /// </summary>
        public virtual Unit? Prerequisite { get; set; }

        /// <summary>
        /// Navigation property for unit allocations
        /// </summary>
        public virtual ICollection<UnitAllocation> Allocations { get; set; } = new List<UnitAllocation>();

        /// <summary>
        /// Navigation property for lecture notes
        /// </summary>
        public virtual ICollection<LectureNote> LectureNotes { get; set; } = new List<LectureNote>();

        /// <summary>
        /// Navigation property for assignments
        /// </summary>
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

        /// <summary>
        /// Navigation property for classes
        /// </summary>
        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

        /// <summary>
        /// Navigation property for student enrollments
        /// </summary>
        public virtual ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();

        /// <summary>
        /// Navigation property for programme units
        /// </summary>
        public virtual ICollection<ProgrammeUnit> ProgrammeUnits { get; set; } = new List<ProgrammeUnit>();
    }
}