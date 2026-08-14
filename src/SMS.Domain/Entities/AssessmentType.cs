using SMS.Domain.Common;
using SMS.Domain.Enums;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class AssessmentType : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AssessmentTypeCategory Category { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystemDefined { get; set; }
        public int? SortOrder { get; set; }
        public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
        public virtual ICollection<AssessmentTemplate> Templates { get; set; } = new List<AssessmentTemplate>();
    }
}
