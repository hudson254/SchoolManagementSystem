namespace SMS.Application.DTOs
{
    public class UnitDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int ContactHours { get; set; }
        public bool IsActive { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public Guid? PrerequisiteUnitId { get; set; }
        public string? PrerequisiteCode { get; set; }
        public string? PrerequisiteName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UnitDetailsDto : UnitDto
    {
        public string? LearningOutcomes { get; set; }
        public string? AssessmentMethods { get; set; }
        public string? RecommendedTextbooks { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalAllocations { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalLectureNotes { get; set; }
        public IEnumerable<LecturerSummaryDto> AllocatedLecturers { get; set; } = new List<LecturerSummaryDto>();
    }

    public class LecturerSummaryDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public bool IsPrimary { get; set; }
    }
}