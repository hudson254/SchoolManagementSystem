namespace SMS.Application.DTOs
{
    public class AssignmentDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid UnitId { get; set; }
        public Guid LecturerId { get; set; }
        public Guid SemesterId { get; set; }
        public int MaxScore { get; set; }
        public int Weight { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string? Instructions { get; set; }
        public string? Attachments { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsGraded { get; set; }
        public bool AllowLateSubmission { get; set; }
        public int LatePenaltyPercent { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public int SubmissionCount { get; set; }
        public int GradedCount { get; set; }
    }

    public class AssignmentSubmissionDto
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? Comments { get; set; }
        public int? Score { get; set; }
        public string? Feedback { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsLate { get; set; }
        public DateTime? GradedDate { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string AssignmentTitle { get; set; } = string.Empty;
        public int MaxScore { get; set; }
    }
}