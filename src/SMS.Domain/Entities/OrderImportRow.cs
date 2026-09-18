using SMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// A single parsed row of an OMS CSV import. Raw content is preserved so the
    /// confirm step can re-validate exactly what was uploaded (no silent partial
    /// imports — open requirements #23-#26).
    /// </summary>
    [Table("oms_order_import_rows")]
    public class OrderImportRow : BaseEntity, ITenantAwareEntity
    {
        [Column("import_id")]
        public Guid ImportId { get; set; }

        /// <summary>1-based row number in the uploaded file (header excluded).</summary>
        [Column("row_number")]
        public int RowNumber { get; set; }

        /// <summary>Raw parsed row values (JSON) captured at validation time.</summary>
        public string? RawJson { get; set; }

        public bool IsValid { get; set; }

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        public virtual OrderImport Import { get; set; } = null!;
    }
}
