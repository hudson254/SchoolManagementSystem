using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.API.Options;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SMS.API.Middleware
{
    /// <summary>
    /// Per-client-IP rate limiting using an in-memory sliding window.
    /// The permit limit, window length, and ban duration are configurable via
    /// the "RateLimiting" section of appsettings.json (see
    /// <see cref="RateLimitingOptions"/>). The in-memory cache is suitable for
    /// a single-instance LAN deployment; a distributed cache (e.g. Redis) is
    /// required for multi-instance scaling.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitingOptions _options;

        public RateLimitingMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<RateLimitingMiddleware> logger,
            IOptions<RateLimitingOptions> options)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.Value ?? "/";
            var key = $"ratelimit_{clientIp}_{path}";

            var window = TimeSpan.FromMinutes(_options.WindowMinutes);

            // Check if client is banned
            var banKey = $"banned_{clientIp}";
            if (_cache.TryGetValue(banKey, out bool _))
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = _options.BanDurationMinutes.ToString();
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Get current request count
            if (_cache.TryGetValue(key, out int requestCount))
            {
                if (requestCount >= _options.PermitLimit)
                {
                    // Ban the client
                    _cache.Set(banKey, true, TimeSpan.FromMinutes(_options.BanDurationMinutes));
                    _cache.Remove(key);

                    _logger.LogWarning("Client {ClientIp} has been banned for exceeding rate limit on {Path}", clientIp, path);

                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers["Retry-After"] = _options.BanDurationMinutes.ToString();
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }

                _cache.Set(key, requestCount + 1, window);
            }
            else
            {
                _cache.Set(key, 1, window);
            }

            await _next(context);
        }
    }
}

