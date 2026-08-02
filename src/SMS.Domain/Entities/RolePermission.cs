using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Role-Permission mapping for fine-grained access control
    /// </summary>
    public class RolePermission : BaseEntity
    {
        /// <summary>
        /// Role ID
        /// </summary>
        [Required]
        public Guid RoleId { get; set; }

        /// <summary>
        /// Permission name/identifier
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Permission { get; set; } = string.Empty;

        /// <summary>
        /// Resource/Module the permission applies to
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Resource { get; set; } = string.Empty;

        /// <summary>
        /// Permission type (Create, Read, Update, Delete, etc.)
        /// </summary>
        [Required]
        public PermissionType PermissionType { get; set; }

        /// <summary>
        /// Whether the permission is granted
        /// </summary>
        public bool IsGranted { get; set; } = true;

        /// <summary>
        /// Navigation property for role
        /// </summary>
        public virtual Role? Role { get; set; }
    }
}