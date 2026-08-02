using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Common
{
    public abstract class TenantAwareEntity : BaseEntity, ITenantAwareEntity
    {
        // TenantId is inherited from BaseEntity (Guid)
        // No need to redeclare it

        [Column("tenant_name")]
        [MaxLength(100)]
        public string? TenantName { get; set; }
    }
}