using SMS.Domain.Entities;
using SMS.Persistence.Data;

namespace SMS.Infrastructure.MultiTenancy
{
    public class TenantStore : ITenantStore
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TenantStore> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

        public TenantStore(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<TenantStore> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId)
        {
            var cacheKey = $"tenant_id_{tenantId}";
            if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
                return cachedTenant;

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);

            if (tenant != null)
            {
                _cache.Set(cacheKey, tenant, _cacheDuration);
            }

            return tenant;
        }

        public async Task<Tenant?> GetTenantBySubdomainAsync(string subdomain)
        {
            if (string.IsNullOrEmpty(subdomain))
                return null;

            var cacheKey = $"tenant_subdomain_{subdomain.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
                return cachedTenant;

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLowerInvariant() && t.IsActive);

            if (tenant != null)
            {
                _cache.Set(cacheKey, tenant, _cacheDuration);
            }

            return tenant;
        }

        public async Task<Tenant?> GetDefaultTenantAsync()
        {
            var cacheKey = "tenant_default";
            if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
                return cachedTenant;

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.IsActive)
                ?? await _context.Tenants.FirstOrDefaultAsync();

            if (tenant != null)
            {
                _cache.Set(cacheKey, tenant, _cacheDuration);
            }

            return tenant;
        }

        public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
        {
            return await _context.Tenants
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        public async Task<Tenant> CreateTenantAsync(Tenant tenant)
        {
            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant created: {TenantName} ({TenantId})", tenant.Name, tenant.Id);
            return tenant;
        }

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            // Clear cache
            var cacheKey = $"tenant_id_{tenant.Id}";
            _cache.Remove(cacheKey);
            if (!string.IsNullOrEmpty(tenant.Subdomain))
            {
                _cache.Remove($"tenant_subdomain_{tenant.Subdomain.ToLowerInvariant()}");
            }

            _logger.LogInformation("Tenant updated: {TenantName} ({TenantId})", tenant.Name, tenant.Id);
        }
    }
}