using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Infrastructure.MultiTenancy
{
    public class TenantContext : ITenantContext
    {
        private readonly ITenantResolver _tenantResolver;
        private Tenant? _currentTenant;
        private readonly object _lock = new object();

        public TenantContext(ITenantResolver tenantResolver)
        {
            _tenantResolver = tenantResolver;
        }

        public async Task<Tenant?> GetCurrentTenantAsync()
        {
            if (_currentTenant != null)
                return _currentTenant;

            lock (_lock)
            {
                if (_currentTenant != null)
                    return _currentTenant;
            }

            var tenant = await _tenantResolver.GetTenantAsync();
            if (tenant != null)
            {
                lock (_lock)
                {
                    _currentTenant = tenant;
                }
            }

            return tenant;
        }

        public async Task<Guid> GetCurrentTenantIdAsync()
        {
            var tenant = await GetCurrentTenantAsync();
            return tenant?.Id ?? Guid.Empty;
        }

        public async Task<string?> GetCurrentTenantNameAsync()
        {
            var tenant = await GetCurrentTenantAsync();
            return tenant?.Name;
        }

        public async Task<string?> GetCurrentTenantSubdomainAsync()
        {
            var tenant = await GetCurrentTenantAsync();
            return tenant?.Subdomain;
        }

        public async Task<bool> IsTenantActiveAsync()
        {
            var tenant = await GetCurrentTenantAsync();
            return tenant?.IsActive ?? false;
        }

        public void ClearCache()
        {
            lock (_lock)
            {
                _currentTenant = null;
            }
        }
    }
}