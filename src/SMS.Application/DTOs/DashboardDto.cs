namespace SMS.Application.DTOs
{
    public class DashboardStatisticsDto
    {
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int ActiveCourses { get; set; }
        public int PendingAssignments { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalGrades { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int PendingVerifications { get; set; }
        public int RecentActivities { get; set; }
        public decimal AverageGPA { get; set; }
        public decimal OccupancyRate { get; set; }
        public Dictionary<string, int> StudentsByProgramme { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> GradesDistribution { get; set; } = new Dictionary<string, int>();
        public List<MonthlyEnrollmentDto> MonthlyEnrollments { get; set; } = new List<MonthlyEnrollmentDto>();
    }

    public class MonthlyEnrollmentDto
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Count { get; set; }
        public int Cumulative { get; set; }
    }

    public class ActivityDto
    {
        public string Message { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string? Status { get; set; }
        public string? Link { get; set; }
    }

    public class EnrollmentTrendsDto
    {
        public List<MonthlyEnrollmentDto> EnrollmentData { get; set; } = new List<MonthlyEnrollmentDto>();
        public List<ProgrammeEnrollmentDto> ProgrammeDistribution { get; set; } = new List<ProgrammeEnrollmentDto>();
        public List<GenderDistributionDto> GenderDistribution { get; set; } = new List<GenderDistributionDto>();
    }

    public class ProgrammeEnrollmentDto
    {
        public string ProgrammeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class GenderDistributionDto
    {
        public string Gender { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class EventDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public string? Time { get; set; }
        public string? Location { get; set; }
        public string? EventType { get; set; }
        public string? Color { get; set; }
    }

    public class PerformanceMetricsDto
    {
        public decimal AverageResponseTime { get; set; }
        public decimal ErrorRate { get; set; }
        public decimal Uptime { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalRequests { get; set; }
        public int ConcurrentUsers { get; set; }
        public decimal DatabaseLatency { get; set; }
        public decimal MemoryUsage { get; set; }
        public decimal CPUUsage { get; set; }
        public List<ApiEndpointMetricDto> Endpoints { get; set; } = new List<ApiEndpointMetricDto>();
    }

    public class ApiEndpointMetricDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public decimal AverageDuration { get; set; }
        public decimal ErrorPercentage { get; set; }
    }

    public class TopStudentDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public decimal GPA { get; set; }
        public int CreditsEarned { get; set; }
        public string? ProfileImage { get; set; }
    }
}