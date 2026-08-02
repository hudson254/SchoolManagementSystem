using Microsoft.AspNetCore.Identity;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// User-Role junction entity
    /// </summary>
    public class UserRole : IdentityUserRole<string>
    {
        /// <summary>
        /// Navigation property for user
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Navigation property for role
        /// </summary>
        public virtual Role Role { get; set; } = null!;
    }
}
