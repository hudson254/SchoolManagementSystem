using SMS.Domain.Interfaces;

namespace SMS.API.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;
        private readonly string _defaultTenantId;

        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _defaultTenantId = configuration["Tenant:DefaultTenantId"] ?? "11111111-1111-1111-1111-111111111111";
        }

        public async Task InvokeAsync(HttpContext context, ITenantStore tenantStore)
        {
            // Skip tenant resolution for health check and other public endpoints
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            if (path == "/health" || path == "/health/")
            {
                await _next(context);
                return;
            }

            try
            {
                // Resolve tenant ID from header or subdomain
                var tenantId = _defaultTenantId;
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
