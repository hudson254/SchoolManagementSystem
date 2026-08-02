namespace SMS.Application.DTOs
{
    public class EnrollmentDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public Guid SemesterId { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DropDate { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string SemesterName { get; set; } = string.Empty;
    }

    public class BulkEnrollmentDto
    {
        public List<Guid> StudentIds { get; set; } = new List<Guid>();
        public Guid UnitId { get; set; }
        public Guid SemesterId { get; set; }
        public int TotalEnrolled { get; set; }
        public int TotalFailed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}