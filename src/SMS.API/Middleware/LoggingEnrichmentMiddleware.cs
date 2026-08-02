using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SMS.API.Middleware
{
    /// <summary>
    /// Middleware that enriches structured logging with request metadata
    /// and scrubs sensitive data from log output.
    /// </summary>
    public class LoggingEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingEnrichmentMiddleware> _logger;

        private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "X-Api-Key",
            "Api-Key",
            "Cookie",
            "Set-Cookie",
            "X-CSRF-TOKEN",
            "X-XSRF-TOKEN"
        };

        private static readonly HashSet<string> SensitiveQueryParams = new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "token",
            "secret",
            "apiKey",
            "api_key",
            "access_token",
            "refresh_token"
        };

        public LoggingEnrichmentMiddleware(RequestDelegate next, ILogger<LoggingEnrichmentMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = context.GetCorrelationId() ?? Guid.NewGuid().ToString();

            // Log request start with scrubbed details
            var scrubbedHeaders = ScrubSensitiveData(context.Request.Headers
                .ToDictionary(h => h.Key, h => h.Value.ToString()));
            var scrubbedQuery = ScrubSensitiveQueryParams(context.Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString()));

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["RequestMethod"] = context.Request.Method,
                ["RequestPath"] = context.Request.Path,
                ["RequestQueryString"] = context.Request.QueryString.ToString(),
                ["UserAgent"] = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown",
                ["RemoteIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            }))
            {
                try
                {
                    await _next(context);
                }
                finally
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "Request completed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }
            }
        }

        /// <summary>
        /// Scrubs sensitive headers by replacing their values with "[REDACTED]".
        /// </summary>
        private static Dictionary<string, string> ScrubSensitiveData(Dictionary<string, string> headers)
        {
            var scrubbed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                scrubbed[header.Key] = SensitiveHeaders.Contains(header.Key)
                    ? "[REDACTED]"
                    : header.Value;
            }
            return scrubbed;
        }

        /// <summary>
        /// Scrubs sensitive query parameters by replacing their values with "[REDACTED]".
        /// </summary>
        private static Dictionary<string, string> ScrubSensitiveQueryParams(Dictionary<string, string> queryParams)
        {
            var scrubbed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var param in queryParams)
            {
                scrubbed[param.Key] = SensitiveQueryParams.Contains(param.Key)
                    ? "[REDACTED]"
                    : param.Value;
            }
            return scrubbed;
        }
    }
}
