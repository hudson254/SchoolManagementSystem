using System.ComponentModel.DataAnnotations;

namespace SMS.Domain.Common
{
    public abstract class TenantAwareEntity : BaseEntity
    {
        [Required]
        public Guid TenantId { get; set; }

        protected TenantAwareEntity() : base()
        {
        }

        protected TenantAwareEntity(Guid tenantId) : base()
        {
            TenantId = tenantId;
        }
    }
}