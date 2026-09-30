using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;

namespace SMS.Persistence.Data
{
    /// <summary>
    /// Applies EF Core migrations using a dedicated, elevated connection
    /// instead of the runtime application connection.
    ///
    /// <para><b>Why this exists.</b> The runtime role (<c>sms_app</c>) is
    /// deliberately NOBYPASSRLS, NOSUPERUSER and a non-owner. Those three
    /// attributes are what make <c>ENABLE ROW LEVEL SECURITY</c> mean
    /// something, and they also mean the role cannot run DDL. Historically
    /// the application applied migrations over the same connection string
    /// it used for requests, which only worked because that connection
    /// happened to be a superuser. Splitting the two is what lets the
    /// runtime role lose its superuser powers without breaking deploys.</para>
    ///
    /// <para>The migration connection is taken from
    /// <c>ConnectionStrings:MigrationConnection</c>. When it is absent the
    /// runtime connection is used, which preserves the previous behaviour
    /// for any deployment that has not been provisioned yet. That fallback
    /// is logged as a warning rather than silently taken, because on a
    /// provisioned deployment it means the wrong role is running DDL.</para>
    ///
    /// <para>Migrations themselves need no tenant context: they run DDL,
    /// and DDL is not subject to row level security. The stubs supplied
    /// here therefore only need to satisfy the
    /// <see cref="ApplicationDbContext"/> constructor.</para>
    /// </summary>
    public static class DatabaseMigrationRunner
    {
        /// <summary>Configuration key holding the elevated migration connection.</summary>
        public const string MigrationConnectionName = "MigrationConnection";

        /// <summary>
        /// Resolves the connection string migrations should run over, and
        /// reports whether that was the dedicated one or a fallback.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="usedFallback">
        /// True when no dedicated migration connection is configured and the
        /// runtime connection string was returned instead.
        /// </param>
        public static string? ResolveConnectionString(
            IConfiguration configuration,
            out bool usedFallback)
        {
            var migrationConnection = configuration.GetConnectionString(MigrationConnectionName);
            if (!string.IsNullOrWhiteSpace(migrationConnection))
            {
                usedFallback = false;
                return migrationConnection;
            }

            usedFallback = true;
            return configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Applies every pending migration over <paramref name="connectionString"/>.
        /// </summary>
        /// <returns>The number of migrations that were applied.</returns>
        public static async Task<int> ApplyAsync(
            string? connectionString,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "No connection string was supplied to DatabaseMigrationRunner. " +
                    $"Set ConnectionStrings:{MigrationConnectionName} for the migration role, " +
                    "or ConnectionStrings:DefaultConnection.");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(3);
                    npgsql.CommandTimeout(120);
                    // Explicitly point at this assembly so the same migrations
                    // are discovered regardless of which project hosts the caller.
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                })
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                .Options;

            await using var context = new ApplicationDbContext(
                options,
                new MigrationCurrentUserService(),
                new MigrationTenantContext());

            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
            {
                logger?.LogInformation("Database is already up to date. No migrations to apply.");
                return 0;
            }

            logger?.LogInformation(
                "Applying {Count} pending migration(s) as the migration role: {Migrations}",
                pending.Count,
                string.Join(", ", pending));

            await context.Database.MigrateAsync(cancellationToken);

            logger?.LogInformation("Database migrations applied successfully.");
            return pending.Count;
        }

        /// <summary>
        /// A tenant context for migration time. There is no request, so no
        /// tenant. It reports the all-zero sentinel, which every tenant
        /// policy treats as "matches no row" - the fail-closed default.
        /// </summary>
        private sealed class MigrationTenantContext : ITenantContext
        {
            public string TenantId => Guid.Empty.ToString();
            public string TenantName => string.Empty;
            public string ConnectionString => string.Empty;
        }

        private sealed class MigrationCurrentUserService : ICurrentUserService
        {
            public string UserId => "migration";
            public string Username => "migration";
            public string Email => string.Empty;
            public bool IsAuthenticated => false;
            public System.Collections.Generic.IEnumerable<string> Roles
                => Array.Empty<string>();
        }
    }
}