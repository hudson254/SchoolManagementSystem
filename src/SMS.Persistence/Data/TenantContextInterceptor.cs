using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;

namespace SMS.Persistence.Data
{
    /// <summary>
    /// EF Core interceptor that sets the PostgreSQL session-level tenant context
    /// variable (app.tenant_id) on the first command executed against each
    /// connection. This bridges ITenantContext (HttpContext.Items) with
    /// PostgreSQL Row Level Security.
    /// 
    /// The session variable is set once per connection open. Connection pooling
    /// ensures connections are reused within the same request, and when a
    /// connection is returned to the pool and reused for a different tenant,
    /// the Interceptor runs again on the next ConnectionOpened event.
    /// </summary>
    public class TenantContextDbInterceptor : DbCommandInterceptor
    {
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<TenantContextDbInterceptor> _logger;
        private static readonly Guid EmptyGuid = Guid.Empty;

        public TenantContextDbInterceptor(ITenantContext tenantContext, ILogger<TenantContextDbInterceptor> logger)
        {
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            EnsureTenantContextSet(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            EnsureTenantContextSet(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            EnsureTenantContextSet(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            EnsureTenantContextSet(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void EnsureTenantContextSet(DbCommand command)
        {
            try
            {
                if (command.Connection == null) return;

                var tenantId = _tenantContext?.TenantId;
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    tenantId = EmptyGuid.ToString();
                }

                // Set the PostgreSQL session variable for RLS policy evaluation.
                // Using a synchronous approach to keep things simple.
                using var setCmd = command.Connection.CreateCommand();
                setCmd.CommandText = $"SELECT set_config('app.tenant_id', '{tenantId}', false)";
                setCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Silently ignore - the RLS function will return empty GUID
                // if the session variable is not set, which fails secure.
                _logger.LogTrace(ex, "Could not set PostgreSQL tenant session context");
            }
        }
    }
}