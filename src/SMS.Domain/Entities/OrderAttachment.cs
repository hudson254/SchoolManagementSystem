using SMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// An attachment linked to an OMS order. Persistence model only in Phase 2 —
    /// upload/download flows arrive in a later phase and MUST reuse the existing
    /// upload infrastructure (<c>UploadService</c>/<c>FileStorageService</c> and
    /// the <see cref="UploadFile"/> record referenced by
    /// <see cref="UploadFileId"/>). No parallel upload system is introduced.
    /// </summary>
    [Table("oms_order_attachments")]
    public class OrderAttachment : BaseEntity, ITenantAwareEntity
    {
        [Column("order_id")]
        public Guid OrderId { get; set; }

        /// <summary>Links to the existing central upload record (upload_files table).</summary>
        [Column("upload_file_id")]
        public Guid UploadFileId { get; set; }

        /// <summary>Original file name as supplied by the user (display only, never a storage path).</summary>
        [Required]
        [MaxLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Storage-relative path produced by the existing file storage service.</summary>
        [Required]
        [MaxLength(1000)]
        public string StoragePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        /// <summary>SHA-256 content hash (hex), computed by the upload pipeline.</summary>
        [MaxLength(64)]
        public string? Sha256Hash { get; set; }

        [MaxLength(200)]
        public string? ContentType { get; set; }

        [MaxLength(100)]
        public string? UploadedByUserId { get; set; }

        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual UploadFile? UploadFile { get; set; }
    }
}
