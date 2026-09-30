using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using Xunit;

namespace SMS.UnitTests.Persistence
{
    /// <summary>
    /// Pins the Production migration-connection contract.
    ///
    /// <para><b>The defect this guards.</b>
    /// <c>DatabaseMigrationRunner.ResolveConnectionString</c> used to warn and
    /// fall back to <c>DefaultConnection</c> whenever
    /// <c>ConnectionStrings:MigrationConnection</c> was missing - in every
    /// environment, including Production. In a correctly provisioned
    /// deployment <c>DefaultConnection</c> is <c>sms_app</c>: NOSUPERUSER,
    /// NOBYPASSRLS, and the owner of no table. Running DDL as that role either
    /// fails part-way through a migration or, on an un-provisioned box, quietly
    /// executes schema changes as a superuser. Both outcomes are the exact
    /// failure the runtime/migration split exists to prevent, and a warning in
    /// a log file is not a control.</para>
    ///
    /// <para>Production now fails closed with an explicit exception. Development
    /// and Testing keep the fallback, because a local throwaway database that
    /// has never been provisioned is a legitimate case where failing would be
    /// worse than warning.</para>
    /// </summary>
    public class DatabaseMigrationRunnerTests
    {
        private const string MigrationCs = "Host=h;Database=d;Username=sms_migration;Password=m;";
        private const string RuntimeCs = "Host=h;Database=d;Username=sms_app;Password=a;";

        private static IConfiguration Build(
            string? migrationConnection,
            string? defaultConnection = null,
            string? environmentName = null)
        {
            var values = new Dictionary<string, string?>();
            if (migrationConnection is not null)
                values["ConnectionStrings:MigrationConnection"] = migrationConnection;
            if (defaultConnection is not null)
                values["ConnectionStrings:DefaultConnection"] = defaultConnection;
            if (environmentName is not null)
                values["ASPNETCORE_ENVIRONMENT"] = environmentName;

            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        // ==================================================================
        // The regression: Production must NOT fall back.
        // ==================================================================

        [Theory]
        [InlineData("Production")]
        [InlineData("production")]   // case must not decide the outcome
        [InlineData("PRODUCTION")]
        [InlineData("Production ")]  // trailing whitespace from a compose/env value
        public void Production_WithNoMigrationConnection_Throws(string environmentName)
        {
            var configuration = Build(
                migrationConnection: null,
                defaultConnection: RuntimeCs,
                environmentName: environmentName);

            var act = () => DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out _, environmentName);

            act.Should().Throw<InvalidOperationException>(
                $"a Production deployment must fail closed, not run DDL as the runtime role ({environmentName})");
        }

        [Fact]
        public void Production_WithNoMigrationConnection_NamesBothSettingsInTheError()
        {
            // An operator has to be able to fix this from the message alone.
            var configuration = Build(null, RuntimeCs, "Production");

            var act = () => DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out _, "Production");

            act.Should().Throw<InvalidOperationException>()
                .Which.Message.Should().ContainAll(
                    "MigrationConnection",
                    "DefaultConnection",
                    "sms_migration",
                    "Production");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Production_WithBlankMigrationConnection_AlsoThrows(string blank)
        {
            // A blank/whitespace value is not "configured" either. Treating it
            // as configured would let an empty env var silently select the
            // runtime connection, which is precisely the case being closed.
            var configuration = Build(blank, RuntimeCs, "Production");

            var act = () => DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out _, "Production");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Production_WithNothingConfiguredAtAll_Throws()
        {
            // Nothing is configured. This must still be the explicit Production
            // failure, not a null string handed back to the caller.
            var configuration = Build(null, null, "Production");

            var act = () => DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out _, "Production");

            act.Should().Throw<InvalidOperationException>();
        }

        // ==================================================================
        // The positive path: a configured migration connection is always used,
        // in every environment. These must not regress into a throw.
        // ==================================================================

        [Theory]
        [InlineData("Production")]
        [InlineData("Development")]
        [InlineData("Testing")]
        [InlineData("")]
        public void MigrationConnection_WhenConfigured_IsUsedInEveryEnvironment(string environmentName)
        {
            var configuration = Build(MigrationCs, RuntimeCs, environmentName);

            var resolved = DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out var usedFallback, environmentName);

            resolved.Should().Be(MigrationCs);
            usedFallback.Should().BeFalse(
                "a dedicated migration connection must always win; the flag exists to report its absence");
        }

        [Fact]
        public void Production_ResolvedMigrationConnectionIsTheMigrationRole()
        {
            var configuration = Build(MigrationCs, RuntimeCs, "Production");

            var resolved = DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out _, "Production");

            resolved.Should().Contain("Username=sms_migration")
                .And.NotContain("Username=sms_app")
                .And.NotContain("sms_user",
                    "sms_user is the bootstrap SUPERUSER with BYPASSRLS and must never run application DDL");
        }

        // ==================================================================
        // Development/Testing keep the fallback, loudly.
        // ==================================================================

        [Theory]
        [InlineData("Development")]
        [InlineData("Testing")]
        [InlineData("Staging")]
        [InlineData("")]
        public void NonProduction_WithNoMigrationConnection_FallsBackAndReportsIt(string environmentName)
        {
            var configuration = Build(null, RuntimeCs, environmentName);

            var resolved = DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out var usedFallback, environmentName);

            resolved.Should().Be(RuntimeCs);
            usedFallback.Should().BeTrue(
                "the fallback is retained outside Production, but must always be reported so the caller warns");
        }

        // ==================================================================
        // Environment detection.
        // ==================================================================

        [Fact]
        public void EnvironmentName_IsReadFromConfigurationWhenNotOverridden()
        {
            // This is how the real call sites behave: they pass only the
            // configuration, and the environment has to be discovered from it.
            var configuration = Build(null, RuntimeCs, "Production");

            var act = () => DatabaseMigrationRunner.ResolveConnectionString(configuration, out _);

            act.Should().Throw<InvalidOperationException>(
                "a configuration carrying ASPNETCORE_ENVIRONMENT=Production must fail closed with no override");
        }

        [Theory]
        [InlineData("Production", true)]
        [InlineData("production", true)]
        [InlineData(" Development ", false)]
        [InlineData("Testing", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void IsProduction_MatchesOnlyProductionCaseInsensitively(string? name, bool expected)
            => DatabaseMigrationRunner.IsProduction(name).Should().Be(expected);

        [Fact]
        public void ResolveEnvironmentName_PrefersAspNetCoreOverDotNet()
        {
            // Same precedence the generic host applies, so the runner can never
            // disagree with the host about which environment it is in.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["DOTNET_ENVIRONMENT"] = "Development",
                })
                .Build();

            DatabaseMigrationRunner.ResolveEnvironmentName(configuration)
                .Should().Be("Production");
        }

        [Fact]
        public void ExplicitOverride_BeatsConfiguration()
        {
            var configuration = Build(null, RuntimeCs, "Development");

            var act = () => DatabaseMigrationRunner.ResolveConnectionString(
                configuration, out _, "Production");

            act.Should().Throw<InvalidOperationException>();
        }

    }
}
