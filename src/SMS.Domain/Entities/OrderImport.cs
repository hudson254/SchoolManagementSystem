using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// An OMS CSV import batch (two-stage validate → confirm workflow).
    /// Phase 2 provides the persistence model only; the importer arrives in a
    /// later phase (open requirements #23-#26).
    /// </summary>
    [Table("oms_order_imports")]
    public class OrderImport : BaseEntity, ITenantAwareEntity
    {
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public OrderImportStatus Status { get; set; } = OrderImportStatus.PendingValidation;

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int InvalidRows { get; set; }

        public int ImportedRows { get; set; }

        public int SkippedRows { get; set; }

        /// <summary>Row-level validation errors (JSON), used by the preview report.</summary>
        public string? ErrorsJson { get; set; }

        [MaxLength(100)]
        public string? UploadedByUserId { get; set; }

        public DateTime? ValidatedAtUtc { get; set; }

        public DateTime? ImportedAtUtc { get; set; }

        public virtual ICollection<OrderImportRow> Rows { get; set; } = new List<OrderImportRow>();
    }
}
