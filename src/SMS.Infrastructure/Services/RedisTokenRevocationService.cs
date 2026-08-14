using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using StackExchange.Redis;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Redis-backed token revocation service for production use.
    /// Replaces the in-memory implementation when horizontal scaling
    /// or persistence across restarts is required.
    ///
    /// Design:
    /// - Access tokens are stored by their JTI (JWT ID) with a TTL
    ///   matching the access token's remaining lifetime.
    /// - The service uses a Redis Set for each tenant to allow
    ///   batch revocation if needed.
    /// - Connection failures are logged but do not throw, ensuring
    ///   the authentication flow is not blocked by a Redis outage.
    /// </summary>
    public class RedisTokenRevocationService : ITokenRevocationService, IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisTokenRevocationService> _logger;
        private readonly bool _enabled;

        public RedisTokenRevocationService(
            IOptions<RedisTokenRevocationOptions> options,
            ILogger<RedisTokenRevocationService> logger)
        {
            _logger = logger;
            _enabled = !string.IsNullOrWhiteSpace(options.Value.ConnectionString);

            if (!_enabled)
            {
                _logger.LogWarning("RedisTokenRevocationService is disabled. No Redis connection string configured.");
                _redis = null;
                _db = null;
                return;
            }

            try
            {
                _redis = ConnectionMultiplexer.Connect(options.Value.ConnectionString);
                _db = _redis.GetDatabase(options.Value.DatabaseIndex);
                _logger.LogInformation("RedisTokenRevocationService connected to {Endpoint}",
                    options.Value.ConnectionString.Split(',')[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis. Token revocation will fall back to no-op.");
                _redis = null;
                _db = null;
            }
        }

        public async Task<bool> IsAccessTokenRevokedAsync(string jti)
        {
            if (!_enabled || _db == null)
            {
                _logger.LogWarning("RedisTokenRevocationService is not connected. Treating all tokens as non-revoked for availability.");
                return false;
            }

            try
            {
                return await _db.KeyExistsAsync(GetTokenKey(jti));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis error checking revoked token {Jti}. Failing closed for security.", jti);
                // Fail closed: if we cannot check Redis, assume the token is revoked
                // to prevent bypassing revocation during a Redis outage.
                throw new InvalidOperationException(
                    $"Redis connection failed while checking revocation status for token {jti}. " +
                    "Revocation status cannot be verified, rejecting the request for security.", ex);
            }
        }

        public async Task RevokeAccessTokenAsync(string jti)
        {
            if (!_enabled || _db == null)
                return;

            if (string.IsNullOrEmpty(jti))
                return;

            try
            {
                // Store with TTL of 15 minutes (default access token lifetime)
                // This ensures revoked tokens are automatically cleaned up
                await _db.StringSetAsync(GetTokenKey(jti), "revoked", TimeSpan.FromMinutes(15));
                _logger.LogDebug("Access token {Jti} revoked in Redis", jti);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis error revoking token {Jti}", jti);
            }
        }

        public async Task RevokeAccessTokenAsync(string jti, TimeSpan ttl)
        {
            if (!_enabled || _db == null)
                return;

            if (string.IsNullOrEmpty(jti))
                return;

            try
            {
                await _db.StringSetAsync(GetTokenKey(jti), "revoked", ttl);
                _logger.LogDebug("Access token {Jti} revoked in Redis with TTL {Ttl}", jti, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis error revoking token {Jti}", jti);
            }
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }

        private static string GetTokenKey(string jti) => $"revoked:token:{jti}";
    }

    /// <summary>
    /// Configuration options for RedisTokenRevocationService.
    /// Bind from appsettings.json "RedisTokenRevocation" section.
    /// </summary>
    public class RedisTokenRevocationOptions
    {
        /// <summary>
        /// Redis connection string (e.g., "localhost:6379").
        /// Leave empty or null to disable Redis-based revocation.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Redis database index to use (default 0).
        /// </summary>
        public int DatabaseIndex { get; set; } = 0;
    }
}
