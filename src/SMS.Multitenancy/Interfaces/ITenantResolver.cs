using System.Threading.Tasks;

namespace SMS.Multitenancy.Interfaces
{
    /// <summary>
    /// Tenant resolver interface for Multi-Tenancy support.
    /// Resolves the current tenant from the request context.
    /// </summary>
    public interface ITenantResolver
    {
        Task<string> GetTenantIdAsync();
        Task<object> GetTenantAsync();
        string GetTenantSubdomain();
    }
}

