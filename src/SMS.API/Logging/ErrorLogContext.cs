using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SMS.API.Middleware;
using SMS.Application.Common;

namespace SMS.API.Logging
{
    /// <summary>
    /// Captures the complete technical diagnostic context for an error.
    /// This is the PRIVATE error layer — never exposed to end users.
    /// Stored securely server-side and accessible only to authorized administrators.
    /// </summary>
    public class ErrorLogContext
    {
        // Request Information
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string? RequestId { get; set; }
        public string? CorrelationId { get; set; }
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserRole { get; set; }
        public string? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Device { get; set; }
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public string? Route { get; set; }
        public string? Controller { get; set; }
        public string? Endpoint { get; set; }
        public string? HttpMethod { get; set; }

        // Request Data (masked)
        public IDictionary<string, string>? QueryParameters { get; set; }
        public IDictionary<string, string>? RouteParameters { get; set; }
        public string? RequestBody { get; set; }
        public IDictionary<string, string>? FormData { get; set; }

        // Exception Information
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? InnerException { get; set; }
        public string? FullStackTrace { get; set; }
        public string? SourceFile { get; set; }
        public int? LineNumber { get; set; }
        public string? Namespace { get; set; }
        public string? Assembly { get; set; }
        public string? Method { get; set; }
        public ErrorCategory Category { get; set; } = ErrorCategory.Unknown;
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Medium;
        public string? RootCause { get; set; }

        // Performance Metrics
        public long? RequestDurationMs { get; set; }
        public long? DatabaseDurationMs { get; set; }
        public long? ApiDurationMs { get; set; }
        public long? MemoryUsageBytes { get; set; }
        public string? ThreadId { get; set; }

        // Database Details
        public string? SqlCommand { get; set; }
        public string? DatabaseProvider { get; set; }
        public string? TransactionId { get; set; }
        public int? RetryCount { get; set; }
        public string? ConnectionStatus { get; set; }

        /// <summary>
        /// Captures the ambient HTTP context into the diagnostic context.
        /// </summary>
        public static ErrorLogContext FromHttpContext(HttpContext context)
        {
            var ctx = new ErrorLogContext
            {
                CorrelationId = context.GetCorrelationId(),
                RequestId = context.TraceIdentifier,
                SessionId = context.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session?.Id,
                IpAddress = context.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = context.Request?.Headers["User-Agent"].ToString(),
                Route = context.Request?.Path,
                HttpMethod = context.Request?.Method,
                Endpoint = $"{context.Request?.Method} {context.Request?.Path}{context.Request?.QueryString}"
            };

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                ctx.UserId = context.User.FindFirst("sub")?.Value;
                ctx.Username = context.User.Identity.Name;
                ctx.UserRole = context.User.FindFirst("role")?.Value;
            }

            return ctx;
        }
    }
}
