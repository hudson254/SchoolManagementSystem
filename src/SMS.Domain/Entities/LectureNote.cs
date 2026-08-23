using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class LectureNote : BaseEntity, ITenantAwareEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public Guid UnitId { get; set; }

        [Required]
        public Guid LecturerId { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public int Version { get; set; } = 1;

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = true;

        public virtual Unit? Unit { get; set; }
        public virtual Lecturer? Lecturer { get; set; }
    }
}