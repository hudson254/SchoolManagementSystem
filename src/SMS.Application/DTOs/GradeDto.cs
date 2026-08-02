namespace SMS.Application.DTOs
{
    public class GradeDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid EnrollmentId { get; set; }
        public string? GradeValue { get; set; }
        public decimal? Score { get; set; }
        public string? Remarks { get; set; }
        public DateTime? GradedDate { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public int Credits { get; set; }
        public decimal? GradePoints { get; set; }
    }

    public class TranscriptDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public int TotalCreditsEarned { get; set; }
        public decimal CumulativeGPA { get; set; }
        public decimal SemesterGPA { get; set; }
        public List<SemesterTranscriptDto> Semesters { get; set; } = new List<SemesterTranscriptDto>();
        public List<GradeSummaryDto> AllGrades { get; set; } = new List<GradeSummaryDto>();
    }

    public class SemesterTranscriptDto
    {
        public string SemesterName { get; set; } = string.Empty;
        public int SemesterNumber { get; set; }
        public int Credits { get; set; }
        public decimal GPA { get; set; }
        public List<GradeSummaryDto> Grades { get; set; } = new List<GradeSummaryDto>();
    }
}