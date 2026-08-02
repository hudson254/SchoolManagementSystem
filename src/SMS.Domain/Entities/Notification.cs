using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Notification : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        [MaxLength(500)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }

        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public virtual User? User { get; set; }
    }
}