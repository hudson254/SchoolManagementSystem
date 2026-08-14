using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayName => BuildDisplayName();
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public Guid? ProgrammeId { get; set; }
        public string? ProgrammeName { get; set; }
        public string AcademicStatus { get; set; } = "Active";
        public bool IsEnrolled { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public string? Gender { get; set; }
        public decimal? CumulativeGPA { get; set; }
        public int? TotalCreditsEarned { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        /// <summary>
        /// Staff ID / Establishment Number. Preserves leading zeros.
        /// </summary>
        public string? StaffIdEstNo { get; set; }

        /// <summary>
        /// National ID or Passport Number. Alphanumeric, preserves leading zeros.
        /// </summary>
        public string? NationalIdPassport { get; set; }

        /// <summary>
        /// Tracks the registration approval lifecycle.
        /// </summary>
        public string RegistrationStatus { get; set; } = "PendingCourseSelection";

        private string BuildDisplayName()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Title))
                parts.Add(Title);
            parts.Add(FirstName);
            if (!string.IsNullOrWhiteSpace(MiddleName))
                parts.Add(MiddleName);
            parts.Add(LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }
    }

    public class StudentDetailsDto : StudentDto
    {
        public Guid? CurrentSemesterId { get; set; }
        public string? CurrentSemesterName { get; set; }
        public int CurrentSemesterNumber { get; set; }
        public decimal CurrentGPA { get; set; }
        public int TotalCredits { get; set; }
        public int CompletedUnits { get; set; }
        public int EnrollmentCount { get; set; }
        public int GradeCount { get; set; }
        public int TotalEnrollments { get; set; }
        public int InProgressUnits { get; set; }
        public string? Organization { get; set; }
        public string? Username { get; set; }
        public string? UserEmail { get; set; }
        public bool IsEmailVerified { get; set; }
        public List<EnrollmentSummaryDto> Enrollments { get; set; } = new List<EnrollmentSummaryDto>();
        public List<GradeSummaryDto> Grades { get; set; } = new List<GradeSummaryDto>();
    }

    public class EnrollmentSummaryDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? CourseCode { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitName { get; set; }
        public string? UnitCode { get; set; }
        public int Credits { get; set; }
        public Guid? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class GradeSummaryDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public string? UnitName { get; set; }
        public string? UnitCode { get; set; }
        public decimal Score { get; set; }
        public string? LetterGrade { get; set; }
        public string? Grade { get; set; }
        public int Credits { get; set; }
        public string? Remarks { get; set; }
        public Guid? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
