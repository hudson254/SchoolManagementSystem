namespace SMS.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for student information
    /// </summary>
    public class StudentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public Guid? ProgrammeId { get; set; }
        public string? ProgrammeName { get; set; }
        public string? AcademicStatus { get; set; }
        public bool IsEnrolled { get; set; }
        public decimal? CumulativeGPA { get; set; }
        public int TotalCreditsEarned { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Detailed student DTO with additional information
    /// </summary>
    public class StudentDetailsDto : StudentDto
    {
        public string? Organization { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public Guid? CurrentSemesterId { get; set; }
        public string? CurrentSemesterName { get; set; }
        public int CurrentSemesterNumber { get; set; }
        public int TotalEnrollments { get; set; }
        public int CompletedUnits { get; set; }
        public int InProgressUnits { get; set; }
        public IEnumerable<EnrollmentSummaryDto> Enrollments { get; set; } = new List<EnrollmentSummaryDto>();
        public IEnumerable<GradeSummaryDto> Grades { get; set; } = new List<GradeSummaryDto>();
        public AccommodationAssignmentDto? Accommodation { get; set; }
    }

    /// <summary>
    /// Enrollment summary DTO
    /// </summary>
    public class EnrollmentSummaryDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
    }

    /// <summary>
    /// Grade summary DTO
    /// </summary>
    public class GradeSummaryDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string? Grade { get; set; }
        public decimal? Score { get; set; }
        public string? Remarks { get; set; }
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Paged result DTO
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}