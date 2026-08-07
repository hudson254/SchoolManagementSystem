using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a lecturer assigned to teach a specific course offering.
    /// Each teaching assignment is stored independently so a lecturer can
    /// teach the same course across multiple offerings without losing
    /// historical teaching records.
    /// </summary>
    [Table("course_offering_lecturers")]
    public class CourseOfferingLecturer : BaseEntity, ITenantAwareEntity
    {
        [Required]
        public Guid CourseOfferingId { get; set; }

        [Required]
        public Guid LecturerId { get; set; }

        public DateTime AssignmentDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Assignment status: PendingConfirmation, Active, Completed, Removed.
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "PendingConfirmation";

        /// <summary>
        /// Whether the assignment is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this lecturer is the primary lecturer for the offering.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Confirmation status of the teaching assignment by the lecturer.
        /// </summary>
        public ConfirmationStatus ConfirmationStatus { get; set; } = ConfirmationStatus.Pending;

        /// <summary>
        /// When the lecturer accepted the teaching assignment.
        /// </summary>
        public DateTime? ConfirmedDate { get; set; }

        /// <summary>
        /// When the assignment was removed, if applicable.
        /// </summary>
        public DateTime? RemovedDate { get; set; }

        /// <summary>
        /// Optional notes about the assignment.
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual CourseOffering CourseOffering { get; set; } = null!;
        public virtual Lecturer Lecturer { get; set; } = null!;
    }
}
