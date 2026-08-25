using Microsoft.AspNetCore.Identity;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// User-Role junction entity
    /// </summary>
    public class UserRole : IdentityUserRole<string>, ITenantAwareEntity
    {
        /// <summary>
        /// Tenant identifier for multitenant isolation.
        /// Must match the TenantId of the associated User.
        /// </summary>
        public Guid TenantId { get; set; }

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
