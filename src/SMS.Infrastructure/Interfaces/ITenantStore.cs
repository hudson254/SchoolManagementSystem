using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface ITenantStore
    {
        Task<Tenant> GetTenantAsync(string tenantId);
        Task<bool> ValidateTenantAsync(string tenantId);
    }
}