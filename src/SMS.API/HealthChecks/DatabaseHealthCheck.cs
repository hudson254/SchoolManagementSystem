using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SMS.Persistence.Data;

namespace SMS.API.HealthChecks
{
    /// <summary>
    /// Custom health check that verifies PostgreSQL database connectivity
    /// by executing a simple query against the ApplicationDbContext.
    /// This does not require the AspNetCore.Diagnostics.HealthChecks.EntityFrameworkCore
    /// NuGet package.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(ApplicationDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Execute a simple query to verify database connectivity
                // Uses a cancellation token with a timeout to prevent hanging
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                var canConnect = await _dbContext.Database.CanConnectAsync(linkedCts.Token);

                if (canConnect)
                {
                    return HealthCheckResult.Healthy("Database is reachable and responding.");
                }

                return HealthCheckResult.Unhealthy("Database returned false from CanConnectAsync.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Database health check timed out after 10 seconds.");
                return HealthCheckResult.Unhealthy("Database health check timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database health check failed.");
                return HealthCheckResult.Unhealthy("Database is not reachable.", ex);
            }
        }
    }
}
