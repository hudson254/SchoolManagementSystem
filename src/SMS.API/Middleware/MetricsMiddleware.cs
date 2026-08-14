using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SMS.API.Middleware
{
    /// <summary>
    /// Middleware that exposes a /metrics endpoint in Prometheus text format.
    /// This is a lightweight, dependency-free implementation suitable for the
    /// LAN-only deployment. It tracks:
    ///   - HTTP request count by method and status code
    ///   - HTTP request duration in milliseconds
    ///   - Active requests gauge
    ///   - Authentication failure count (without exposing credentials)
    ///
    /// No sensitive data (passwords, tokens, personal info) is exposed.
    /// The endpoint is intended to be scraped by Prometheus on the internal
    /// Docker network only. In production, restrict access to the monitoring
    /// VLAN via nginx or firewall rules.
    /// </summary>
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MetricsMiddleware> _logger;

        // Metrics counters (thread-safe via Interlocked)
        private static long _totalRequests;
        private static long _activeRequests;
        private static long _authFailures;
        private static long _totalErrors;

        // Duration tracking
        private static readonly object _durationLock = new();
        private static double _totalDurationMs;
        private static long _durationCount;

        // Per-status counters
        private static long _status2xx;
        private static long _status3xx;
        private static long _status4xx;
        private static long _status5xx;

        // Per-method counters
        private static long _getRequests;
        private static long _postRequests;
        private static long _putRequests;
        private static long _deleteRequests;
        private static long _patchRequests;
        private static long _otherRequests;

        private static readonly DateTime _startTime = DateTime.UtcNow;

        public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only track metrics for API routes, not static files
            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Path != "/health" &&
                context.Request.Path != "/metrics")
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _activeRequests);

            // Track method
            IncrementMethodCounter(context.Request.Method);

            try
            {
                await _next(context);

                // Track status code
                IncrementStatusCounter(context.Response.StatusCode);

                // Track auth failures (401 Unauthorized)
                if (context.Response.StatusCode == 401)
                {
                    Interlocked.Increment(ref _authFailures);
                }

                // Track 5xx errors
                if (context.Response.StatusCode >= 500)
                {
                    Interlocked.Increment(ref _totalErrors);
                }
            }
            catch (Exception)
            {
                Interlocked.Increment(ref _totalErrors);
                Interlocked.Increment(ref _status5xx);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                Interlocked.Decrement(ref _activeRequests);

                lock (_durationLock)
                {
                    _totalDurationMs += stopwatch.Elapsed.TotalMilliseconds;
                    _durationCount++;
                }
            }
        }

        private static void IncrementMethodCounter(string method)
        {
            switch (method.ToUpperInvariant())
            {
                case "GET": Interlocked.Increment(ref _getRequests); break;
                case "POST": Interlocked.Increment(ref _postRequests); break;
                case "PUT": Interlocked.Increment(ref _putRequests); break;
                case "DELETE": Interlocked.Increment(ref _deleteRequests); break;
                case "PATCH": Interlocked.Increment(ref _patchRequests); break;
                default: Interlocked.Increment(ref _otherRequests); break;
            }
        }

        private static void IncrementStatusCounter(int statusCode)
        {
            if (statusCode >= 200 && statusCode < 300) Interlocked.Increment(ref _status2xx);
            else if (statusCode >= 300 && statusCode < 400) Interlocked.Increment(ref _status3xx);
            else if (statusCode >= 400 && statusCode < 500) Interlocked.Increment(ref _status4xx);
            else if (statusCode >= 500) Interlocked.Increment(ref _status5xx);
        }

        /// <summary>
        /// Generates the Prometheus metrics text for the /metrics endpoint.
        /// </summary>
        public static string GenerateMetrics()
        {
            var total = Interlocked.Read(ref _totalRequests);
            var active = Interlocked.Read(ref _activeRequests);
            var authFailures = Interlocked.Read(ref _authFailures);
            var errors = Interlocked.Read(ref _totalErrors);
            var s2xx = Interlocked.Read(ref _status2xx);
            var s3xx = Interlocked.Read(ref _status3xx);
            var s4xx = Interlocked.Read(ref _status4xx);
            var s5xx = Interlocked.Read(ref _status5xx);
            var get = Interlocked.Read(ref _getRequests);
            var post = Interlocked.Read(ref _postRequests);
            var put = Interlocked.Read(ref _putRequests);
            var del = Interlocked.Read(ref _deleteRequests);
            var patch = Interlocked.Read(ref _patchRequests);
            var other = Interlocked.Read(ref _otherRequests);

            double avgDurationMs = 0;
            long durationCount = 0;
            lock (_durationLock)
            {
                if (_durationCount > 0)
                {
                    avgDurationMs = _totalDurationMs / _durationCount;
                    durationCount = _durationCount;
                }
            }

            var uptime = (DateTime.UtcNow - _startTime).TotalSeconds;

            return $@"# HELP sms_http_requests_total Total HTTP requests
# TYPE sms_http_requests_total counter
sms_http_requests_total {total}
# HELP sms_http_requests_active Current active requests
# TYPE sms_http_requests_active gauge
sms_http_requests_active {active}
# HELP sms_http_request_duration_ms HTTP request duration in milliseconds
# TYPE sms_http_request_duration_ms gauge
sms_http_request_duration_ms {avgDurationMs:F2}
# HELP sms_http_request_duration_ms_count Number of requests measured for duration
# TYPE sms_http_request_duration_ms_count counter
sms_http_request_duration_ms_count {durationCount}
# HELP sms_http_requests_by_method_total HTTP requests by method
# TYPE sms_http_requests_by_method_total counter
sms_http_requests_by_method_total{{method=""GET""}} {get}
sms_http_requests_by_method_total{{method=""POST""}} {post}
sms_http_requests_by_method_total{{method=""PUT""}} {put}
sms_http_requests_by_method_total{{method=""DELETE""}} {del}
sms_http_requests_by_method_total{{method=""PATCH""}} {patch}
sms_http_requests_by_method_total{{method=""OTHER""}} {other}
# HELP sms_http_requests_by_status_total HTTP requests by status category
# TYPE sms_http_requests_by_status_total counter
sms_http_requests_by_status_total{{status=""2xx""}} {s2xx}
sms_http_requests_by_status_total{{status=""3xx""}} {s3xx}
sms_http_requests_by_status_total{{status=""4xx""}} {s4xx}
sms_http_requests_by_status_total{{status=""5xx""}} {s5xx}
# HELP sms_auth_failures_total Authentication failures
# TYPE sms_auth_failures_total counter
sms_auth_failures_total {authFailures}
# HELP sms_errors_total Total HTTP 5xx errors
# TYPE sms_errors_total counter
sms_errors_total {errors}
# HELP sms_uptime_seconds Application uptime in seconds
# TYPE sms_uptime_seconds gauge
sms_uptime_seconds {uptime:F0}
# HELP sms_build_info Build information
# TYPE sms_build_info gauge
sms_build_info{{version=""1.0.0"",environment=""production""}} 1
";
        }
    }
}
