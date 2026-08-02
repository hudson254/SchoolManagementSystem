namespace SMS.Application.DTOs
{
    public class ReportRequestDto
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? EntityId { get; set; }
        public string? Format { get; set; } = "PDF";
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    public class ReportResponseDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FileContent { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    public class StudentReportDto
    {
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int GraduatedStudents { get; set; }
        public int SuspendedStudents { get; set; }
        public int WithdrawnStudents { get; set; }
        public Dictionary<string, int> StudentsByProgramme { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> StudentsByGender { get; set; } = new Dictionary<string, int>();
        public List<StudentEnrollmentReportDto> Enrollments { get; set; } = new List<StudentEnrollmentReportDto>();
    }

    public class StudentEnrollmentReportDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public int TotalUnits { get; set; }
        public int CompletedUnits { get; set; }
        public int InProgressUnits { get; set; }
        public decimal GPA { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class LecturerWorkloadReportDto
    {
        public string LecturerName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public int TotalUnits { get; set; }
        public int TotalStudents { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalGraded { get; set; }
        public decimal AverageGrade { get; set; }
        public List<UnitWorkloadDto> Units { get; set; } = new List<UnitWorkloadDto>();
    }

    public class UnitWorkloadDto
    {
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int AssignmentCount { get; set; }
        public int LectureNoteCount { get; set; }
    }

    public class CourseStatisticsDto
    {
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int TotalUnits { get; set; }
        public int TotalProgrammes { get; set; }
        public decimal AverageCompletionRate { get; set; }
        public decimal AverageGPA { get; set; }
    }
}