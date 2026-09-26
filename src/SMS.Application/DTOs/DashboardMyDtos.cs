using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Unit summary used inside dashboard course cards.
    /// </summary>
    public class DashboardUnitDto
    {
        public Guid UnitId { get; set; }
        public Guid? CourseOfferingUnitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Credits { get; set; }
    }

    /// <summary>
    /// Course (offering) card shown on the lecturer dashboard.
    /// </summary>
    public class LecturerCourseDto
    {
        public Guid CourseOfferingId { get; set; }
        public string OfferingCode { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string? Intake { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public List<DashboardUnitDto> Units { get; set; } = new List<DashboardUnitDto>();
    }

    /// <summary>
    /// Lecturer dashboard payload: profile + courses taught + unit allocations + accommodation.
    /// </summary>
    public class MyLecturerDashboardDto
    {
        public Guid LecturerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Email { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public List<LecturerCourseDto> Courses { get; set; } = new List<LecturerCourseDto>();
        public List<DashboardUnitDto> UnitAllocations { get; set; } = new List<DashboardUnitDto>();
        public AccommodationAssignmentDto? Accommodation { get; set; }
    }

    /// <summary>
    /// Enrolled course card shown on the student dashboard.
    /// </summary>
    public class StudentCourseDto
    {
        public Guid CourseOfferingId { get; set; }
        public string OfferingCode { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string AcademicYearName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ConfirmationStatus { get; set; } = string.Empty;
        public int AttemptNumber { get; set; } = 1;
        public List<DashboardUnitDto> Units { get; set; } = new List<DashboardUnitDto>();
    }

    /// <summary>
    /// The course a student has selected but is not yet enrolled in. Set from
    /// the persisted Student.SelectedCourseId when the student has no active
    /// course-offering enrollment yet, so the dashboard reflects the choice made
    /// at registration or in the selection wizard instead of rendering empty.
    /// </summary>
    public class StudentPendingCourseDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// RegistrationStatus of the student: PendingCourseSelection when the
        /// choice still needs to be submitted, PendingApproval once submitted,
        /// Approved or Rejected otherwise.
        /// </summary>
        public string RegistrationStatus { get; set; } = string.Empty;

        /// <summary>
        /// Display state for the card. "PendingSelection" while the student still
        /// has to confirm/submit the choice, "PendingApproval" while staff review
        /// it. Never "Active" — an active, offering-backed course is reported in
        /// <see cref="MyStudentDashboardDto.Enrollments"/> instead.
        /// </summary>
        public string Status { get; set; } = "PendingSelection";

        /// <summary>Whether the student still has to submit this selection.</summary>
        public bool RequiresSubmission { get; set; }
    }

    /// <summary>
    /// Student dashboard payload: profile + enrolled courses + accommodation.
    /// </summary>
    public class MyStudentDashboardDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Email { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = string.Empty;

        /// <summary>
        /// The student's registration approval lifecycle, e.g.
        /// PendingCourseSelection, PendingApproval, Approved.
        /// </summary>
        public string RegistrationStatus { get; set; } = string.Empty;
        public List<StudentCourseDto> Enrollments { get; set; } = new List<StudentCourseDto>();

        /// <summary>
        /// The course the student has selected but is not yet enrolled in. Null
        /// once the student has an active course-offering enrollment, so it never
        /// duplicates the cards in <see cref="Enrollments"/>.
        /// </summary>
        public StudentPendingCourseDto? PendingCourse { get; set; }

        public AccommodationAssignmentDto? Accommodation { get; set; }
    }

    /// <summary>
    /// File download descriptor returned by download query handlers.
    /// </summary>
    public class FileDownloadResult
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long Length { get; set; }
        public System.IO.Stream Stream { get; set; } = null!;
    }
}