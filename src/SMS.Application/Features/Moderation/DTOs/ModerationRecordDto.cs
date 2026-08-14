using SMS.Domain.Enums;

namespace SMS.Application.Features.Moderation.DTOs
{
    public class ModerationRecordDto
    {
        public Guid Id { get; set; }
        public Guid AssessmentId { get; set; }
        public string AssessmentName { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public Guid? MarkId { get; set; }
        public decimal OriginalScore { get; set; }
        public decimal? RevisedScore { get; set; }
        public ModerationStatus Status { get; set; }
        public string? Comments { get; set; }
        public string? ModeratedBy { get; set; }
        public DateTime? ModeratedDate { get; set; }
        public string? ReviewerComments { get; set; }
    }
}



