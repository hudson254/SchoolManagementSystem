using SMS.Domain.Interfaces;

namespace SMS.API.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;

        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantStore tenantStore)
        {
            try
            {
                // Resolve tenant ID from header or subdomain
                var tenantId = "default";
                if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
                {
                    tenantId = headerTenantId.ToString();
                }

                var tenant = await tenantStore.GetTenantAsync(tenantId);

                if (tenant == null)
                {
                    _logger.LogWarning("Tenant not found for request");
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid tenant");
                    return;
                }

                if (!tenant.IsActive)
                {
                    _logger.LogWarning("Tenant {TenantId} is inactive", tenant.Id);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Tenant is inactive");
                    return;
                }

                // Add tenant info to HttpContext items
                context.Items["TenantId"] = tenant.Id;
                context.Items["TenantName"] = tenant.Name;
                context.Items["TenantSubdomain"] = tenant.Subdomain;

                _logger.LogDebug("Tenant resolved: {TenantName} ({TenantId})", tenant.Name, tenant.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving tenant");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Unable to resolve tenant");
                return;
            }

            await _next(context);
        }
    }
}
