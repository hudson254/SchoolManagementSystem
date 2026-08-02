using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Block : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid BuildingId { get; set; }

        public int FloorNumber { get; set; }
        public int TotalRooms { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Building? Building { get; set; }
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}