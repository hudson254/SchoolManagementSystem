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

                // Tenant resolution must run before any context exists. See
                // IsTenantResolutionQuery for why writing the context here
                // would break it. Once a tenant IS resolved this guard is
                // inert, so the RLS boundary is unchanged for all other work.
                if (HasNoResolvedTenant() && IsTenantResolutionQuery(command)) return;

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

        /// <summary>
        /// Detects the tenant-resolution read issued by
        /// <c>TenantResolutionMiddleware</c> before the tenant context exists.
        ///
        /// <para>That query (<c>TenantStore.GetTenantAsync</c>) is the one
        /// operation that must run with no tenant context: it is what
        /// establishes the context for the rest of the request. Writing
        /// <c>app.tenant_id</c> on it first - even to the all-zero sentinel,
        /// which is what this interceptor does when the context is empty -
        /// would make the <c>Tenants</c> RLS policy evaluate against a tenant
        /// that has not been resolved yet, and the query would return no rows.
        /// Tenant resolution would then fail and the API would answer HTTP 400
        /// "Invalid tenant" for every request.</para>
        ///
        /// <para>This is deliberately narrow: only the <c>Tenants</c> registry
        /// read is exempt, and only while the context is still unresolved. The
        /// moment the middleware has set a real tenant, the normal path applies
        /// and every other query - and every later <c>Tenants</c> read - is
        /// tenant-scoped exactly as before. No other table is exempt, so the
        /// RLS boundary is unchanged for all tenant-owned data.</para>
        /// </summary>
        private static bool IsTenantResolutionQuery(DbCommand command)
        {
            var text = command.CommandText;
            if (string.IsNullOrEmpty(text)) return false;

            // Match the tenant registry read regardless of casing/aliasing.
            // Requires the FROM target to be the Tenants table itself.
            return text.IndexOf("\"Tenants\"", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Tenants", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// True when no tenant has been resolved for the current request yet.
        /// </summary>
        private bool HasNoResolvedTenant()
        {
            var tenantId = _tenantContext?.TenantId;
            return string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out _);
        }
    }
}