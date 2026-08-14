namespace SMS.Application.Features.Assessments.DTOs
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? UserRole { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? PreviousValue { get; set; }
        public string? NewValue { get; set; }
        public string? Reason { get; set; }
        public string? IpAddress { get; set; }
        public string? SessionId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? AssessmentId { get; set; }
        public Guid? StudentId { get; set; }
    }

    public class LecturerDashboardDto
    {
        public Guid LecturerId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public List<AssessmentDto> Assessments { get; set; } = new();
        public List<StudentResultDto> StudentResults { get; set; } = new();
        public decimal PassRate { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new();
        public List<StudentResultDto> StudentsAtRisk { get; set; } = new();
        public List<AssessmentDto> IncompleteAssessments { get; set; } = new();
        public List<AssessmentDto> PendingGradingTasks { get; set; } = new();
        public int TotalAssessments { get; set; }
        public int TotalStudents { get; set; }
    }
}


