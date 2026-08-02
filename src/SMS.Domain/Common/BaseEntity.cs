using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Common
{
    /// <summary>
    /// Base abstract class for all domain entities providing common properties,
    /// auditing fields, and multi-tenancy support.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant identifier for multi-tenancy isolation
        /// </summary>
        [Required]
        public Guid TenantId { get; set; }

        /// <summary>
        /// User who created the record
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = "SYSTEM";

        /// <summary>
        /// Date and time when the record was created
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who last modified the record
        /// </summary>
        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Date and time when the record was last modified
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// User who deleted the record (soft delete)
        /// </summary>
        [MaxLength(100)]
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Date and time when the record was deleted (soft delete)
        /// </summary>
        public DateTime? DeletedDate { get; set; }

        /// <summary>
        /// Flag indicating if the record is soft deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Concurrency token for optimistic concurrency control
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// Marks the entity as deleted (soft delete)
        /// </summary>
        /// <param name="deletedBy">User performing the deletion</param>
        public void SoftDelete(string deletedBy)
        {
            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Restores a soft-deleted entity
        /// </summary>
        public void Restore()
        {
            IsDeleted = false;
            DeletedBy = null;
            DeletedDate = null;
        }

        /// <summary>
        /// Updates the audit fields for modification
        /// </summary>
        /// <param name="modifiedBy">User performing the modification</param>
        public void UpdateAudit(string modifiedBy)
        {
            ModifiedBy = modifiedBy;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}