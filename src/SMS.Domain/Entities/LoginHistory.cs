using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class LoginHistory : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime? LogoutTime { get; set; }

        [MaxLength(45)]
        public string? IPAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Success";

        [MaxLength(100)]
        public string? Email { get; set; }

        public virtual User? User { get; set; }
    }
}