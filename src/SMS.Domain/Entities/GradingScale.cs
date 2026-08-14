using SMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Versioned grading scale configuration. Historical results retain the
    /// grading scale version that was in effect at the time of publication.
    /// </summary>
    public class GradingScale : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? CreatedBy { get; set; }

        public virtual ICollection<GradeBand> Bands { get; set; } = new List<GradeBand>();
    }
}
