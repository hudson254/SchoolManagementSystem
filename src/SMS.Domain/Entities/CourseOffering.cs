using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a specific offering of a course template in a particular
    /// academic year and semester. Each time a course is made available, a new
    /// CourseOffering is created. This preserves historical records and allows
    /// the same course to be offered repeatedly without affecting prior
    /// offerings.
    /// </summary>
    [Table("course_offerings")]
    public class CourseOffering : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// Unique human-readable offering identifier, e.g. WM-2026-S1-001.
        /// Generated automatically when the offering is created.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string OfferingCode { get; set; } = string.Empty;

        /// <summary>
        /// The course template this offering is based on.
        /// </summary>
        [Required]
        public Guid CourseId { get; set; }

        /// <summary>
        /// Academic year in which this offering is held.
        /// </summary>
        [Required]
        public Guid AcademicYearId { get; set; }

        /// <summary>
        /// Semester or term in which this offering is held.
        /// </summary>
        [Required]
        public Guid SemesterId { get; set; }

        /// <summary>
        /// Optional intake/cohort name (e.g. "2026 Main Intake").
        /// </summary>
        [MaxLength(100)]
        public string? Intake { get; set; }

        /// <summary>
        /// Start date of the offering.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the offering.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Start of the registration window for this offering.
        /// </summary>
        public DateTime? RegistrationStartDate { get; set; }

        /// <summary>
        /// End of the registration window for this offering.
        /// </summary>
        public DateTime? RegistrationEndDate { get; set; }

        /// <summary>
        /// Current lifecycle status of this offering.
        /// </summary>
        public CourseOfferingStatus Status { get; set; } = CourseOfferingStatus.Draft;

        /// <summary>
        /// Whether the offering is currently active (not soft-deleted and
        /// not cancelled).
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional notes about the offering.
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Course Course { get; set; } = null!;
        public virtual AcademicYear AcademicYear { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;

        /// <summary>
        /// Units configured for this offering (snapshots of the course
        /// template units that can be modified independently per offering).
        /// </summary>
        public virtual ICollection<CourseOfferingUnit> Units { get; set; } = new List<CourseOfferingUnit>();

        /// <summary>
        /// Students enrolled in this offering.
        /// </summary>
        public virtual ICollection<CourseOfferingEnrollment> Enrollments { get; set; } = new List<CourseOfferingEnrollment>();

        /// <summary>
        /// Lecturers assigned to teach this offering.
        /// </summary>
        public virtual ICollection<CourseOfferingLecturer> Lecturers { get; set; } = new List<CourseOfferingLecturer>();
    }
}
