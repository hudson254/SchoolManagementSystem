using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Classroom : BaseEntity, ITenantAwareEntity
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string RoomNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Building { get; set; }

        public int Capacity { get; set; } = 50;

        [MaxLength(500)]
        public string? Facilities { get; set; }

        public bool HasProjector { get; set; } = false;
        public bool HasWhiteboard { get; set; } = true;
        public bool HasComputers { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
    }
}