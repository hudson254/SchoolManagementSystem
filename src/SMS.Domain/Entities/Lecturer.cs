using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Lecturer entity representing teaching staff
    /// </summary>
    public class Lecturer : BaseEntity
    {
        /// <summary>
        /// User ID associated with this lecturer
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Unique employee number
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EmployeeNumber { get; set; } = string.Empty;

        /// <summary>
        /// Lecturer's specialization area
        /// </summary>
        [MaxLength(100)]
        public string? Specialization { get; set; }

        /// <summary>
        /// Qualifications (degrees, certifications)
        /// </summary>
        [MaxLength(500)]
        public string? Qualifications { get; set; }

        /// <summary>
        /// Whether the lecturer has been verified by moderator
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Date of verification
        /// </summary>
        public DateTime? VerificationDate { get; set; }

        /// <summary>
        /// User ID of the moderator who verified
        /// </summary>
        public Guid? VerifiedBy { get; set; }

        /// <summary>
        /// Whether the lecturer is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Hire date
        /// </summary>
        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Termination date (if applicable)
        /// </summary>
        public DateTime? TerminationDate { get; set; }

        /// <summary>
        /// Maximum teaching load per semester
        /// </summary>
        public int MaxTeachingLoad { get; set; } = 6;

        /// <summary>
        /// Biography/Profile
        /// </summary>
        [MaxLength(1000)]
        public string? Biography { get; set; }

        /// <summary>
        /// Office location
        /// </summary>
        [MaxLength(100)]
        public string? OfficeLocation { get; set; }

        /// <summary>
        /// Navigation property for user
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Navigation property for unit allocations
        /// </summary>
        public virtual ICollection<UnitAllocation> UnitAllocations { get; set; } = new List<UnitAllocation>();

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
        /// Navigation property for accommodation assignment
        /// </summary>
        public virtual AccommodationAssignment? AccommodationAssignment { get; set; }
    }
}