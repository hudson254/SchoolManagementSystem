using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    public class CourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int Duration { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? DepartmentCode { get; set; }
        public Guid? ProgrammeId { get; set; }
        public string? ProgrammeName { get; set; }
        public Guid? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public bool IsActive { get; set; }
        public int TotalUnits { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCredits { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CourseDetailsDto : CourseDto
    {
        public string? AdmissionRequirements { get; set; }
        public string? Objectives { get; set; }
        public int TotalProgrammes { get; set; }
        public List<ProgrammeSummaryDto> Programmes { get; set; } = new List<ProgrammeSummaryDto>();
        public List<UnitSummaryDto> Units { get; set; } = new List<UnitSummaryDto>();
    }

    public class ProgrammeSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int TotalCredits { get; set; }
    }

    public class UnitSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Credits { get; set; }
    }
}
