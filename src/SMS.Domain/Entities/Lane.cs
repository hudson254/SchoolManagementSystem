using SMS.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a lane within an accommodation area.
    /// Each lane contains multiple houses.
    /// Examples: Lane A, East Lane, North Lane, Staff Lane, Riverside Lane
    /// </summary>
    public class Lane : BaseEntity, ITenantAwareEntity
    {
        [Required]
        [MaxLength(100)]
        public string LaneName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Default numbering format for houses in this lane.
        /// Supports formats like: D3 (001, 002...), D4 (0001, 0002...)
        /// </summary>
        [MaxLength(20)]
        public string NumberingFormat { get; set; } = "D3";

        /// <summary>
        /// Starting house number for auto-generation (default: 1)
        /// </summary>
        public int StartingHouseNumber { get; set; } = 1;

        // Navigation properties
        public virtual ICollection<House> Houses { get; set; } = new List<House>();
    }
}
