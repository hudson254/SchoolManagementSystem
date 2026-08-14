using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents an uploaded file with all metadata, versioning, and
    /// security-related information. This is the central entity for all
    /// file uploads across the system.
    /// </summary>
    [Table("upload_files")]
    public class UploadFile : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// Original filename as supplied by the user (for reference only).
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// System-generated standardized filename.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string GeneratedFileName { get; set; } = string.Empty;

        /// <summary>
        /// Storage path relative to the uploads root.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// File extension (lowercase, with dot, e.g. ".pdf").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// MIME type as detected from file content.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// SHA-256 hash of the file content (hex string).
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string Sha256Hash { get; set; } = string.Empty;

        /// <summary>
        /// Upload category determining validation rules and storage path.
        /// </summary>
        public UploadCategory Category { get; set; } = UploadCategory.Default;

        /// <summary>
        /// Version number (starts at 1, increments on re-upload).
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// ID of the user who uploaded the file.
        /// </summary>
        [MaxLength(100)]
        public string? UploadedByUserId { get; set; }

        /// <summary>
        /// Username of the uploader.
        /// </summary>
        [MaxLength(256)]
        public string? UploadedByUsername { get; set; }

        /// <summary>
        /// Timestamp when the file was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Virus scan result ("Clean", "Infected", "NotScanned", "Error").
        /// </summary>
        [MaxLength(50)]
        public string VirusScanResult { get; set; } = "NotScanned";

        /// <summary>
        /// Details of the virus scan if infected.
        /// </summary>
        [MaxLength(1000)]
        public string? VirusScanDetails { get; set; }

        /// <summary>
        /// Current status of the file (Active, Quarantined, Deleted, Archived).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Whether this file is a duplicate of a previously uploaded file.
        /// </summary>
        public bool IsDuplicate { get; set; }

        /// <summary>
        /// ID of the original file if this is a duplicate.
        /// </summary>
        public Guid? OriginalFileId { get; set; }

        /// <summary>
        /// Optional context identifiers for linking to domain entities.
        /// </summary>
        public Guid? CourseOfferingId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? AssignmentId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? LecturerId { get; set; }

        /// <summary>
        /// Optional description/reason for the upload.
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey(nameof(OriginalFileId))]
        public virtual UploadFile? OriginalFile { get; set; }
        public virtual ICollection<UploadFile>? Duplicates { get; set; }
    }
}
