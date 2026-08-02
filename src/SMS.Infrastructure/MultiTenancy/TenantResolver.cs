using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace SMS.Infrastructure.MultiTenancy
{
    public class TenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantStore _tenantStore;
        private readonly ILogger<TenantResolver> _logger;

        public TenantResolver(
            IHttpContextAccessor httpContextAccessor,
            ITenantStore tenantStore,
            ILogger<TenantResolver> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenantStore = tenantStore;
            _logger = logger;
        }

        public async Task<Guid> GetTenantIdAsync()
        {
            var context = _httpContextAccessor.HttpContext;

            if (context == null)
                return Guid.Empty;

            // Try from JWT claim
            var tenantIdClaim = context.User?.FindFirst("tenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
                return tenantId;

            // Try from subdomain
            var subdomain = GetTenantSubdomain();
            if (!string.IsNullOrEmpty(subdomain))
            {
                var tenant = await _tenantStore.GetTenantBySubdomainAsync(subdomain);
                if (tenant != null)
                    return tenant.Id;
            }

            // Try from header
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader))
            {
                if (Guid.TryParse(tenantHeader, out var headerTenantId))
                    return headerTenantId;
            }

            // Use default tenant for development
            if (context.Request.Host.Host.Contains("localhost"))
            {
                var defaultTenant = await _tenantStore.GetDefaultTenantAsync();
                if (defaultTenant != null)
                    return defaultTenant.Id;
            }

            _logger.LogWarning("Tenant could not be resolved");
            return Guid.Empty;
        }

        public async Task<Tenant?> GetTenantAsync()
        {
            var tenantId = await GetTenantIdAsync();
            if (tenantId == Guid.Empty)
                return null;

            return await _tenantStore.GetTenantByIdAsync(tenantId);
        }

        public string? GetTenantSubdomain()
        {
            var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
            if (string.IsNullOrEmpty(host))
                return null;

            var parts = host.Split('.');
            if (parts.Length >= 2)
                return parts[0];

            return null;
        }
    }
}