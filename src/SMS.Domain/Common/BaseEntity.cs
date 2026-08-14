using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Common
{
    /// <summary>
    /// Marker for entities that expose a UUID public identifier.
    /// In this system, <see cref="IBaseEntity.Id"/> IS the public identifier used in APIs.
    /// It is a non-sequential, cryptographically random Guid generated at creation and immutable thereafter.
    /// Possession of a UUID is never treated as authorization; tenant isolation and role checks still apply.
    /// </summary>
    public interface IHasPublicId
    {
        /// <summary>
        /// UUID public identifier used in external API routes, DTOs, and cross-service calls.
        /// Equivalent to <see cref="IBaseEntity.Id"/> for BaseEntity-derived types.
        /// </summary>
        Guid Id { get; }
    }

    public interface IBaseEntity : IHasPublicId
    {
        new Guid Id { get; set; }
        Guid TenantId { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
        DateTime? CreatedDate { get; set; }
        DateTime? ModifiedDate { get; set; }
        DateTime? DeletedDate { get; set; }
        string? CreatedBy { get; set; }
        string? ModifiedBy { get; set; }
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
        byte[]? RowVersion { get; set; }
    }

    public abstract class BaseEntity : IBaseEntity
    {
        /// <summary>
        /// UUID public identifier (and primary key).
        /// Generated securely via <see cref="Guid.NewGuid"/> at construction.
        /// Exposed through REST APIs as the resource identifier.
        /// Never regenerated on update. Never accepted from untrusted client input on create
        /// unless an explicit architectural requirement exists.
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();


        [Column("tenant_id")]
        public Guid TenantId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("modified_date")]
        public DateTime? ModifiedDate { get; set; }

        [Column("deleted_date")]
        public DateTime? DeletedDate { get; set; }

        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("modified_by")]
        public string? ModifiedBy { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("deleted_by")]
        public string? DeletedBy { get; set; }

        [Column("row_version")]
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public void SoftDelete(string deletedBy)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedDate = DateTime.UtcNow;
            DeletedBy = deletedBy;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedDate = null;
            DeletedBy = null;
        }
    }
}
