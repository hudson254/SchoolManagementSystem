using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SMS.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly int _limitPerMinute = 60;
        private readonly int _banDurationMinutes = 5;

        public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.Value ?? "/";
            var key = $"ratelimit_{clientIp}_{path}";

            // Check if client is banned
            var banKey = $"banned_{clientIp}";
            if (_cache.TryGetValue(banKey, out bool _))
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = _banDurationMinutes.ToString();
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Get current request count
            if (_cache.TryGetValue(key, out int requestCount))
            {
                if (requestCount >= _limitPerMinute)
                {
                    // Ban the client
                    _cache.Set(banKey, true, TimeSpan.FromMinutes(_banDurationMinutes));
                    _cache.Remove(key);

                    _logger.LogWarning("Client {ClientIp} has been banned for exceeding rate limit on {Path}", clientIp, path);

                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers["Retry-After"] = _banDurationMinutes.ToString();
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }

                _cache.Set(key, requestCount + 1, TimeSpan.FromMinutes(1));
            }
            else
            {
                _cache.Set(key, 1, TimeSpan.FromMinutes(1));
            }

            await _next(context);
        }
    }
}

