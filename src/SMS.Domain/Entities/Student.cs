using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Student entity representing a student enrolled in the school
    /// </summary>
    public class Student : BaseEntity
    {
        /// <summary>
        /// User ID associated with this student
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Unique student number
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Student's date of birth
        /// </summary>
        [Required]
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Gender (Male, Female, Other)
        /// </summary>
        [MaxLength(10)]
        public string? Gender { get; set; }

        /// <summary>
        /// Physical address
        /// </summary>
        [MaxLength(200)]
        public string? Address { get; set; }

        /// <summary>
        /// Date of enrollment
        /// </summary>
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date of graduation (if graduated)
        /// </summary>
        public DateTime? GraduationDate { get; set; }

        /// <summary>
        /// Program ID the student is enrolled in
        /// </summary>
        public Guid? ProgrammeId { get; set; }

        /// <summary>
        /// Current semester ID
        /// </summary>
        public Guid? CurrentSemesterId { get; set; }

        /// <summary>
        /// Whether the student is currently enrolled
        /// </summary>
        public bool IsEnrolled { get; set; } = true;

        /// <summary>
        /// Academic status (Active, Suspended, Graduated, Withdrawn, Probation)
        /// </summary>
        [MaxLength(20)]
        public string? AcademicStatus { get; set; } = "Active";

        /// <summary>
        /// Cumulative GPA
        /// </summary>
        public decimal? CumulativeGPA { get; set; }

        /// <summary>
        /// Total credits earned
        /// </summary>
        public int TotalCreditsEarned { get; set; } = 0;

        /// <summary>
        /// Emergency contact name
        /// </summary>
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        /// <summary>
        /// Emergency contact phone
        /// </summary>
        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>
        /// Emergency contact relationship
        /// </summary>
        [MaxLength(50)]
        public string? EmergencyContactRelation { get; set; }

        /// <summary>
        /// Navigation property for user
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Navigation property for programme
        /// </summary>
        public virtual Programme? Programme { get; set; }

        /// <summary>
        /// Navigation property for current semester
        /// </summary>
        public virtual Semester? CurrentSemester { get; set; }

        /// <summary>
        /// Navigation property for enrollments
        /// </summary>
        public virtual ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();

        /// <summary>
        /// Navigation property for attendances
        /// </summary>
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        /// <summary>
        /// Navigation property for grades
        /// </summary>
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

        /// <summary>
        /// Navigation property for assignment submissions
        /// </summary>
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();

        /// <summary>
        /// Navigation property for accommodation assignment
        /// </summary>
        public virtual AccommodationAssignment? AccommodationAssignment { get; set; }
    }
}