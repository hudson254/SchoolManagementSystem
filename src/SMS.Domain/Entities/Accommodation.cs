using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    public class Accommodation : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// The student assigned to this accommodation (nullable for lecturer assignments).
        /// </summary>
        public Guid? StudentId { get; set; }

        /// <summary>
        /// The lecturer assigned to this accommodation (nullable for student assignments).
        /// </summary>
        public Guid? LecturerId { get; set; }

        /// <summary>
        /// The type of occupant (Student or Lecturer).
        /// </summary>
        public OccupantType OccupantType { get; set; } = OccupantType.Student;

        /// <summary>
        /// The house assigned to this occupant (replaces RoomId).
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
        public virtual Lecturer Lecturer { get; set; }
        public virtual House House { get; set; }
        public virtual Lane Lane { get; set; }
        public virtual Room Room { get; set; }
    }
}
