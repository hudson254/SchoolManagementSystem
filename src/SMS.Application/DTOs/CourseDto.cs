namespace SMS.Application.DTOs
{
    public class CourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public int TotalCredits { get; set; }
        public bool IsActive { get; set; }
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? DepartmentCode { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CourseDetailsDto : CourseDto
    {
        public string? AdmissionRequirements { get; set; }
        public string? Objectives { get; set; }
        public int TotalUnits { get; set; }
        public int TotalProgrammes { get; set; }
        public int TotalStudents { get; set; }
        public IEnumerable<UnitDto> Units { get; set; } = new List<UnitDto>();
        public IEnumerable<ProgrammeSummaryDto> Programmes { get; set; } = new List<ProgrammeSummaryDto>();
    }

    public class ProgrammeSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int TotalCredits { get; set; }
    }
}