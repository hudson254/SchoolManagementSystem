using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    public class RolePermission : BaseEntity, ITenantAwareEntity
    {
        public string RoleId { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string PermissionType { get; set; } = string.Empty;
        public bool IsGranted { get; set; } = true;

        // Navigation properties
        public virtual Role Role { get; set; }
    }
}
