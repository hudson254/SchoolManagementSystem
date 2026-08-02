using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class CalendarEvent : BaseEntity, ITenantAwareEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string EventType { get; set; } // Academic, Holiday, Exam, etc.
        public string Location { get; set; }
        public bool IsActive { get; set; } = true;
        //public string TenantId { get; set; }
    }
}
