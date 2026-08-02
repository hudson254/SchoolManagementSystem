using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Room : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        public Guid BlockId { get; set; }

        public int Capacity { get; set; } = 1;

        [MaxLength(20)]
        public string? RoomType { get; set; }

        public decimal PricePerSemester { get; set; }

        [MaxLength(500)]
        public string? Facilities { get; set; }

        public bool IsAvailable { get; set; } = true;
        public bool IsOccupied { get; set; } = false;

        [MaxLength(20)]
        public string? Status { get; set; } = "Available";

        public virtual Block? Block { get; set; }
        public virtual AccommodationAssignment? CurrentAssignment { get; set; }
        public virtual ICollection<AccommodationAssignment> AssignmentHistory { get; set; } = new List<AccommodationAssignment>();
    }
}