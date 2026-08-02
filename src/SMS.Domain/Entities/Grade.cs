using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Grade : BaseEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid EnrollmentId { get; set; }

        [Required]
        [MaxLength(2)]
        public string? GradeValue { get; set; }

        public decimal? Score { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime? GradedDate { get; set; }
        public Guid? GradedBy { get; set; }

        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedDate { get; set; }

        public virtual Student? Student { get; set; }
        public virtual StudentEnrollment? Enrollment { get; set; }
    }
}