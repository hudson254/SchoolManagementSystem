using System;

namespace SMS.Application.DTOs
{
    public class AssignmentSubmissionDto
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentNumber { get; set; }
        public string? AssignmentTitle { get; set; }
        public decimal MaxScore { get; set; }
        public string? SubmissionDate { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? Comments { get; set; }
        public decimal Score { get; set; }
        public decimal? GradedScore { get; set; }
        public string? GradedDate { get; set; }
        public string? GraderName { get; set; }
        public string? Feedback { get; set; }
        public string Status { get; set; } = "Submitted";
        public bool IsLate { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? GradedAt { get; set; }
    }
}
