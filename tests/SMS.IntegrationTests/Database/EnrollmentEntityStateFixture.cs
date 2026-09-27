using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        public static string User => Env("SMS_TEST_PG_USER", "testuser");
        public static string Password => Env("SMS_TEST_PG_PASSWORD", "testpass123");

        private static string BuildConnectionString(string database) =>
            $"Host={Host};Port={Port};Database={database};Username={User};Password={Password};" +
            "Minimum Pool Size=1;Maximum Pool Size=10;Include Error Detail=true;";


        public async Task InitializeAsync()
        {
            await InitLock.WaitAsync();
            try
            {
                _connectionString = BuildConnectionString(DatabaseName);

                await EnsureDatabaseExistsAsync();

                await using var context = CreateContext();
                if (!string.Equals(context.Database.ProviderName, NpgsqlProviderName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The enrollment entity-state regression tests REQUIRE a real PostgreSQL provider but got " +
                        $"'{context.Database.ProviderName}'. These tests must not be downgraded to InMemory: the " +
                        "InMemory provider does not reproduce the Modified/Added misclassification under test.");
                }

                var pending = await context.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                    await context.Database.MigrateAsync();

                await SeedTenantsAsync(context);
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
        /// </summary>
        private static async Task SeedTenantsAsync(ApplicationDbContext context)
        {
            var wanted = new[]
            {
                (TenantId, "Enrollment State Test Tenant", "enrollment-state-test"),
                (OtherTenantId, "Other Test Tenant", "other-test")
            };

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

        private static async Task EnsureDatabaseExistsAsync()
        {
            await using var connection = new NpgsqlConnection(BuildConnectionString(AdminDatabaseName));
            await connection.OpenAsync();

            await using (var probe = new NpgsqlCommand(
                "select 1 from pg_database where datname = @name", connection))
            {
                probe.Parameters.AddWithValue("name", DatabaseName);
                var found = await probe.ExecuteScalarAsync();
                if (found is not null && Convert.ToInt32(found, CultureInfo.InvariantCulture) == 1)
                    return;
            }

            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{DatabaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
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

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_connectionString, npgsql => npgsql.CommandTimeout(60))
                .Options;

            return new ApplicationDbContext(options, currentUser.Object, tenant.Object);
        }
    }
}
