using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    [Table("rooms")]
    public class Room : BaseEntity, ITenantAwareEntity
    {
        [Column("room_number")]
        [MaxLength(20)]
        [Required]
        public string RoomNumber { get; set; } = string.Empty;

        [Column("block_id")]
        public Guid BlockId { get; set; }

        [Column("floor")]
        public int Floor { get; set; }

        [Column("capacity")]
        public int Capacity { get; set; }

        [Column("occupied_count")]
        public int OccupiedCount { get; set; }

        [Column("room_type")]
        [MaxLength(50)]
        public string? RoomType { get; set; }

        [Column("facilities")]
        [MaxLength(500)]
        public string? Facilities { get; set; }

        [Column("price_per_semester")]
        public decimal PricePerSemester { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_available")]
        public bool IsAvailable { get; set; } = true;

        [NotMapped]
        public bool IsOccupied => OccupiedCount > 0;

        public void Occupy() { OccupiedCount++; }
        public void Vacate() { if (OccupiedCount > 0) OccupiedCount--; }

        // Navigation properties
        public virtual Block Block { get; set; }
        public virtual ICollection<Accommodation>? Accommodations { get; set; }
    }
}
