using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// An OMS road account. Balance/allocation semantics are an open requirement
    /// (Documentation/OMS/OMS_OPEN_REQUIREMENTS.md #18) — Phase 2 provides the
    /// persistence foundation only; NO accounting calculations are implemented.
    /// </summary>
    [Table("oms_road_accounts")]
    public class RoadAccount : BaseEntity, ITenantAwareEntity
    {
        /// <summary>Tenant-unique account code.</summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public RoadAccountType AccountType { get; set; } = RoadAccountType.Road;

        /// <summary>
        /// Current balance. Not modified by any Phase 2 code path — no posting
        /// rules exist yet (open requirement #18).
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Balance { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
