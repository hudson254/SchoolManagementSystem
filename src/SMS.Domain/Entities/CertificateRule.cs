using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Institution-wide rules for determining certificate eligibility.
    /// </summary>
    public class CertificateRule : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? MinimumPassingPercentage { get; set; }
        public string? MinimumPassingGradeLetter { get; set; }
        public bool RequireAllMandatoryAssessments { get; set; }
        public bool RequireNoOutstandingIncomplete { get; set; }
        public bool RequireAllRequiredUnits { get; set; }
        public string? AdditionalRequirements { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVersioned { get; set; } = true;
        public int Version { get; set; } = 1;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? CreatedBy { get; set; }
    }
}
