using Microsoft.AspNetCore.Http;
using SMS.Domain.Interfaces;
using MultitenancyInterfaces = SMS.Multitenancy.Interfaces;

namespace SMS.Infrastructure.MultiTenancy
{
    public class TenantContext : ITenantContext, MultitenancyInterfaces.ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Lazily reads the current tenant ID from HttpContext.Items["TenantId"].
        /// Reading on property access (rather than caching in the constructor) is
        /// essential because TenantContext is a scoped service that may be resolved
        /// (constructed) BEFORE TenantResolutionMiddleware sets HttpContext.Items.
        /// If we cached the value in the constructor, the tenant would be empty for
        /// the entire request whenever the context is resolved early (e.g. by the
        /// middleware's own ITenantStore dependency), causing Guid.Parse failures
        /// downstream.
        /// </summary>
        public string TenantId
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("TenantId", out var tenantIdObj) == true)
                {
                    if (tenantIdObj is Guid tenantGuid)
                    {
                        return tenantGuid.ToString();
                    }
                    if (tenantIdObj is string tenantIdString)
                    {
                        return tenantIdString;
                    }
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Lazily reads the current tenant name from HttpContext.Items["TenantName"].
        /// </summary>
        public string TenantName
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("TenantName", out var tenantNameObj) == true &&
                    tenantNameObj is string tenantName)
                {
                    return tenantName;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Lazily reads the current tenant connection string from
        /// HttpContext.Items["TenantConnectionString"].
        /// </summary>
        public string ConnectionString
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("TenantConnectionString", out var connectionStringObj) == true &&
                    connectionStringObj is string connectionString)
                {
                    return connectionString;
                }
                return string.Empty;
            }
        }
    }
}
