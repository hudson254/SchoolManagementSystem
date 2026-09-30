using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// A REAL PostgreSQL fixture for the enrollment entity-state regression.
    ///
    /// <para>
    /// Unlike <see cref="DatabaseFixture"/> - which silently falls back to the
    /// EF InMemory provider when Docker is not running - this fixture is
    /// deliberately NON-NEGOTIABLE: it provisions a dedicated database on the
    /// real PostgreSQL server and applies the real EF migrations. If the server
    /// cannot be reached the fixture throws instead of degrading, because the
    /// whole point of these tests is to observe EF Core's relational change
    /// tracking. The InMemory provider would happily accept a misclassified
    /// "Modified" entity and hide the exact production defect under test.
    /// </para>
    ///
    /// <para>
    /// A dedicated database name is used so this suite can never interfere with
    /// <c>sms_test</c>, which SMS.ApiTests shares, and vice versa.
    /// </para>
    /// </summary>
    public class EnrollmentEntityStateFixture : IAsyncLifetime
    {
        public const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
        public const string DatabaseName = "sms_enrollment_state_test";
        public const string AdminDatabaseName = "postgres";
        public const string TenantIdValue = "11111111-1111-1111-1111-111111111111";
        public const string OtherTenantIdValue = "22222222-2222-2222-2222-222222222222";

        public static readonly Guid TenantId = Guid.Parse(TenantIdValue);
        public static readonly Guid OtherTenantId = Guid.Parse(OtherTenantIdValue);

        private static readonly SemaphoreSlim InitLock = new(1, 1);
        private string _connectionString = string.Empty;

        public string ConnectionString => _connectionString;

        private static string Env(string name, string fallback) =>
            Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        public static string Host => Env("SMS_TEST_PG_HOST", "localhost");
        public static int Port => int.Parse(Env("SMS_TEST_PG_PORT", "5433"), CultureInfo.InvariantCulture);

        /// <summary>
        /// Bootstrap role. Only used to CREATE DATABASE and to hand the
        /// freshly created database's schema to the migration role. The
        /// application never connects as this.
        /// </summary>
        public static string AdminUser => Env("SMS_TEST_PG_ADMIN_USER", "testuser");
        public static string AdminPassword => Env("SMS_TEST_PG_ADMIN_PASSWORD", "testpass123");

        /// <summary>
        /// Least-privilege runtime role: NOSUPERUSER, NOBYPASSRLS, owns
        /// nothing. Every query the tests make runs as this role, which is
        /// what lets them observe row level security rather than bypass it.
        /// </summary>
        public static string User => Env("SMS_TEST_PG_USER", "sms_app");
        public static string Password => Env("SMS_TEST_PG_PASSWORD", "testapp123");

        /// <summary>
        /// DDL role. Applies migrations and therefore owns the tables it
        /// creates. Kept separate from <see cref="User"/> so the runtime
        /// role never needs DDL.
        /// </summary>
        public static string MigrationUser => Env("SMS_TEST_PG_MIGRATION_USER", "sms_migration");
        public static string MigrationPassword => Env("SMS_TEST_PG_MIGRATION_PASSWORD", "testmigr123");

        private static string BuildConnectionString(string database) =>
            $"Host={Host};Port={Port};Database={database};Username={User};Password={Password};" +
            "Minimum Pool Size=1;Maximum Pool Size=10;Include Error Detail=true;";

        private static string BuildAdminConnectionString(string database) =>
            $"Host={Host};Port={Port};Database={database};Username={AdminUser};Password={AdminPassword};";

        private static string BuildMigrationConnectionString(string database) =>
            $"Host={Host};Port={Port};Database={database};Username={MigrationUser};Password={MigrationPassword};" +
            "Minimum Pool Size=1;Maximum Pool Size=2;Include Error Detail=true;";


        public async Task InitializeAsync()
        {
            await InitLock.WaitAsync();
            try
            {
                _connectionString = BuildConnectionString(DatabaseName);

                // The database is owned outright by this fixture, so it is
                // dropped and rebuilt on every run. That is not tidiness:
                // a database left over from a previous run has tables owned
                // by whichever role created them, and reusing it would
                // silently keep the old, over-privileged ownership instead
                // of exercising the runtime/migration split.
                await RecreateDatabaseAsync();

                // A brand-new database has a stock `public` schema with no
                // CREATE for sms_migration, so the migration role could not
                // create anything. Hand it over before migrating.
                await ApplyLeastPrivilegeGrantsAsync();

                await DatabaseMigrationRunner.ApplyAsync(
                    BuildMigrationConnectionString(DatabaseName));

                // Narrow "Tenants" to SELECT for the runtime role AFTER
                // migrations, mirroring section 2a of
                // docker/grant-least-privilege-privileges.sql. The grants above
                // used ALTER DEFAULT PRIVILEGES, which necessarily covers
                // "Tenants" because default privileges cannot name an
                // exception; production closes that by re-running the grant
                // script after migrating, and this fixture mirrors it so the
                // two arrangements cannot drift apart.
                await ApplyTenantsRegistryGrantsAsync();

                await using var context = CreateContext();
                if (!string.Equals(context.Database.ProviderName, NpgsqlProviderName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The enrollment entity-state regression tests REQUIRE a real PostgreSQL provider but got " +
                        $"'{context.Database.ProviderName}'. These tests must not be downgraded to InMemory: the " +
                        "InMemory provider does not reproduce the Modified/Added misclassification under test.");
                }

                await SeedTenantsAsync();
            }
            finally
            {
                InitLock.Release();
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Both tenants referenced by the tests must exist as real rows, because
        /// Courses/Students/Enrollments carry a tenant_id foreign key into
        /// "Tenants".
        ///
        /// <para>This runs on the MIGRATION connection, not the runtime one,
        /// and that is not incidental. The runtime role holds SELECT only on
        /// "Tenants" - tenant resolution reads it, nothing writes it - so a
        /// seed performed over the runtime connection would now fail with
        /// <c>permission denied for table Tenants</c>. This mirrors production
        /// exactly: <c>DatabaseSeeder.SeedDefaultTenantAsync</c> is reached only
        /// from the <c>seed-data</c> CLI, which builds its DbContext on
        /// ConnectionStrings:MigrationConnection. The migration role owns the
        /// table, so it is exempt from its own RLS and retains the write
        /// privilege.</para>
        /// </summary>
        private static async Task SeedTenantsAsync()
        {
            var wanted = new[]
            {
                (TenantId, "Enrollment State Test Tenant", "enrollment-state-test"),
                (OtherTenantId, "Other Test Tenant", "other-test")
            };

            await using var context = CreateMigrationContext();
            foreach (var (id, name, subdomain) in wanted)
            {
                var exists = await context.Tenants.AnyAsync(t => t.Id == id);
                if (exists)
                    continue;

                context.Tenants.Add(new Tenant
                {
                    Id = id,
                    Name = name,
                    Organization = name,
                    Subdomain = subdomain,
                    IsActive = true
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// A context bound to the migration role. Used only for seed data that
        /// the runtime role is not permitted to write - "Tenants" in
        /// particular.
        /// </summary>
        private static ApplicationDbContext CreateMigrationContext()
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns("integration-test-user");
            currentUser.Setup(x => x.Username).Returns("integration-test");
            currentUser.Setup(x => x.IsAuthenticated).Returns(true);

            var tenant = new Mock<ITenantContext>();
            tenant.Setup(x => x.TenantId).Returns(TenantId.ToString());
            tenant.Setup(x => x.TenantName).Returns("Enrollment State Test Tenant");
            tenant.Setup(x => x.ConnectionString).Returns(string.Empty);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(BuildMigrationConnectionString(DatabaseName),
                    npgsql => npgsql.CommandTimeout(60))
                .Options;

            return new ApplicationDbContext(options, currentUser.Object, tenant.Object);
        }

        /// <summary>
        /// Drops (if present) and recreates the dedicated test database.
        /// Connections are terminated first so a pooled connection left
        /// behind by a previous run cannot block the drop.
        ///
        /// <para><c>NpgsqlConnection.ClearAllPools()</c> is what makes the
        /// drop total. Npgsql's connection pool is <b>process-wide static</b>,
        /// so a connector opened against <c>sms_enrollment_state_test</c> by an
        /// earlier fixture or an earlier test class outlives this database
        /// being dropped and recreated. <c>WITH (FORCE)</c> terminates the
        /// server-side backends, but it cannot retract a client-side pooled
        /// connector that still believes the database exists, so the next
        /// rental is handed a dead backend. That is the intermittent failure
        /// this method used to cause: a connection reset or a "database is
        /// being accessed by other users" error raised inside an unrelated
        /// test, sometimes minutes after the drop that caused it. Clearing
        /// every pool first means no reference to the old database can survive
        /// it, so the suite is deterministic regardless of what ran before.</para>
        /// </summary>
        private static async Task RecreateDatabaseAsync()
        {
            // Discard every pooled connection before destroying what it points
            // at. This is the fix for the DROP ... WITH (FORCE) vs static-pool
            // interaction; it is a correctness fix, not a retry.
            NpgsqlConnection.ClearAllPools();

            await using var connection = new NpgsqlConnection(BuildAdminConnectionString(AdminDatabaseName));
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
            await using var connection = new NpgsqlConnection(BuildAdminConnectionString(DatabaseName));
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
        /// Applies the same least-privilege grants that
        /// docker/init-db-least-privilege.sql and
        /// docker/grant-least-privilege-privileges.sql apply to the main
        /// database. Kept here so the fixture exercises the real role
        /// arrangement instead of testing against a superuser.
        /// </summary>
        private static async Task ApplyLeastPrivilegeGrantsAsync()
        {
            await using var connection = new NpgsqlConnection(BuildAdminConnectionString(DatabaseName));
            await connection.OpenAsync();

            const string sql = @"
                GRANT CREATE, USAGE ON SCHEMA public TO sms_migration;
                GRANT USAGE ON SCHEMA public TO sms_app;
                REVOKE CREATE ON SCHEMA public FROM sms_app;
                REVOKE CREATE ON SCHEMA public FROM PUBLIC;

                -- PG15+ requires database-level CREATE for CREATE SCHEMA,
                -- which the RLS migration performs (CREATE SCHEMA app).
                GRANT CONNECT, CREATE ON DATABASE """ + DatabaseName + @""" TO sms_migration;
                GRANT CONNECT ON DATABASE """ + DatabaseName + @""" TO sms_app;

                ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
                    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO sms_app;
                ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
                    GRANT USAGE, SELECT ON SEQUENCES TO sms_app;

                -- Mirrors section 2a of docker/grant-least-privilege-privileges.sql.
                -- The runtime role resolves tenants by READING this table, so
                -- SELECT is kept; the three write privileges are revoked.
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
        /// Creates a context bound to the default test tenant. <paramref name="email"/>
        /// becomes the current user, which is how the handlers resolve the student.
        /// </summary>
        public ApplicationDbContext CreateContext(string email = "integration-test@school.com")
            => CreateContext(email, TenantId);

        public ApplicationDbContext CreateContext(string email, Guid tenantId)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns("integration-test-user");
            currentUser.Setup(x => x.Username).Returns("integration-test");
            currentUser.Setup(x => x.Email).Returns(email);
            currentUser.Setup(x => x.IsAuthenticated).Returns(true);
            currentUser.Setup(x => x.Roles).Returns(new[] { "Student" });

            var tenant = new Mock<ITenantContext>();
            tenant.Setup(x => x.TenantId).Returns(tenantId.ToString());
            tenant.Setup(x => x.TenantName).Returns("Enrollment State Test Tenant");

            // The tenant interceptor has to be attached here, exactly as the
            // runtime DI root attaches it. This context connects as the
            // least-privilege NOBYPASSRLS role, so once row level security is
            // enabled a context without it reads nothing at all: no
            // app.tenant_id on the session means every policy evaluates
            // against the all-zero sentinel.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_connectionString, npgsql => npgsql.CommandTimeout(60))
                .AddInterceptors(new TenantContextDbInterceptor(
                    tenant.Object,
                    NullLogger<TenantContextDbInterceptor>.Instance))
                .AddInterceptors(new TenantConnectionDbInterceptor(
                    tenant.Object,
                    NullLogger<TenantConnectionDbInterceptor>.Instance))
                .Options;

            return new ApplicationDbContext(options, currentUser.Object, tenant.Object);
        }
    }
}
