using System;

namespace SMS.Domain.Interfaces
{
    public interface ITenantContext
    {
        string TenantId { get; }
        string TenantName { get; }
        string ConnectionString { get; }
    }
}