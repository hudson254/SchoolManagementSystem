using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a student's enrollment in a specific course offering. Each
    /// attempt at a course (including retakes) gets its own independent
    /// enrollment record so historical data is never overwritten.
    /// </summary>
    [Table("course_offering_enrollments")]
    public class CourseOfferingEnrollment : BaseEntity, ITenantAwareEntity
    {
        [Required]
        public Guid CourseOfferingId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Enrollment status: PendingConfirmation, Active, Completed, Dropped.
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "PendingConfirmation";

        /// <summary>
        /// Whether the enrollment is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indicates which attempt number this enrollment represents for the
        /// student (1 = first attempt, 2 = retake, etc.).
        /// </summary>
        public int AttemptNumber { get; set; } = 1;

        /// <summary>
        /// Confirmation status of the enrollment by the student.
        /// </summary>
        public ConfirmationStatus ConfirmationStatus { get; set; } = ConfirmationStatus.Pending;

        /// <summary>
        /// When the student confirmed the enrollment.
        /// </summary>
        public DateTime? ConfirmedDate { get; set; }

        /// <summary>
        /// When the student was dropped from the offering, if applicable.
        /// </summary>
        public DateTime? DropDate { get; set; }

        /// <summary>
        /// Optional notes about the enrollment.
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual CourseOffering CourseOffering { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
    }
}
