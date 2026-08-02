using System;

namespace SMS.Multitenancy.Interfaces
{
    /// <summary>
    /// Tenant context interface for Multi-Tenancy support.
    /// Provides access to current tenant information.
    /// </summary>
    public interface ITenantContext
    {
        string TenantId { get; }
        string TenantName { get; }
    }
}

