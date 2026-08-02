using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Assignment : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public Guid UnitId { get; set; }

        [Required]
        public Guid LecturerId { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        public int MaxScore { get; set; } = 100;
        public int Weight { get; set; } = 20;

        public DateTime DueDate { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ClosingDate { get; set; }

        [MaxLength(500)]
        public string? Instructions { get; set; }

        [MaxLength(500)]
        public string? Attachments { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Draft";

        public bool IsGraded { get; set; } = false;
        public bool AllowLateSubmission { get; set; } = false;
        public int LatePenaltyPercent { get; set; } = 10;

        public virtual Unit? Unit { get; set; }
        public virtual Lecturer? Lecturer { get; set; }
        public virtual Semester? Semester { get; set; }
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }
}