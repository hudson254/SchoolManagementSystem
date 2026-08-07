using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a single residential house within a lane.
    /// Each house can be occupied by one student.
    /// </summary>
    public class House : BaseEntity, ITenantAwareEntity
    {
        [Required]
        public Guid LaneId { get; set; }

        /// <summary>
        /// House number string with configurable formatting.
        /// Examples: 001, 002, 101, A1, B2
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string HouseNumber { get; set; } = string.Empty;

        /// <summary>
        /// Numeric house number for sorting purposes.
        /// </summary>
        public int HouseNumberNumeric { get; set; }

        /// <summary>
        /// Current occupancy status of the house.
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = HouseStatus.Vacant;

        /// <summary>
        /// Whether the house is currently occupied by a student.
        /// </summary>
        public bool IsOccupied { get; set; }

        /// <summary>
        /// The occupant currently occupying this house (student or lecturer).
        /// </summary>
        public Guid? OccupantId { get; set; }

        /// <summary>
        /// The type of occupant (Student or Lecturer).
        /// </summary>
        public OccupantType? OccupantType { get; set; }

        /// <summary>
        /// Whether the house is enabled/disabled for use.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether the house is available for assignment.
        /// A house may be unavailable due to maintenance.
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// The semester/period this house is assigned for.
        /// </summary>
        public Guid? SemesterId { get; set; }

        /// <summary>
        /// Additional notes about this house (e.g., maintenance notes).
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Date the current occupant moved in.
        /// </summary>
        public DateTime? OccupiedDate { get; set; }

        /// <summary>
        /// Date the house was vacated (last vacated date).
        /// </summary>
        public DateTime? VacatedDate { get; set; }

        // Navigation properties
        public virtual Lane Lane { get; set; }
        public virtual Student Occupant { get; set; }
        public virtual Lecturer LecturerOccupant { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
        public virtual ICollection<AccommodationAssignment> AccommodationAssignments { get; set; } = new List<AccommodationAssignment>();
    }

    /// <summary>
    /// Constants for house occupancy status values.
    /// </summary>
    public static class HouseStatus
    {
        public const string Vacant = "Vacant";
        public const string Occupied = "Occupied";
        public const string Reserved = "Reserved";
        public const string Maintenance = "Maintenance";
        public const string Disabled = "Disabled";
        public const string Unavailable = "Unavailable";

        /// <summary>
        /// All valid status values.
        /// </summary>
        public static readonly string[] All = { Vacant, Occupied, Reserved, Maintenance, Disabled, Unavailable };

        /// <summary>
        /// Statuses that indicate the house is not available for new assignments.
        /// </summary>
        public static readonly string[] UnavailableStatuses = { Occupied, Reserved, Maintenance, Disabled, Unavailable };

        /// <summary>
        /// Statuses that indicate the house is available for assignment.
        /// </summary>
        public static readonly string[] AvailableStatuses = { Vacant };
    }
}
