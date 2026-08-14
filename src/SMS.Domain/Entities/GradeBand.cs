using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// A single grade band within a grading scale.
    /// </summary>
    public class GradeBand : BaseEntity, ITenantAwareEntity
    {
        public Guid GradingScaleId { get; set; }
        public decimal MinPercentage { get; set; }
        public decimal MaxPercentage { get; set; }
        public string GradeLetter { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? GpaPoints { get; set; }
        public string ColorCode { get; set; } = "#000000";
        public string? HonorsClassification { get; set; }
        public int SortOrder { get; set; }

        public virtual GradingScale GradingScale { get; set; }
    }
}
