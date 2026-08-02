using Microsoft.AspNetCore.Http;
using SMS.Domain.Interfaces;
using MultitenancyInterfaces = SMS.Multitenancy.Interfaces;

namespace SMS.Infrastructure.MultiTenancy
{
    public class TenantContext : ITenantContext, MultitenancyInterfaces.ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public string TenantId { get; private set; } = string.Empty;
        public string TenantName { get; private set; } = string.Empty;
        public string ConnectionString { get; private set; } = string.Empty;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            PopulateFromHttpContext();
        }

        private void PopulateFromHttpContext()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return;
            }

            if (context.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantGuid)
            {
                TenantId = tenantGuid.ToString();
            }
            else if (context.Items.TryGetValue("TenantId", out var tenantIdStringObj) && tenantIdStringObj is string tenantIdString)
            {
                TenantId = tenantIdString;
            }

            if (context.Items.TryGetValue("TenantName", out var tenantNameObj) && tenantNameObj is string tenantName)
            {
                TenantName = tenantName;
            }

            if (context.Items.TryGetValue("TenantConnectionString", out var connectionStringObj) && connectionStringObj is string connectionString)
            {
                ConnectionString = connectionString;
            }
        }
    }
}
