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
    /// <c>ConnectionStrings:MigrationConnection</c>.
    ///
    /// <para><b>No fallback in Production.</b> When
    /// <c>MigrationConnection</c> is absent, a Production deployment now
    /// throws instead of falling back. The reason is that the fallback is
    /// precisely the failure this split exists to prevent: on a provisioned
    /// deployment the runtime connection is <c>sms_app</c> - NOSUPASSRLS,
    /// non-owner - so "fall back" does not mean "carry on as before", it means
    /// running DDL as the least-privilege role, which either fails obscurely
    /// or, worse, on an un-provisioned box silently executes schema changes as
    /// a superuser. A loud failure at boot is the correct outcome in both
    /// cases; guessing is not.
    ///
    /// <para>The fallback is retained for Development and Testing, where the
    /// database is frequently a local throwaway that has never been
    /// provisioned and where failing would be a worse outcome than a warning.
    /// It is reported through <paramref name="usedFallback"/> and logged as a
    /// warning, never taken silently.</para>
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

        /// <summary>Configuration key holding the runtime application connection.</summary>
        public const string DefaultConnectionName = "DefaultConnection";

        /// <summary>
        /// The one environment in which falling back to the runtime
        /// connection is permitted. Compared case-insensitively.
        /// </summary>
        public const string ProductionEnvironmentName = "Production";

        /// <summary>
        /// Resolves the connection string migrations should run over, and
        /// reports whether that was the dedicated one or a fallback.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="usedFallback">
        /// True when no dedicated migration connection is configured and the
        /// runtime connection string was returned instead.
        /// </param>
        /// <param name="environmentName">
        /// Overrides the detected environment name. Only used by tests; when
        /// null the environment is read from configuration and then from the
        /// process environment.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no <c>MigrationConnection</c> is configured and the
        /// deployment is running in Production.
        /// </exception>
        public static string? ResolveConnectionString(
            IConfiguration configuration,
            out bool usedFallback,
            string? environmentName = null)
        {
            var migrationConnection = configuration.GetConnectionString(MigrationConnectionName);
            if (!string.IsNullOrWhiteSpace(migrationConnection))
            {
                usedFallback = false;
                return migrationConnection;
            }

            if (IsProduction(ResolveEnvironmentName(configuration, environmentName)))
            {
                // Deliberately thrown before `usedFallback` is assigned: this
                // path must never return a connection string at all.
                throw new InvalidOperationException(
                    $"ConnectionStrings:{MigrationConnectionName} is not configured, and this " +
                    $"deployment is running in the {ProductionEnvironmentName} environment. " +
                    "Migrations would otherwise fall back to " +
                    $"ConnectionStrings:{DefaultConnectionName} - the least-privilege runtime " +
                    "connection - and either fail part-way through or, on an un-provisioned " +
                    "database, quietly run DDL as a superuser. Set " +
                    $"ConnectionStrings:{MigrationConnectionName} to a connection string for the " +
                    "sms_migration role: it owns the tables, so it is the only role that may " +
                    "perform DDL against them.");
            }

            usedFallback = true;
            return configuration.GetConnectionString(DefaultConnectionName);
        }

        /// <summary>
        /// Determines the environment name from the explicit override, then
        /// from configuration, then from the process environment.
        /// </summary>
        public static string ResolveEnvironmentName(
            IConfiguration configuration,
            string? environmentName = null)
        {
            if (!string.IsNullOrWhiteSpace(environmentName))
                return environmentName!.Trim();

            // ASPNETCORE_ENVIRONMENT wins over DOTNET_ENVIRONMENT: that is the
            // order the generic host itself applies.
            var fromConfiguration =
                configuration["ASPNETCORE_ENVIRONMENT"] ??
                configuration["DOTNET_ENVIRONMENT"];

            if (!string.IsNullOrWhiteSpace(fromConfiguration))
                return fromConfiguration!.Trim();

            var fromEnvironment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            return fromEnvironment?.Trim() ?? string.Empty;
        }

        /// <summary>True when <paramref name="environmentName"/> is Production.</summary>
        public static bool IsProduction(string? environmentName)
            => string.Equals(environmentName?.Trim(), ProductionEnvironmentName,
                StringComparison.OrdinalIgnoreCase);

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