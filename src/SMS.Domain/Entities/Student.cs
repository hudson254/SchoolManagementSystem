using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class Student : BaseEntity, ITenantAwareEntity
    {
        public string? UserId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Staff ID / Establishment Number. Preserves leading zeros.
        /// </summary>
        public string? StaffIdEstNo { get; set; }

        /// <summary>
        /// National ID or Passport Number. Alphanumeric, preserves leading zeros.
        /// </summary>
        public string? NationalIdPassport { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public DateTime? GraduationDate { get; set; }
        public Guid? ProgrammeId { get; set; }
        public Guid? CurrentSemesterId { get; set; }
        public string AcademicStatus { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
        public bool IsEnrolled { get; set; } = true;

        /// <summary>
        /// Tracks the registration approval lifecycle.
        /// New registrations start as PendingCourseSelection.
        /// </summary>
        public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.PendingCourseSelection;

        // Additional properties required by Application handlers
        public string? Gender { get; set; }
        public decimal? CumulativeGPA { get; set; }
        public int? TotalCreditsEarned { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Programme Programme { get; set; }
        public virtual Semester CurrentSemester { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
        public virtual ICollection<AccommodationAssignment> AccommodationAssignments { get; set; } = new List<AccommodationAssignment>();
        public virtual ICollection<House> Houses { get; set; } = new List<House>();
    }
}
