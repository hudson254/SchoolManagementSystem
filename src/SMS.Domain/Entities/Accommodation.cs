using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class Accommodation : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }

        /// <summary>
        /// The house assigned to this student (replaces RoomId).
        /// </summary>
        public Guid HouseId { get; set; }

        /// <summary>
        /// The lane containing the house (redundant for query performance, derived from House.LaneId).
        /// </summary>
        public Guid LaneId { get; set; }

        /// <summary>
        /// Legacy RoomId for backward compatibility (nullable).
        /// </summary>
        public Guid? RoomId { get; set; }

        public DateTime AssignedDate { get; set; }
        public DateTime? VacatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } // Active, Vacated, Pending

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual House House { get; set; }
        public virtual Lane Lane { get; set; }
        public virtual Room Room { get; set; }
    }
}
