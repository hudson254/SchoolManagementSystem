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

        /// <summary>
        /// Optional reference to the centralized <see cref="UploadFile"/> record that
        /// holds the validated storage metadata (storage path, MIME type, SHA-256 hash,
        /// status). All file bytes are managed by the UploadFile/storage pipeline; this
        /// link keeps the domain study-material record decoupled from filesystem paths.
        /// </summary>
        public Guid? UploadFileId { get; set; }

        public virtual Unit? Unit { get; set; }
        public virtual Lecturer? Lecturer { get; set; }
        public virtual UploadFile? UploadFile { get; set; }
    }
}