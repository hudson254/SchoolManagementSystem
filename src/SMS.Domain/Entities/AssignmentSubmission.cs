using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class AssignmentSubmission : BaseEntity
    {
        [Required]
        public Guid AssignmentId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? FilePath { get; set; }

        [MaxLength(200)]
        public string? FileName { get; set; }

        [MaxLength(50)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public int? Score { get; set; }

        [MaxLength(500)]
        public string? Feedback { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Submitted";

        public bool IsLate { get; set; } = false;
        public DateTime? GradedDate { get; set; }
        public Guid? GradedBy { get; set; }

        public virtual Assignment? Assignment { get; set; }
        public virtual Student? Student { get; set; }
    }
}