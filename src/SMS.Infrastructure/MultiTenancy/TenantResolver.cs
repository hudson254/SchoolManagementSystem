using Microsoft.AspNetCore.Http;
using SMS.Multitenancy.Interfaces;
using System.Threading.Tasks;

namespace SMS.Infrastructure.MultiTenancy
{
    public class TenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetTenantIdAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "default";

            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
            {
                return tenantId.ToString();
            }

            return "default";
        }

        public async Task<object> GetTenantAsync()
        {
            var tenantId = await GetTenantIdAsync();
            return new { Id = tenantId, Name = tenantId };
        }

        public string GetTenantSubdomain()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var host = context.Request.Host.Host;
            var parts = host.Split('.');
            if (parts.Length > 2)
            {
                return parts[0];
            }

            return null;
        }
    }
}
