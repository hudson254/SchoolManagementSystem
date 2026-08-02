using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SMS.API.Middleware
{
    /// <summary>
    /// Middleware that generates and propagates a correlation ID for each request,
    /// enabling distributed tracing across services.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get correlation ID from request header
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

            // If not provided, generate a new one
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Store in HttpContext items and start Activity
            context.Items[CorrelationIdHeader] = correlationId;

            // Set on the current Activity for distributed tracing
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag("correlation-id", correlationId);
            }

            // Add to response headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // Add to log context
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// Extension methods for CorrelationIdMiddleware.
    /// </summary>
    public static class CorrelationIdExtensions
    {
        /// <summary>
        /// Gets the correlation ID from the current HTTP context.
        /// </summary>
        public static string? GetCorrelationId(this HttpContext context)
        {
            if (context?.Items["X-Correlation-ID"] is string correlationId)
            {
                return correlationId;
            }
            return Activity.Current?.Id;
        }
    }
}
