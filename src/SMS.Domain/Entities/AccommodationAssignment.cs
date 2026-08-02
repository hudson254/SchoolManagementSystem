using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class AccommodationAssignment : BaseEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        public DateTime AssignmentDate { get; set; } = DateTime.UtcNow;
        public DateTime? MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [MaxLength(200)]
        public string? AssignedBy { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public virtual Student? Student { get; set; }
        public virtual Room? Room { get; set; }
        public virtual Semester? Semester { get; set; }
    }
}