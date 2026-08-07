using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    public class AccommodationAssignment : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// The student assigned to the house (nullable for lecturer assignments).
        /// </summary>
        public Guid? StudentId { get; set; }

        /// <summary>
        /// The lecturer assigned to the house (nullable for student assignments).
        /// </summary>
        public Guid? LecturerId { get; set; }

        /// <summary>
        /// The type of occupant (Student or Lecturer).
        /// </summary>
        public OccupantType OccupantType { get; set; } = OccupantType.Student;

        /// <summary>
        /// The house assigned to the occupant.
        /// </summary>
        public Guid HouseId { get; set; }

        /// <summary>
        /// The lane containing the assigned house.
        /// </summary>
        public Guid LaneId { get; set; }

        /// <summary>
        /// Legacy RoomId for backward compatibility (nullable).
        /// </summary>
        public Guid? RoomId { get; set; }

        public Guid SemesterId { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime? VacatedDate { get; set; }
        public string Status { get; set; } = "Active";

        // Additional properties
        public DateTime AssignmentDate { get; set; } = DateTime.UtcNow;
        public DateTime? MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public string? Remarks { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Lecturer Lecturer { get; set; }
        public virtual House House { get; set; }
        public virtual Lane Lane { get; set; }
        public virtual Room Room { get; set; }
        public virtual Semester Semester { get; set; }
    }
}
