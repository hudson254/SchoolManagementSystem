using Microsoft.AspNetCore.Identity;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Role entity extending ASP.NET Core Identity
    /// </summary>
    public class Role : IdentityRole
    {
        /// <summary>
        /// Description of the role
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether the role is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display name for the role
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Priority/level of the role (higher = more permissions)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Navigation property for user roles
        /// </summary>
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        /// <summary>
        /// Navigation property for role permissions
        /// </summary>
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
