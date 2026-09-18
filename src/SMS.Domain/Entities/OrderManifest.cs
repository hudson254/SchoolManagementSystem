using SMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Metadata record for a generated OMS order manifest. The file itself lives
    /// in tenant-segmented manifest storage (<see cref="SMS.Domain.Interfaces.IOrderManifestStorage"/>)
    /// at the configured <c>OrderManifestStorage:BasePath</c>; this record holds
    /// only metadata and is the authorization anchor for downloads.
    /// Phase 2: persistence model + storage foundation (no generation endpoints).
    /// </summary>
    [Table("oms_order_manifests")]
    public class OrderManifest : BaseEntity, ITenantAwareEntity
    {
        [Column("order_id")]
        public Guid OrderId { get; set; }

        /// <summary>Generated, collision-resistant file name (never a client-supplied name).</summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Storage-relative, tenant-segmented path returned by the manifest storage.</summary>
        [Required]
        [MaxLength(1000)]
        public string StoragePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [MaxLength(100)]
        public string? GeneratedByUserId { get; set; }

        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Number of authorized downloads served.</summary>
        public int DownloadedCount { get; set; }

        public DateTime? LastDownloadedAtUtc { get; set; }

        public virtual Order Order { get; set; } = null!;

        /// <summary>Records an authorized manifest download (audit-relevant usage counter).</summary>
        public void RecordDownload()
        {
            DownloadedCount++;
            LastDownloadedAtUtc = DateTime.UtcNow;
        }
    }
}
