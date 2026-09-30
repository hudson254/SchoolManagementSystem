using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace SMS.Persistence.Data
{
    /// <summary>
    /// Design-time factory for EF Core migrations. Used when running `dotnet ef migrations add`.
    /// Provides mock services for design-time tools.
    ///
    /// <para><b>No connection string is hard-coded here.</b> This file used to
    /// embed a username and password, which put a credential in git and, more
    /// importantly, meant <c>dotnet ef</c> silently targeted a superuser. The
    /// connection string is now resolved, in order, from:</para>
    /// <list type="number">
    /// <item>the <c>SMS_DESIGN_TIME_CONNECTION</c> environment variable,</item>
    /// <item>the <c>ConnectionStrings:DefaultConnection</c> value from an
    /// <c>appsettings.json</c> found next to the project or above it,</item>
    /// <item>the standard <c>DefaultConnection</c> environment variable.</item>
    /// </list>
    /// <para>Use the runtime role by default. Point
    /// <c>SMS_DESIGN_TIME_CONNECTION</c> at the migration role only when
    /// scaffolding a migration that needs to read real data.</para>
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var connectionString = ResolveConnectionString();

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

            builder.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(3);
                    npgsqlOptions.CommandTimeout(60);
                });

            return new ApplicationDbContext(
                builder.Options,
                new DesignTimeCurrentUserService(),
                new DesignTimeTenantContext());
        }

        /// <summary>
        /// Resolves the design-time connection string.
        ///
        /// <para>When nothing is configured this returns a
        /// <b>metadata-only placeholder</b> rather than throwing. That is
        /// deliberate: the great majority of design-time and test use of
        /// this factory never opens a connection at all - it only reads
        /// the migrations assembly and the model snapshot, both of which
        /// are compiled-in metadata. Throwing here broke those callers for
        /// no benefit.</para>
        ///
        /// <para>The placeholder carries no password, names a database
        /// that does not exist, and points at the default local port, so
        /// any attempt to actually connect with it fails loudly instead of
        /// silently reaching a real database as a privileged user. That is
        /// the specific failure mode the previous hard-coded superuser
        /// credential had.</para>
        /// </summary>
        internal const string MetadataOnlyPlaceholderConnectionString =
            "Host=localhost;Port=5432;Database=sms_design_time_metadata_only_not_a_real_database;" +
            "Username=sms_app_metadata_only;";

        internal static string ResolveConnectionString()
        {
            var explicitConnection = Environment.GetEnvironmentVariable("SMS_DESIGN_TIME_CONNECTION");
            if (!string.IsNullOrWhiteSpace(explicitConnection))
                return explicitConnection;

            var fromConfiguration = ReadFromAppsettings();
            if (!string.IsNullOrWhiteSpace(fromConfiguration))
                return fromConfiguration;

            var fromEnvironment = Environment.GetEnvironmentVariable("DefaultConnection")
                                   ?? Environment.GetEnvironmentVariable("DefaultConnection__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;

            return MetadataOnlyPlaceholderConnectionString;
        }

        /// <summary>
        /// Reads ConnectionStrings:DefaultConnection from the nearest
        /// appsettings.json. Hand-rolled rather than taken as a dependency
        /// so the design-time tooling keeps working on a bare checkout.
        /// </summary>
        private static string? ReadFromAppsettings()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "appsettings.json");
                if (File.Exists(candidate))
                {
                    var connection = TryReadDefaultConnection(candidate);
                    if (!string.IsNullOrWhiteSpace(connection))
                        return connection;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static string? TryReadDefaultConnection(string path)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(
                    File.ReadAllText(path),
                    new System.Text.Json.JsonDocumentOptions
                    {
                        CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });

                if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
                    && connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
                {
                    return defaultConnection.GetString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // A malformed appsettings.json is the application's problem,
                // not a reason to fail `dotnet ef`. The caller falls through
                // to the environment variable.
            }

            return null;
        }
    }

    /// <summary>
    /// Mock ICurrentUserService for design-time EF Core tools
    /// </summary>
    public class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string UserId => string.Empty;
        public string Username => string.Empty;
        public string Email => string.Empty;
        public bool IsAuthenticated => false;
        public IEnumerable<string> Roles => new List<string>();
    }

    /// <summary>
    /// Mock ITenantContext for design-time EF Core tools
    /// </summary>
    public class DesignTimeTenantContext : ITenantContext
    {
        public string TenantId => "00000000-0000-0000-0000-000000000000";
        public string TenantName => "DesignTime";
        public string ConnectionString => string.Empty;
    }
}
