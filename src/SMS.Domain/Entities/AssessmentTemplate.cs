using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Reusable assessment configuration template that can be applied to multiple units.
    /// </summary>
    public class AssessmentTemplate : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public Guid? AssessmentTypeId { get; set; }
        public bool IsMandatory { get; set; }
        public bool RequiresModeration { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SortOrder { get; set; }

        public virtual AssessmentType AssessmentType { get; set; }
    }
}
