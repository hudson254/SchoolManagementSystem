using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface ITenantResolver
    {
        Task<Guid> GetTenantIdAsync();
        Task<Tenant?> GetTenantAsync();
        string? GetTenantSubdomain();
    }
}