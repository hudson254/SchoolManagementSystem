using SMS.Domain.Common;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class Block : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;

        // Additional properties required by handlers
        public string? Building { get; set; }
        public string? BuildingName { get; set; }

        // Navigation properties
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
