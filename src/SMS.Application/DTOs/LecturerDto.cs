namespace SMS.Application.DTOs
{
    public class LecturerDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? Qualifications { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
        public int MaxTeachingLoad { get; set; }
        public string? OfficeLocation { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LecturerDetailsDto : LecturerDto
    {
        public string? Biography { get; set; }
        public int TotalUnitsAllocated { get; set; }
        public int CurrentUnitsCount { get; set; }
        public IEnumerable<UnitSummaryDto> Units { get; set; } = new List<UnitSummaryDto>();
        public IEnumerable<AssignmentSummaryDto> Assignments { get; set; } = new List<AssignmentSummaryDto>();
        public AccommodationAssignmentDto? Accommodation { get; set; }
    }

    public class UnitSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string SemesterName { get; set; } = string.Empty;
    }

    public class AssignmentSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int SubmissionCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}