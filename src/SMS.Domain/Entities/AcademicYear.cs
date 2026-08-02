using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class AcademicYear : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsCurrent { get; set; }
    }
}
