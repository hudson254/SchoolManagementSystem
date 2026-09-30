using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SMS.Persistence.Data;

namespace SMS.ApiTests
{
    /// <summary>
    /// Applies pending EF Core migrations for the API test suite over the
    /// dedicated migration connection.
    ///
    /// <para>The API test database now runs two PostgreSQL roles:
    /// <c>sms_app</c> (NOSUPERUSER, NOBYPASSRLS, owns nothing) for the code
    /// under test, and <c>sms_migration</c> for DDL. Fixtures used to call
    /// <c>dbContext.Database.MigrateAsync()</c> on the runtime connection,
    /// which only worked because that connection happened to be a
    /// superuser. Keeping that pattern would have forced the tests to keep
    /// the runtime role over-privileged and would have made the suite
    /// incapable of proving anything about row level security.</para>
    ///
    /// <para>Readiness and seeding still go through the runtime connection,
    /// because that is exactly what production does after the migration
    /// role has been granted ownership of the tables.</para>
    /// </summary>
    internal static class TestDatabaseMigrator
    {
        /// <summary>
        /// Applies any pending migrations, resolving the elevated
        /// connection from the host's configuration.
        /// </summary>
        public static async Task MigrateAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();

            var connectionString = DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out var usedRuntimeConnectionAsFallback);

            if (usedRuntimeConnectionAsFallback)
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:MigrationConnection is not configured. The API tests " +
                    "require the sms_migration role so that the runtime role can stay " +
                    "NOSUPERUSER/NOBYPASSRLS. See Documentation/Security/RLS-ENFORCEMENT.md.");
            }

            await DatabaseMigrationRunner.ApplyAsync(connectionString);
        }
    }
}