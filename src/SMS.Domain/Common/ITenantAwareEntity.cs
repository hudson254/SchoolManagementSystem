using System;

namespace SMS.Domain.Common
{
    public interface ITenantAwareEntity
    {
        Guid TenantId { get; set; }
    }
}