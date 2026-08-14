namespace SMS.Application.Features.Reporting.DTOs
{
    public class GradeDistributionReportDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new();
        public Dictionary<string, int> GradeCounts { get; set; } = new();
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public decimal PassRate { get; set; }
        public decimal FailRate { get; set; }
    }

    public class PassFailRateReportDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public decimal PassRatePercentage { get; set; }
        public decimal FailRatePercentage { get; set; }
        public List<AssessmentPassFailDto> AssessmentBreakdown { get; set; } = new();
    }

    public class AssessmentPassFailDto
    {
        public Guid AssessmentId { get; set; }
        public string AssessmentName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public decimal PassRate { get; set; }
    }

    public class AssessmentSummaryReportDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public List<AssessmentSummaryDto> Assessments { get; set; } = new();
        public decimal OverallAverage { get; set; }
        public int TotalAssessments { get; set; }
        public int CompletedAssessments { get; set; }
        public decimal CompletionRate { get; set; }
    }

    public class AssessmentSummaryDto
    {
        public Guid AssessmentId { get; set; }
        public string AssessmentName { get; set; } = string.Empty;
        public string AssessmentTypeName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public int TotalStudents { get; set; }
        public int GradedStudents { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public decimal CompletionRate { get; set; }
    }
}

