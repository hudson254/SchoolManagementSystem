using System;

namespace SMS.Application.DTOs
{
    public class AssignmentDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SubmissionCount { get; set; }
        public int GradedCount { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? SemesterId { get; set; }
        public decimal Weight { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string? Instructions { get; set; }
        public string? Attachments { get; set; }
        public bool IsGraded { get; set; }
        public bool AllowLateSubmission { get; set; }
        public decimal LatePenaltyPercent { get; set; }
        public string? LecturerName { get; set; }
        public string? SemesterName { get; set; }
    }
}