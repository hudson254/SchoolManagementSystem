namespace SMS.Application.DTOs
{
    public class TimetableDto
    {
        public Guid Id { get; set; }
        public Guid ClassId { get; set; }
        public Guid SemesterId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Venue { get; set; }
        public string? Topic { get; set; }
        public bool IsActive { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string Duration => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }

    public class WeeklyTimetableDto
    {
        public string WeekStartDate { get; set; } = string.Empty;
        public string WeekEndDate { get; set; } = string.Empty;
        public List<DailyTimetableDto> Days { get; set; } = new List<DailyTimetableDto>();
    }

    public class DailyTimetableDto
    {
        public string Day { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<TimetableDto> Entries { get; set; } = new List<TimetableDto>();
    }
}