using FluentAssertions;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace SMS.UnitTests.Security
{
    /// <summary>
    /// Static audit of the deployment configuration.
    ///
    /// <para><b>Why a text test at all.</b> The whole least-privilege and RLS
    /// architecture is only real if the deployed API actually connects as
    /// <c>sms_app</c>. Nothing in the C# code can enforce that: it is decided
    /// entirely by the connection string compose interpolates into the
    /// container environment. The audit found the production service still
    /// declared <c>Username=sms_user</c> - the cluster bootstrap SUPERUSER
    /// with BYPASSRLS - which silently disabled all 70 RLS-enabled tables
    /// while every RLS test still passed, because those tests connect as
    /// <c>sms_app</c> directly and never went through compose.</para>
    ///
    /// <para>This test reads the compose files as committed and fails if the
    /// runtime role regresses, if the migration role disappears, or if the
    /// role-initialisation scripts stop being mounted. It is a cheap guard
    /// against a class of defect that no amount of application testing
    /// catches.</para>
    /// </summary>
    public class ComposeLeastPrivilegeWiringTests
    {
        private static string ReadRepoFile(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(
                    directory.FullName,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(candidate))
                    return File.ReadAllText(candidate);

                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                $"Could not locate '{relativePath}' above {AppContext.BaseDirectory}.");
        }

        private static string ServiceBlock(string compose, string serviceName)
        {
            // Services are keyed at two-space indentation; the block runs to
            // the next key at the same indentation.
            var match = Regex.Match(
                compose,
                @"^\s{2}" + serviceName + @":\s*$(?<body>.*?)(?=^\s{2}\S|\z)",
                RegexOptions.Multiline | RegexOptions.Singleline);

            match.Success.Should().BeTrue($"the compose file must declare a '{serviceName}' service");
            return match.Groups["body"].Value;
        }

        private static string ApiServiceBlock(string file) => ServiceBlock(ReadRepoFile(file), "api");

        private static string PostgresServiceBlock(string file)
        {
            var compose = ReadRepoFile(file);
            return compose.Contains("postgres-test:")
                ? ServiceBlock(compose, "postgres-test")
                : ServiceBlock(compose, "postgres");
        }

        private static string EnvValue(string serviceBlock, string key)
        {
            var match = Regex.Match(
                serviceBlock,
                Regex.Escape(key) + @":\s*""?(?<v>[^""\r\n]*)""?",
                RegexOptions.Multiline);

            return match.Success ? match.Groups["v"].Value : string.Empty;
        }

        // ==================================================================
        // P0 - the application must never run as the bootstrap superuser.
        // ==================================================================

        [Theory]
        [InlineData("docker/docker-compose.prod.yml")]
        [InlineData("docker/docker-compose.yml")]
        public void ProductionApi_DefaultConnectionUsesTheLeastPrivilegeRuntimeRole(string file)
        {
            var defaultConnection = EnvValue(ApiServiceBlock(file), "ConnectionStrings__DefaultConnection");

            defaultConnection.Should().NotBeNullOrEmpty("DefaultConnection must be set explicitly");
            defaultConnection.Should().Contain("Username=sms_app");
            defaultConnection.Should().NotContain("Username=sms_user",
                "sms_user is SUPERUSER with BYPASSRLS; connecting as it silently disables every RLS policy");
        }

        [Theory]
        [InlineData("docker/docker-compose.prod.yml")]
        [InlineData("docker/docker-compose.yml")]
        public void ProductionApi_HasASeparateMigrationConnectionOnTheMigrationRole(string file)
        {
            var migrationConnection = EnvValue(ApiServiceBlock(file), "ConnectionStrings__MigrationConnection");

            migrationConnection.Should().NotBeNullOrEmpty(
                "without MigrationConnection, migrations fall back to the runtime role - and in Production that now " +
                "aborts the process at boot");
            migrationConnection.Should().Contain("Username=sms_migration");
            migrationConnection.Should().NotContain("Username=sms_user");
        }

        [Theory]
        [InlineData("docker/docker-compose.prod.yml")]
        [InlineData("docker/docker-compose.yml")]
        public void ProductionApi_UsesTwoDistinctSecrets_AndHardCodesNeither(string file)
        {
            var api = ApiServiceBlock(file);

            ReadRepoFile(file).Should().Contain("SMS_DB_APP_PASSWORD");
            ReadRepoFile(file).Should().Contain("SMS_DB_MIGRATION_PASSWORD");

            // The migration password must be a separate secret, not a copy of
            // the runtime one, and neither may be a literal in the file.
            api.Should().Contain("Password=${SMS_DB_APP_PASSWORD");
            api.Should().Contain("Password=${SMS_DB_MIGRATION_PASSWORD");
            api.Should().NotContain("Password=${DB_PASSWORD",
                "DB_PASSWORD is the bootstrap superuser's password and must not authenticate the application");
        }

        // ==================================================================
        // The bootstrap superuser stays where it belongs.
        // ==================================================================

        [Theory]
        [InlineData("docker/docker-compose.prod.yml")]
        [InlineData("docker/docker-compose.yml")]
        [InlineData("docker/docker-compose.dev.yml")]
        [InlineData("docker/docker-compose.test.yml")]
        public void PostgreSqlBootstrapRole_IsStillDeclared(string file)
        {
            // sms_user (or testuser) is still required: it owns the database,
            // runs the /docker-entrypoint-initdb.d scripts and is used for
            // pg_dump. Rolling the role split forward must never delete the
            // bootstrap role, or a fresh volume cannot initialise at all.
            EnvValue(PostgresServiceBlock(file), "POSTGRES_USER").Should().NotBeNullOrEmpty();
        }

        // ==================================================================
        // Fresh-database role creation must actually be wired up.
        // ==================================================================

        [Theory]
        [InlineData("docker/docker-compose.prod.yml")]
        [InlineData("docker/docker-compose.yml")]
        [InlineData("docker/docker-compose.dev.yml")]
        [InlineData("docker/docker-compose.test.yml")]
        public void EveryPostgresService_MountsTheLeastPrivilegeInitialisation(string file)
        {
            var postgres = PostgresServiceBlock(file);

            postgres.Should().Contain("init-db-least-privilege.sql",
                "without this mount a fresh volume has no sms_app/sms_migration at all and the API cannot start");
            postgres.Should().Contain("init-db-least-privilege-passwords.sh",
                "the roles are created without passwords, so a fresh volume needs this to make them usable");
        }

        [Theory]
        [InlineData("docker/docker-compose.prod.yml")]
        [InlineData("docker/docker-compose.yml")]
        [InlineData("docker/docker-compose.dev.yml")]
        [InlineData("docker/docker-compose.test.yml")]
        public void RoleCreationIsOrderedBeforePasswordAssignment(string file)
        {
            // /docker-entrypoint-initdb.d is executed in LEXICAL ORDER of the
            // file names inside it, not in the order they are written here. The
            // numeric prefixes are therefore load-bearing rather than
            // cosmetic: they are what guarantees the roles exist before the
            // script that ALTERs them runs.
            //
            // The assertion is on the mount TARGETS (the path inside the
            // container), not on the host-side source paths and not on raw
            // file offsets - a comment mentioning a script name would
            // otherwise satisfy a naive substring search.
            var postgres = PostgresServiceBlock(file);

            postgres.Should().MatchRegex(
                @"docker-entrypoint-initdb\.d/01-init-db-least-privilege\.sql",
                "the role-creation script must sort first in /docker-entrypoint-initdb.d");
            postgres.Should().MatchRegex(
                @"docker-entrypoint-initdb\.d/02-init-db-least-privilege-passwords\.sh",
                "the password script must sort after the script that creates the roles");
        }

        [Fact]
        public void PasswordScript_CarriesNoPasswordAndUsesPosixSh()
        {
            var script = ReadRepoFile("docker/init-db-least-privilege-passwords.sh");

            script.Should().StartWith("#!/bin/sh",
                "postgres:16-alpine ships no bash; a bash shebang cannot be executed inside the container");
            script.Should().NotContain("PASSWORD '", "no role password may be hard-coded in the repository");

            // The passwords must arrive from the environment, never inline.
            script.Should().Contain("SMS_DB_APP_PASSWORD");
            script.Should().Contain("SMS_DB_MIGRATION_PASSWORD");

            // Re-asserted on every ALTER ROLE so the script cannot be the
            // reason a role became privileged.
            script.Should().Contain("NOBYPASSRLS");
            script.Should().Contain("NOSUPERUSER");
        }

        [Fact]
        public void TenantsIsReadOnlyForTheRuntimeRoleInTheGrantScript()
        {
            var grant = ReadRepoFile("docker/grant-least-privilege-privileges.sql");

            grant.Should().Contain(@"REVOKE INSERT, UPDATE, DELETE ON TABLE public.""Tenants"" FROM sms_app");
            grant.Should().Contain(@"GRANT SELECT ON TABLE public.""Tenants"" TO sms_app");
        }


    }
}
