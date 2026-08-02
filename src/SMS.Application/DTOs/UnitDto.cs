using System;

namespace SMS.Application.DTOs
{
    public class UnitDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int ContactHours { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int Semester { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public Guid? PrerequisiteUnitId { get; set; }
        public string PrerequisiteCode { get; set; } = string.Empty;
        public string PrerequisiteName { get; set; } = string.Empty;
        public string LearningOutcomes { get; set; } = string.Empty;
        public string AssessmentCriteria { get; set; } = string.Empty;
        public string RecommendedResources { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int TotalStudents { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = "Active";
    }
}
