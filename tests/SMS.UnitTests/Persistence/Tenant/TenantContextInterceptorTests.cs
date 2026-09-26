using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.Persistence.Tenant
{
    /// <summary>
    /// Guards the runtime wiring of the PostgreSQL tenant RLS context.
    ///
    /// <para>Two defects were found while moving the application from a
    /// SUPERUSER/BYPASSRLS role onto a least-privilege NOBYPASSRLS role, and
    /// both were invisible while the role bypassed RLS:</para>
    ///
    /// <list type="number">
    /// <item>The production <c>AddDbContext</c> registration in
    /// <c>Program.cs</c> never registered <see cref="TenantContextDbInterceptor"/>,
    /// so <c>app.tenant_id</c> was never written to the PostgreSQL session. The
    /// tenant policies then evaluated against the all-zero sentinel and every
    /// tenant-scoped query - including the ASP.NET Identity login lookup -
    /// returned zero rows, so authentication failed with HTTP 401.</item>
    /// <item>The interceptor also wrote the context onto the one query that must
    /// run <i>without</i> a context: the <c>Tenants</c> registry read performed by
    /// <c>TenantResolutionMiddleware</c> before the tenant is known. That made
    /// tenant resolution fail and the API answer HTTP 400 "Invalid tenant"
    /// for every request.</item>
    /// </list>
    ///
    /// <para>These tests pin the behaviour of both, so a future refactor cannot
    /// silently reintroduce either failure.</para>
    /// </summary>
    public class TenantContextInterceptorTests
    {
        private const string TenantA = "aaaaaaaa-0000-0000-0000-000000000001";

        /// <summary>
        /// Minimal <see cref="DbCommand"/> stand-in that records whether
        /// <c>set_config('app.tenant_id', ...)</c> was executed. No database is
        /// required: the point under test is <i>whether</i> the interceptor
        /// chooses to set the context, not what PostgreSQL does with it.
        /// </summary>
        private sealed class RecordingCommand : DbCommand
        {
            public List<string> Executed { get; } = new();

            public RecordingCommand(DbConnection? connection) => DbConnection = connection;

            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection { get; } = null!;
            protected override DbTransaction? DbTransaction { get; set; }
            public override void Cancel() { }
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
            public override int ExecuteNonQuery()
            {
                Executed.Add(CommandText);
                return 0;
            }
            public override object? ExecuteScalar() => null;
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
                => throw new NotSupportedException();
        }

        /// <summary>
        /// Minimal <see cref="DbConnection"/> whose <see cref="CreateDbCommand"/>
        /// hands back the recording command the interceptor writes through.
        /// </summary>
        private sealed class RecordingConnection : DbConnection
        {
            public RecordingCommand Command { get; }
            public RecordingConnection() => Command = new RecordingCommand(this);
            public override string ConnectionString { get; set; } = "Host=localhost";
            public override string Database => "test";
            public override string DataSource => "localhost";
            public override string ServerVersion => "16.0";
            public override ConnectionState State => ConnectionState.Open;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbCommand CreateDbCommand() => Command;
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
                => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { }
        }

        /// <summary>
        /// Builds an interceptor bound to a recording connection, plus a command
        /// attached to that connection, which is what the interceptor requires
        /// before it will publish the session context.
        /// </summary>
        private static (RecordingConnection Connection, RecordingCommand Command, TenantContextDbInterceptor Interceptor)
            Arrange(string tenantId, string sql)
        {
            var connection = new RecordingConnection();
            var command = new RecordingCommand(connection) { CommandText = sql };
            var tenant = new Mock<ITenantContext>();
            tenant.Setup(t => t.TenantId).Returns(tenantId);
            var interceptor = new TenantContextDbInterceptor(tenant.Object, NullLogger<TenantContextDbInterceptor>.Instance);
            return (connection, command, interceptor);
        }

        private static RecordingCommand Query(string sql) => new(null) { CommandText = sql };

        private const string TenantResolutionSql =
            "SELECT t.id, t.\"Name\" FROM \"Tenants\" AS t " +
            "WHERE t.id = @p AND t.\"IsActive\" AND NOT (t.is_deleted) LIMIT 1";

        /// <summary>
        /// The ordinary case: with a resolved tenant, every query must publish the
        /// context so the RLS policies have a tenant to evaluate.
        /// </summary>
        [Fact]
        public void ReaderExecuting_WithResolvedTenant_SetsSessionContext()
        {
            var (connection, command, interceptor) = Arrange(TenantA, "SELECT * FROM \"Courses\"");

            _ = interceptor.ReaderExecuting(command, default!, default);

            connection.Command.Executed.Should().ContainSingle()
                .Which.Should().Contain("set_config('app.tenant_id'")
                .And.Contain(TenantA);
        }

        [Fact]
        public void NonQueryExecuting_WithResolvedTenant_SetsSessionContext()
        {
            var (connection, command, interceptor) = Arrange(TenantA, "INSERT INTO \"Courses\" (id) VALUES (@p)");

            _ = interceptor.NonQueryExecuting(command, default!, default);

            connection.Command.Executed.Should().ContainSingle()
                .Which.Should().Contain(TenantA);
        }

        /// <summary>
        /// The tenant-resolution read must run BEFORE any context exists, so the
        /// interceptor must not write one. Writing the all-zero sentinel here is
        /// what produced HTTP 400 "Invalid tenant" on every request.
        /// </summary>
        [Fact]
        public void ReaderExecuting_TenantResolutionQuery_WithoutContext_DoesNotSetContext()
        {
            var (connection, command, interceptor) = Arrange(string.Empty, TenantResolutionSql);

            _ = interceptor.ReaderExecuting(command, default!, default);

            connection.Command.Executed.Should().BeEmpty(
                "the Tenants registry read establishes the context; it must see no context");
        }

        /// <summary>
        /// The exemption is scoped to the unresolved case. Once a tenant IS
        /// resolved, a <c>Tenants</c> read is an ordinary tenant-scoped query and
        /// must be filtered like everything else.
        /// </summary>
        [Fact]
        public void ReaderExecuting_TenantsQuery_WithResolvedContext_SetsContext()
        {
            var (connection, command, interceptor) = Arrange(TenantA, "SELECT t.id FROM \"Tenants\" AS t WHERE t.id = @p");

            _ = interceptor.ReaderExecuting(command, default!, default);

            connection.Command.Executed.Should().ContainSingle()
                .Which.Should().Contain(TenantA);
        }

        /// <summary>
        /// The exemption must not extend to tenant-owned data. Any other table
        /// still has to publish the context, otherwise RLS silently falls back to
        /// the sentinel and hides the tenant's own rows.
        /// </summary>
        [Theory]
        [InlineData("SELECT * FROM \"Courses\"")]
        [InlineData("SELECT * FROM sms_requests")]
        [InlineData("SELECT * FROM \"AspNetUsers\"")]
        [InlineData("DELETE FROM \"Notifications\"")]
        public void ReaderExecuting_OtherTables_WithResolvedTenant_SetsContext(string sql)
        {
            var (connection, command, interceptor) = Arrange(TenantA, sql);

            _ = interceptor.ReaderExecuting(command, default!, default);

            connection.Command.Executed.Should().ContainSingle()
                .Which.Should().Contain(TenantA);
        }

        /// <summary>
        /// A missing context must still fail closed. The interceptor substitutes
        /// the all-zero sentinel rather than leaving the session variable unset,
        /// and <c>app.current_tenant_id()</c> maps that sentinel to "matches no row".
        /// </summary>
        [Fact]
        public async Task ReaderExecutingAsync_WithoutTenant_StillSetsSentinelContext()
        {
            var (connection, command, interceptor) = Arrange(string.Empty, "SELECT * FROM \"Courses\"");

            await interceptor.ReaderExecutingAsync(command, default!, default, CancellationToken.None);

            connection.Command.Executed.Should().ContainSingle()
                .Which.Should().Contain("00000000-0000-0000-0000-000000000000");
        }

        /// <summary>
        /// Fail-closed: with no connection there is nothing to set. The interceptor
        /// must return quietly and must never invent a tenant of its own.
        /// </summary>
        [Fact]
        public void ReaderExecuting_WithNullConnection_DoesNotThrow()
        {
            var connection = new RecordingConnection();
            var tenant = new Mock<ITenantContext>();
            tenant.Setup(t => t.TenantId).Returns(TenantA);
            var interceptor = new TenantContextDbInterceptor(tenant.Object, NullLogger<TenantContextDbInterceptor>.Instance);
            var detached = new RecordingCommand(null) { CommandText = "SELECT 1" };

            var act = () => interceptor.ReaderExecuting(detached, default!, default);

            act.Should().NotThrow();
            connection.Command.Executed.Should().BeEmpty("no connection means nothing can be set");
        }
    }
}