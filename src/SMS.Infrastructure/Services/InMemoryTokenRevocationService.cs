using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// In-memory implementation of <see cref="ITokenRevocationService"/>.
    /// Suitable for single-instance LAN deployments. Revoked JWT identifiers
    /// (jti) are stored in an in-memory cache with an absolute expiry equal
    /// to the access token's lifetime, so the deny-list cannot grow
    /// unbounded and entries auto-expire once the corresponding token would
    /// have expired anyway.
    ///
    /// If multi-instance horizontal scaling is introduced later, replace this
    /// with a distributed cache (e.g. Redis-backed) implementation so all
    /// instances share the same deny-list.
    /// </summary>
    public class InMemoryTokenRevocationService : ITokenRevocationService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<InMemoryTokenRevocationService> _logger;

        /// <summary>
        /// Default lifetime for a deny-list entry. Should match the access
        /// token expiration so revoked entries auto-expire when the token
        /// would have expired anyway. Configurable via DI if needed.
        /// </summary>
        private const int DefaultRevocationTtlMinutes = 60;

        public InMemoryTokenRevocationService(
            IMemoryCache cache,
            ILogger<InMemoryTokenRevocationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task RevokeAccessTokenAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
            {
                _logger.LogWarning("RevokeAccessTokenAsync called with empty jti");
                return Task.CompletedTask;
            }

            // Mark in the cache with an absolute expiration equal to the
            // access token lifetime. After that the token would be invalid
            // anyway, so the deny-list entry can be evicted.
            _cache.Set(DenyListKey(jti), true, TimeSpan.FromMinutes(DefaultRevocationTtlMinutes));

            _logger.LogInformation("Access token revoked (jti={Jti})", jti);
            return Task.CompletedTask;
        }

        public Task<bool> IsAccessTokenRevokedAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return Task.FromResult(false);

            return Task.FromResult(_cache.TryGetValue(DenyListKey(jti), out _));
        }

        private static string DenyListKey(string jti) => $"revoked_jti_{jti}";
    }
}
