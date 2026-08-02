using SMS.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SMS.Infrastructure.MultiTenancy
{
    public class TenantStore : ITenantStore
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<TenantStore> _logger;
        private readonly ApplicationDbContext _context;
        private const string TenantCacheKeyPrefix = "tenant_";

        public TenantStore(IMemoryCache cache, ILogger<TenantStore> logger, ApplicationDbContext context)
        {
            _cache = cache;
            _logger = logger;
            _context = context;
        }

        public async Task<Tenant> GetTenantAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogWarning("GetTenantAsync called with null or empty tenantId");
                return null;
            }

            var cacheKey = $"{TenantCacheKeyPrefix}{tenantId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out Tenant cachedTenant))
            {
                _logger.LogDebug("Tenant {TenantId} retrieved from cache", tenantId);
                return cachedTenant;
            }

            try
            {
                // Parse tenantId to Guid
                if (!Guid.TryParse(tenantId, out var tenantGuid))
                {
                    _logger.LogWarning("Invalid tenant ID format: {TenantId}", tenantId);
                    return null;
                }

                // Query database for tenant
                var tenant = await _context.Set<Tenant>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantGuid && t.IsActive && !t.IsDeleted);

                if (tenant == null)
                {
                    _logger.LogWarning("Tenant {TenantId} not found or inactive", tenantId);
                    return null;
                }

                // Cache the tenant for 5 minutes
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

                _cache.Set(cacheKey, tenant, cacheOptions);
                _logger.LogInformation("Tenant {TenantId} ({TenantName}) loaded and cached", tenantId, tenant.Name);

                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> ValidateTenantAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogWarning("ValidateTenantAsync called with null or empty tenantId");
                return false;
            }

            var cacheKey = $"{TenantCacheKeyPrefix}valid_{tenantId}";

            // Try to get validation result from cache
            if (_cache.TryGetValue(cacheKey, out bool isValid))
            {
                return isValid;
            }

            try
            {
                // Parse tenantId to Guid
                if (!Guid.TryParse(tenantId, out var tenantGuid))
                {
                    _logger.LogWarning("Invalid tenant ID format for validation: {TenantId}", tenantId);
                    return false;
                }

                // Check if tenant exists and is active
                var exists = await _context.Set<Tenant>()
                    .AsNoTracking()
                    .AnyAsync(t => t.Id == tenantGuid && t.IsActive && !t.IsDeleted);

                // Cache the validation result for 5 minutes
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

                _cache.Set(cacheKey, exists, cacheOptions);
                _logger.LogDebug("Tenant {TenantId} validation result: {IsValid}", tenantId, exists);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tenant {TenantId}", tenantId);
                return false;
            }
        }
    }
}

