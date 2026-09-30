using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// A dedicated PostgreSQL database, provisioned exactly the way a fresh
    /// production deployment is: least-privilege roles, grants applied by the
    /// bootstrap role, migrations applied by the migration role, and the
    /// application connecting as the NOBYPASSRLS runtime role.
    ///
    /// <para>It is a separate database so this suite can never interfere with
    /// <c>sms_test</c> (shared with SMS.ApiTests) or with
    /// <c>sms_enrollment_state_test</c>, and so a failure here is
    /// unambiguous.</para>
    ///
    /// <para>Provisioning runs from scratch on every test run rather than
    /// reusing a leftover database. A leftover would carry whatever ownership
    /// the roles happened to have last time, and reusing it would quietly
    /// test the wrong arrangement.</para>
    /// </summary>
    public class TenantRowLevelSecurityFixture : IAsyncLifetime
    {
        public const string DatabaseName = "sms_rls_test";

        /// <summary>The tenant every test treats as "us".</summary>
        public static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

        /// <summary>The tenant every test treats as "them".</summary>
        public static readonly Guid TenantB = Guid.Parse("99999999-9999-9999-9999-999999999999");

        /// <summary>A tenant id that exists in no table.</summary>
        public static readonly Guid TenantC = Guid.Parse("77777777-7777-7777-7777-777777777777");

        private static readonly SemaphoreSlim InitLock = new(1, 1);

        private static string Env(string name, string fallback) =>
            Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        public static string Host => Env("SMS_TEST_PG_HOST", "localhost");
        public static int Port => int.Parse(Env("SMS_TEST_PG_PORT", "5433"), CultureInfo.InvariantCulture);

        /// <summary>
        /// Bootstrap role. Used only to create the database and hand its
        /// schema to the migration role. The application never connects as it.
        /// </summary>
        public static string AdminUser => Env("SMS_TEST_PG_ADMIN_USER", "testuser");
        public static string AdminPassword => Env("SMS_TEST_PG_ADMIN_PASSWORD", "testpass123");

        /// <summary>
        /// The least-privilege role every assertion in this suite runs as.
        /// </summary>
        public static string RuntimeUser => Env("SMS_TEST_PG_USER", "sms_app");
        public static string RuntimePassword => Env("SMS_TEST_PG_PASSWORD", "testapp123");

        /// <summary>
        /// The DDL role. Owns the tables and is therefore exempt from their
        /// policies; used only for migrations and for ground-truth reads.
        /// </summary>
        public static string MigrationUser => Env("SMS_TEST_PG_MIGRATION_USER", "sms_migration");
        public static string MigrationPassword => Env("SMS_TEST_PG_MIGRATION_PASSWORD", "testmigr123");

        public static string RuntimeConnectionString =>
            $"Host={Host};Port={Port};Database={DatabaseName};Username={RuntimeUser};Password={RuntimePassword};" +
            "Minimum Pool Size=1;Maximum Pool Size=20;Include Error Detail=true;";

        private static string AdminConnectionString(string database) =>
            $"Host={Host};Port={Port};Database={database};Username={AdminUser};Password={AdminPassword};";

        private static string MigrationConnectionString =>
            $"Host={Host};Port={Port};Database={DatabaseName};Username={MigrationUser};Password={MigrationPassword};";

        public Guid TenantALaneId { get; private set; }
        public Guid TenantBLaneId { get; private set; }

        public async Task InitializeAsync()
        {
            await InitLock.WaitAsync();
            try
            {
                // Provisioning must be idempotent AND safe to run from two
                // fixture instances at once: xUnit creates one
                // IClassFixture instance per test class, and both classes use
                // this fixture. An unconditional DROP would tear the database
                // out from under the other class mid-run, which is exactly
                // what happened before this guard existed.
                if (!await IsProvisionedAsync())
                {
                    await RecreateDatabaseAsync();
                    await ApplyLeastPrivilegeGrantsAsync();
                    await DatabaseMigrationRunner.ApplyAsync(MigrationConnectionString);
                }

                // Re-apply the Tenants narrowing AFTER migrations.
                //
                // The grants above set ALTER DEFAULT PRIVILEGES so that every
                // table sms_migration goes on to create is immediately usable
                // by the runtime role - which necessarily includes "Tenants",
                // because default privileges cannot name an exception. In
                // production the equivalent re-run is
                // docker/grant-least-privilege-privileges.sql, which the
                // documented deployment flow executes after migrating. Without
                // this step the suite would assert a privilege boundary that
                // the real arrangement does not actually have.
                await ApplyTenantsRegistryGrantsAsync();

                await SeedAsync();
            }
            finally
            {
                InitLock.Release();
            }
        }

        /// <summary>
        /// True when the database already exists and the RLS migration has
        /// been applied, i.e. it is in the arrangement this suite needs.
        /// </summary>
        private static async Task<bool> IsProvisionedAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(AdminConnectionString(DatabaseName));
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(
                    @"select count(*) from ""__EFMigrationsHistory""
                       where ""MigrationId"" = '20260930120000_EnableTenantRowLevelSecurity'", connection);

                return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
            }
            catch (PostgresException)
            {
                // Database does not exist yet. Provision it.
                return false;
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// A context bound to <paramref name="tenantId"/>, wired exactly the
        /// way the runtime DI root wires one: NOBYPASSRLS role, tenant
        /// context, and both tenant interceptors.
        /// </summary>
        public ApplicationDbContext CreateContext(Guid? tenantId = null)
            => BuildContext(tenantId?.ToString());

        /// <summary>
        /// A context whose tenant is unset or malformed - the "no tenant
        /// context" case the security model must fail closed on.
        /// </summary>
        public ApplicationDbContext CreateContextWithRawTenantId(string rawTenantId)
            => BuildContext(rawTenantId);

        private ApplicationDbContext BuildContext(string? tenantId)
        {
            var tenant = new Mock<ITenantContext>();
            tenant.Setup(x => x.TenantId).Returns(tenantId ?? string.Empty);
            tenant.Setup(x => x.TenantName).Returns(tenantId is null ? string.Empty : $"Tenant {tenantId}");
            tenant.Setup(x => x.ConnectionString).Returns(string.Empty);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(RuntimeConnectionString, npgsql => npgsql.CommandTimeout(60))
                .AddInterceptors(new TenantContextDbInterceptor(
                    tenant.Object, NullLogger<TenantContextDbInterceptor>.Instance))
                .AddInterceptors(new TenantConnectionDbInterceptor(
                    tenant.Object, NullLogger<TenantConnectionDbInterceptor>.Instance))
                .Options;

            return new ApplicationDbContext(
                options,
                new Mock<ICurrentUserService>().Object,
                tenant.Object);
        }

        /// <summary>
        /// Runs a scalar query over raw ADO.NET, publishing the tenant first.
        /// Used for the raw-SQL proofs, which must not go through Entity
        /// Framework at all.
        /// </summary>
        public async Task<long> ExecuteRawScalarAsync(
            string sql, Guid tenantId, params (string Name, object Value)[] parameters)
        {
            await using var connection = new NpgsqlConnection(RuntimeConnectionString);
            await connection.OpenAsync();
            await PublishTenantAsync(connection, tenantId);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);

            var result = await command.ExecuteScalarAsync();
            return result is null or DBNull ? 0L : Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        /// <summary>Runs a non-query statement over raw ADO.NET.</summary>
        public async Task<int> ExecuteRawNonQueryAsync(
            string sql, Guid tenantId, params (string Name, object Value)[] parameters)
        {
            await using var connection = new NpgsqlConnection(RuntimeConnectionString);
            await connection.OpenAsync();
            await PublishTenantAsync(connection, tenantId);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Runs a scalar query over raw ADO.NET and returns it as text. Used
        /// for catalog queries such as the role-attribute assertion, whose
        /// result is a string rather than a count.
        /// </summary>
        public async Task<string> ExecuteRawTextAsync(string sql, Guid tenantId)
        {
            await using var connection = new NpgsqlConnection(RuntimeConnectionString);
            await connection.OpenAsync();
            await PublishTenantAsync(connection, tenantId);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            return Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
        }

        /// <summary>
        /// Publishes the tenant onto a raw connection so the policies evaluate
        /// against it. A plain NpgsqlConnection is outside Entity Framework,
        /// so neither interceptor runs for it.
        /// </summary>
        public static async Task PublishTenantAsync(NpgsqlConnection connection, Guid tenantId)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT set_config('app.tenant_id', @tenant, false)";
            command.Parameters.AddWithValue("tenant", tenantId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Runs a non-query statement over raw ADO.NET as the OWNER
        /// (sms_migration). Used to prove that the migration role is itself
        /// constrained: it may perform DDL against the tables it owns, but it
        /// is still NOSUPERUSER / NOCREATEROLE / NOCREATEDB.
        /// </summary>
        public async Task ExecuteAsOwnerNonQueryAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(MigrationConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Reads a scalar as the OWNER of the tables, bypassing policies
        /// entirely. Establishes ground truth so that a "cannot see" result is
        /// distinguishable from "was never written".
        /// </summary>
        public async Task<long> ExecuteAsOwnerScalarAsync(string sql, params (string Name, object Value)[] parameters)
        {
            await using var connection = new NpgsqlConnection(
                $"Host={Host};Port={Port};Database={DatabaseName};Username={MigrationUser};Password={MigrationPassword};");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);

            var result = await command.ExecuteScalarAsync();
            return result is null or DBNull ? 0L : Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Drops (if present) and recreates the dedicated test database.
        ///
        /// <para><c>NpgsqlConnection.ClearAllPools()</c> is called immediately
        /// before the drop. Npgsql keeps a <b>process-wide static</b>
        /// connection pool, so a pooled connector created for
        /// <c>sms_rls_test</c> survives this database being destroyed and
        /// recreated, and the next rental hands back a connector whose backend
        /// is gone. PostgreSQL therefore still counts the doomed database as
        /// referenced, and the failure surfaces much later as an unrelated
        /// "database is being accessed by other users" or a connection reset
        /// inside a test that has nothing to do with teardown. Clearing the
        /// pools makes the drop total: no live or pooled reference to the old
        /// database can outlive it.</para>
        /// </summary>
        private static async Task RecreateDatabaseAsync()
        {
            // No live or pooled connection may outlive the database below.
            NpgsqlConnection.ClearAllPools();

            await using var connection = new NpgsqlConnection(AdminConnectionString("postgres"));
            await connection.OpenAsync();

            await using (var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE)", connection))
            {
                await drop.ExecuteNonQueryAsync();
            }

            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{DatabaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Narrows "Tenants" to SELECT for the runtime role, after migrations.
        /// Mirrors section 2a of docker/grant-least-privilege-privileges.sql.
        /// </summary>
        private static async Task ApplyTenantsRegistryGrantsAsync()
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString(DatabaseName));
            await connection.OpenAsync();

            const string sql = @"
                DO $do$
                BEGIN
                    IF to_regclass('public.""Tenants""') IS NOT NULL THEN
                        EXECUTE 'GRANT SELECT ON TABLE public.""Tenants"" TO sms_app';
                        EXECUTE 'REVOKE INSERT, UPDATE, DELETE ON TABLE public.""Tenants"" FROM sms_app';
                    END IF;
                END
                $do$;";

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Mirrors docker/init-db-least-privilege.sql and
        /// docker/grant-least-privilege-privileges.sql so the suite exercises
        /// the real role arrangement rather than a superuser.
        /// </summary>
        private static async Task ApplyLeastPrivilegeGrantsAsync()
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString(DatabaseName));
            await connection.OpenAsync();

            const string sql = @"
                GRANT CREATE, USAGE ON SCHEMA public TO sms_migration;
                GRANT USAGE ON SCHEMA public TO sms_app;
                REVOKE CREATE ON SCHEMA public FROM sms_app;
                REVOKE CREATE ON SCHEMA public FROM PUBLIC;

                -- PG15+ requires database-level CREATE for CREATE SCHEMA,
                -- which the RLS migration performs (CREATE SCHEMA app).
                GRANT CONNECT, CREATE ON DATABASE ""sms_rls_test"" TO sms_migration;
                GRANT CONNECT ON DATABASE ""sms_rls_test"" TO sms_app;

                ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
                    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO sms_app;
                ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
                    GRANT USAGE, SELECT ON SEQUENCES TO sms_app;

                -- Mirrors section 2a of docker/grant-least-privilege-privileges.sql.
                -- SELECT on Tenants is what tenant resolution needs and is
                -- kept; the three write privileges are revoked so the runtime
                -- role cannot create or rewrite a tenant registry row.
                --
                -- Guarded on existence because this block runs BEFORE
                -- migrations: at this point the Tenants table has not been
                -- created yet, and an unconditional GRANT/REVOKE on a missing
                -- relation aborts the whole provisioning step.
                DO $$
                BEGIN
                    IF to_regclass('public.""Tenants""') IS NOT NULL THEN
                        EXECUTE 'GRANT SELECT ON TABLE public.""Tenants"" TO sms_app';
                        EXECUTE 'REVOKE INSERT, UPDATE, DELETE ON TABLE public.""Tenants"" FROM sms_app';
                    END IF;
                END $$;";

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// A context bound to the migration role, used only for seed rows the
        /// runtime role is deliberately not allowed to write - the "Tenants"
        /// registry in particular. The migration role owns every table here, so
        /// it is exempt from their row level security policies and retains the
        /// privileges the runtime role has had revoked.
        /// </summary>
        public ApplicationDbContext CreateMigrationContext()
        {
            var tenant = new Mock<ITenantContext>();
            tenant.Setup(x => x.TenantId).Returns(TenantA.ToString());
            tenant.Setup(x => x.TenantName).Returns("RLS Seed");
            tenant.Setup(x => x.ConnectionString).Returns(string.Empty);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(MigrationConnectionString, npgsql => npgsql.CommandTimeout(60))
                .Options;

            return new ApplicationDbContext(
                options,
                new Mock<ICurrentUserService>().Object,
                tenant.Object);
        }

        private async Task SeedAsync()
        {
            // The tenant registry rows are written over the MIGRATION
            // connection. The runtime role holds SELECT only on "Tenants" -
            // it reads the registry to resolve the tenant, and nothing at
            // runtime writes it - so seeding over the runtime connection would
            // fail with `permission denied for table Tenants`. This is the same
            // split production uses: DatabaseSeeder writes bootstrap rows on
            // ConnectionStrings:MigrationConnection, and sms_migration owns the
            // table so it is exempt from its own RLS.
            await using var seedContext = CreateMigrationContext();

            // Both tenants must exist as real rows: every tenant-owned table
            // carries a foreign key into Tenants.
            var existingTenants = await seedContext.Tenants.IgnoreQueryFilters()
                .Select(t => t.Id).ToListAsync();

            if (!existingTenants.Contains(TenantA))
            {
                seedContext.Tenants.Add(new SMS.Domain.Entities.Tenant
                {
                    Id = TenantA, Name = "RLS Tenant A", Organization = "RLS Org A",
                    Subdomain = "rls-a", IsActive = true
                });
            }

            if (!existingTenants.Contains(TenantB))
            {
                seedContext.Tenants.Add(new SMS.Domain.Entities.Tenant
                {
                    Id = TenantB, Name = "RLS Tenant B", Organization = "RLS Org B",
                    Subdomain = "rls-b", IsActive = true
                });
            }

            await seedContext.SaveChangesAsync();

            // Read the lane ids back rather than creating them unconditionally:
            // the second fixture instance must find the SAME rows, otherwise
            // the "the foreign row really exists" ground truth would be
            // measuring a lane this instance just made.
            TenantALaneId = await ResolveLaneIdAsync(TenantA, "RLS-A-LANE");
            TenantBLaneId = await ResolveLaneIdAsync(TenantB, "RLS-B-LANE");
        }

        private async Task<Guid> ResolveLaneIdAsync(Guid tenantId, string laneName)
        {
            await using (var existing = CreateContext(tenantId))
            {
                var found = await existing.Lanes
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(l => l.LaneName == laneName);

                if (found is not null)
                    return found.Id;
            }

            await using var context = CreateContext(tenantId);
            context.Lanes.Add(new SMS.Domain.Entities.Lane
            {
                LaneName = laneName,
                Description = $"{laneName} seeded for RLS verification",
                IsActive = true,
                NumberingFormat = "A-000"
            });

            await context.SaveChangesAsync();
            return context.Lanes.IgnoreQueryFilters().First(l => l.LaneName == laneName).Id;
        }
    }

    /// <summary>
    /// xUnit collection that owns the single shared
    /// <see cref="TenantRowLevelSecurityFixture"/> and serialises every class
    /// that joins it.
    ///
    /// <para><b>Why this exists.</b> Both <see cref="TenantRowLevelSecurityTests"/>
    /// (read isolation) and <see cref="TenantRowLevelSecurityWriteTests"/> (write
    /// isolation) use the SAME <c>sms_rls_test</c> database and the SAME seed
    /// rows. They previously declared it as <c>IClassFixture</c>, which xUnit
    /// honours by constructing <b>one independent fixture instance per test
    /// class</b> - and each test class is its own collection, so the two ran in
    /// parallel. That produced two real defects:</para>
    ///
    /// <list type="number">
    /// <item>Two <c>InitializeAsync</c> runs racing over one database. The
    /// <c>IsProvisionedAsync</c> guard made a second <c>DROP DATABASE</c>
    /// unlikely, but only by luck of ordering: the check and the recreate are
    /// not atomic, so whichever class evaluated the check first could still be
    /// mid-<c>DROP</c> when the other decided to recreate.</item>
    ///
    /// <item>Concurrent mutation of shared rows. The write tests INSERT, UPDATE
    /// and soft-DELETE <c>Lanes</c> rows for BOTH tenants, while the read tests
    /// assert on live counts - for example
    /// <c>RawSql_TenantA_SeesOnlyItsOwnRows</c> requires
    /// <c>visible &lt; allLanes</c>. A row deleted or inserted between those two
    /// reads makes a security assertion fail for a reason that has nothing to
    /// do with row level security.</item>
    /// </list>
    ///
    /// <para>Joining a collection fixes both at once: the collection fixture is
    /// constructed <b>once</b> for the whole collection, and xUnit never runs
    /// two classes from the same collection in parallel. No test is removed, no
    /// seed row is dropped, and no timeout is increased.</para>
    /// </summary>
    [CollectionDefinition(Name)]
    public class TenantRowLevelSecurityCollection
        : ICollectionFixture<TenantRowLevelSecurityFixture>
    {
        /// <summary>
        /// Name both test classes join. Declared here rather than as a separate
        /// constant so the attribute and the definition cannot drift apart.
        /// </summary>
        public const string Name = "TenantRowLevelSecurity";
    }
}
