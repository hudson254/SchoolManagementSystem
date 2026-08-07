using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents an issue reported by a student or lecturer about an
    /// incorrect enrollment or teaching assignment. Used by Moderators and
    /// Administrators to review and resolve assignment issues.
    /// </summary>
    [Table("assignment_issue_reports")]
    public class AssignmentIssueReport : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// The user who reported the issue.
        /// </summary>
        [Required]
        public Guid ReporterUserId { get; set; }

        /// <summary>
        /// The type of assignment the issue relates to.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AssignmentType { get; set; } = string.Empty; // Enrollment / Teaching

        /// <summary>
        /// The ID of the course offering enrollment or lecturer assignment
        /// that the issue relates to.
        /// </summary>
        public Guid? CourseOfferingEnrollmentId { get; set; }

        /// <summary>
        /// The ID of the course offering lecturer assignment that the issue
        /// relates to.
        /// </summary>
        public Guid? CourseOfferingLecturerId { get; set; }

        /// <summary>
        /// The course offering involved.
        /// </summary>
        [Required]
        public Guid CourseOfferingId { get; set; }

        /// <summary>
        /// Reason provided by the reporter.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Current lifecycle status of the issue.
        /// </summary>
        public AssignmentIssueStatus Status { get; set; } = AssignmentIssueStatus.Pending;

        /// <summary>
        /// Optional resolution notes by the moderator/administrator.
        /// </summary>
        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }

        /// <summary>
        /// User who resolved the issue.
        /// </summary>
        public Guid? ResolvedByUserId { get; set; }

        /// <summary>
        /// When the issue was resolved.
        /// </summary>
        public DateTime? ResolvedDate { get; set; }

        // Navigation properties
        public virtual CourseOffering CourseOffering { get; set; } = null!;
        public virtual CourseOfferingEnrollment CourseOfferingEnrollment { get; set; } = null!;
        public virtual CourseOfferingLecturer CourseOfferingLecturer { get; set; } = null!;
    }
}
