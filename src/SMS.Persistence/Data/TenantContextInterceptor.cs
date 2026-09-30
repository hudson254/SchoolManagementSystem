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
    /// Publishes the tenant context on connection OPEN, so that any query on
    /// that connection sees it - including queries that never pass through
    /// Entity Framework.
    ///
    /// <para><b>Why this exists.</b> <see cref="TenantContextDbInterceptor"/>
    /// publishes the context per EF Core command. That covers everything the
    /// ORM issues, but it does not cover raw ADO.NET: a caller that resolves
    /// <c>db.Database.GetDbConnection()</c> and runs its own
    /// <c>DbCommand</c> - reporting queries, OMS number allocation,
    /// maintenance scripts - bypasses the command interceptor completely.
    /// Under row level security such a query then evaluates against whatever
    /// the pooled connection happened to be carrying, which is the exact
    /// cross-tenant failure this whole mechanism exists to prevent.</para>
    ///
    /// <para>Publishing at connection open closes that gap, and it is also
    /// what makes pooled-connection reuse safe by construction: Npgsql rents
    /// a physical connection for a scope and returns it to the pool on close,
    /// so every new rental rewrites the context before anything can read it.
    /// The command interceptor remains as a second layer for the case where a
    /// long-lived connection is reused within a scope.</para>
    /// </summary>
    public class TenantConnectionDbInterceptor : DbConnectionInterceptor
    {
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<TenantConnectionDbInterceptor> _logger;

        public TenantConnectionDbInterceptor(
            ITenantContext tenantContext,
            ILogger<TenantConnectionDbInterceptor> logger)
        {
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <inheritdoc />
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            Publish(connection);
            base.ConnectionOpened(connection, eventData);
        }

        /// <inheritdoc />
        public override Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Publish(connection);
            return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        private void Publish(DbConnection connection)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT set_config('app.tenant_id', @tenant, false)";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "tenant";
                parameter.Value = ResolveTenant();
                command.Parameters.Add(parameter);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Fail CLOSED. Returning without publishing would hand the
                // connection to the caller carrying whatever tenant it was
                // last used by, which is a silent cross-tenant read. An
                // unavailable tenant context must stop the request instead.
                _logger.LogError(
                    ex,
                    "Failed to publish the tenant context when opening a PostgreSQL connection; " +
                    "refusing to hand it out under an unknown tenant.");
                throw;
            }
        }

        /// <summary>
        /// The tenant for this connection, or the empty string when no tenant
        /// is resolved. Empty maps to the all-zero sentinel inside
        /// <c>app.current_tenant_id()</c>, which matches no row - the
        /// fail-closed default.
        /// </summary>
        private string ResolveTenant()
        {
            var tenantId = _tenantContext?.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out _))
            {
                return string.Empty;
            }

            return tenantId;
        }
    }

    /// <summary>
    /// Publishes the authenticated tenant to PostgreSQL as the
    /// <c>app.tenant_id</c> session GUC, which every row level security
    /// policy evaluates through <c>app.current_tenant_id()</c>.
    ///
    /// <para>The write happens on the SAME physical connection immediately
    /// before the command it guards, which is what makes it safe under
    /// connection pooling: a pooled connection may still be carrying the
    /// previous request's tenant, but that value is always overwritten
    /// before the command runs, and never observed by anything.</para>
    ///
    /// <para>Three properties matter, and each of them was a real defect
    /// found while moving the application onto a NOBYPASSRLS role:</para>
    /// <list type="bullet">
    /// <item><b>Fail closed.</b> The original wrapped the write in a
    /// catch-all that logged at Trace and continued. If the write failed the
    /// command still executed - carrying whatever tenant the pooled
    /// connection happened to hold, which could be a different tenant. A
    /// failure to establish the tenant context now aborts the command.</item>
    /// <item><b>Parameterised.</b> The value was spliced into the SQL text.
    /// It is now a command parameter, so a hostile tenant id cannot alter
    /// the statement.</item>
    /// <item><b>Always writes.</b> The original skipped the write entirely
    /// for the tenant-resolution read. Skipping leaves any pre-existing
    /// value in place, which is precisely the leak the first bullet is
    /// about. It now writes an explicitly EMPTY context instead, which
    /// <c>app.current_tenant_id()</c> maps to the all-zero sentinel
    /// ("matches no row"). Tenant resolution still works because
    /// <c>Tenants</c> is deliberately not RLS-protected: it is the
    /// bootstrap registry that establishes the tenant in the first
    /// place.</item>
    /// </list>
    /// </summary>
    public class TenantContextDbInterceptor : DbCommandInterceptor
    {
        /// <summary>The PostgreSQL GUC the RLS policies read.</summary>
        private const string TenantSettingName = "app.tenant_id";

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
            var connection = command.Connection;
            if (connection == null)
            {
                // Nothing to publish onto. Only reachable for a detached
                // command, which EF Core never executes.
                return;
            }

            try
            {
                using var setCmd = connection.CreateCommand();
                setCmd.CommandText = $"SELECT set_config('{TenantSettingName}', @tenant, false)";

                var parameter = setCmd.CreateParameter();
                parameter.ParameterName = "tenant";
                parameter.Value = TenantContextToPublish();
                setCmd.Parameters.Add(parameter);

                setCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Fail CLOSED. Continuing here would execute the guarded
                // command under whatever tenant the connection already
                // carried, which is how a pooled connection serving tenant
                // B could run tenant A's queries.
                _logger.LogError(
                    ex,
                    "Failed to publish the tenant context to PostgreSQL; refusing to execute the " +
                    "command rather than running it under an unknown tenant.");
                throw;
            }
        }

        /// <summary>
        /// The tenant to publish for the command about to run.
        ///
        /// <para>An unresolved tenant publishes the empty string rather than
        /// nothing. Empty is meaningful to <c>app.current_tenant_id()</c>:
        /// it yields the all-zero sentinel, which matches no tenant. Leaving
        /// the setting untouched instead would let a pooled connection's
        /// previous tenant stand, which is the cross-tenant leak this whole
        /// mechanism has to avoid.</para>
        /// </summary>
        private string TenantContextToPublish()
        {
            var tenantId = _tenantContext?.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out _))
            {
                return string.Empty;
            }

            return tenantId;
        }
    }
}