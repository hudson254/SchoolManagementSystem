using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Building : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        public int TotalFloors { get; set; }
        public bool HasElevator { get; set; } = false;

        [MaxLength(50)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Block> Blocks { get; set; } = new List<Block>();
    }
}