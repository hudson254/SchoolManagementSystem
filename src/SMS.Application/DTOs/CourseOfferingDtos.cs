using System;
using System.Collections.Generic;
using SMS.Domain.Enums;

namespace SMS.Application.DTOs
{
    public class CourseOfferingDto
    {
        public Guid Id { get; set; }
        public string OfferingCode { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? CourseCode { get; set; }
        public Guid AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public Guid SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public string? Intake { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public CourseOfferingStatus Status { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public int TotalUnits { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalLecturers { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CourseOfferingDetailsDto : CourseOfferingDto
    {
        public List<CourseOfferingUnitDto> Units { get; set; } = new List<CourseOfferingUnitDto>();
        public List<CourseOfferingLecturerDto> Lecturers { get; set; } = new List<CourseOfferingLecturerDto>();
        public List<CourseOfferingEnrollmentDto> Enrollments { get; set; } = new List<CourseOfferingEnrollmentDto>();
    }

    public class CourseOfferingUnitDto
    {
        public Guid Id { get; set; }
        public Guid CourseOfferingId { get; set; }
        public Guid? UnitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int ContactHours { get; set; }
        public int Order { get; set; }
        public string? LearningOutcomes { get; set; }
        public string? AssessmentMethods { get; set; }
        public string? AssessmentWeighting { get; set; }
        public bool IsActive { get; set; }
    }

    public class CourseOfferingLecturerDto
    {
        public Guid Id { get; set; }
        public Guid CourseOfferingId { get; set; }
        public Guid LecturerId { get; set; }
        public string? LecturerName { get; set; }
        public string? LecturerEmail { get; set; }
        public bool IsPrimary { get; set; }
        public string? Role { get; set; }
        public DateTime AssignedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CourseOfferingEnrollmentDto
    {
        public Guid Id { get; set; }
        public Guid CourseOfferingId { get; set; }
        public string? OfferingCode { get; set; }
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentNumber { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = "PendingConfirmation";
        public bool IsActive { get; set; }
        public int AttemptNumber { get; set; }
        public ConfirmationStatus ConfirmationStatus { get; set; }
        public DateTime? ConfirmedDate { get; set; }
        public DateTime? DropDate { get; set; }
        public string? Notes { get; set; }
    }

    public class AssignmentIssueReportDto
    {
        public Guid Id { get; set; }
        public Guid CourseOfferingId { get; set; }
        public string? OfferingCode { get; set; }
        public Guid? StudentId { get; set; }
        public string? StudentName { get; set; }
        public Guid? LecturerId { get; set; }
        public string? LecturerName { get; set; }
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AssignmentIssueStatus Status { get; set; }
        public string? Resolution { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime ReportedDate { get; set; }
    }
}
