using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SMS.Persistence.Data;
using SMS.Persistence.Migrations;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// Guards the tenant Row Level Security workstream.
    ///
    /// <para>
    /// The original <c>20260823120000_EnableRowLevelSecurity</c> was committed with no
    /// <c>[Migration]</c> attributes and no <c>.Designer.cs</c>, so Entity Framework has
    /// never discovered it and it has never run anywhere.
    /// <c>AddTenantRowLevelSecurityPolicies</c> replaces it with a correct, additive
    /// migration.
    /// </para>
    ///
    /// <para>
    /// That migration deliberately creates the tenant policies but does <b>not</b> enable
    /// row level security. The application connects as <c>sms_user</c>, which is a
    /// superuser with BYPASSRLS, and PostgreSQL always bypasses RLS for such roles, so
    /// switching it on now would provide no isolation while risking an outage. These
    /// tests make that deferral explicit and deliberate: if someone later enables RLS
    /// without first moving the application onto a NOBYPASSRLS role, the guard test
    /// fails and explains why.
    /// </para>
    /// </summary>
    public class TenantRlsMigrationTests
    {
        private const string RlsMigrationId = "20260926150343_AddTenantRowLevelSecurityPolicies";

        private static ApplicationDbContext CreateDesignTimeContext()
            => new ApplicationDbContextFactory().CreateDbContext(Array.Empty<string>());

        /// <summary>Runs a migration's Up/Down and returns the SQL it would execute.</summary>
        private static string RunMigration(Migration migration, bool up)
        {
            var builder = new MigrationBuilder("Npgsql");
            var method = typeof(Migration).GetMethod(
                up ? "Up" : "Down",
                BindingFlags.Instance | BindingFlags.NonPublic);

            method.Should().NotBeNull("EF Core exposes Up/Down as protected methods");
            method.Invoke(migration, new object[] { builder });

            return string.Join(
                Environment.NewLine,
                builder.Operations.OfType<SqlOperation>().Select(o => o.Sql));
        }

        /// <summary>The replacement RLS migration must be discoverable by EF Core.</summary>
        [Fact]
        public void RlsMigration_IsDiscoverableByEfCore()
        {
            using var context = CreateDesignTimeContext();

            var migrations = context.GetService<IMigrationsAssembly>().Migrations;

            migrations.Should().ContainKey(RlsMigrationId,
                "the replacement RLS migration must be discoverable, unlike the dead one it replaces");
        }

        /// <summary>Up() builds the tenant infrastructure the design depends on.</summary>
        [Fact]
        public void Up_CreatesAppSchemaAndTenantFunctions()
        {
            var sql = RunMigration(new AddTenantRowLevelSecurityPolicies(), up: true);

            sql.Should().Contain("CREATE SCHEMA IF NOT EXISTS app");
            sql.Should().Contain("CREATE OR REPLACE FUNCTION app.current_tenant_id()");
            sql.Should().Contain("CREATE OR REPLACE FUNCTION app.enable_tenant_rls");
        }

        /// <summary>Up() creates the per-table tenant policies for every tenant-scoped table.</summary>
        [Fact]
        public void Up_CreatesTenantPoliciesForAllFourOperations()
        {
            var sql = RunMigration(new AddTenantRowLevelSecurityPolicies(), up: true);

            sql.Should().Contain("CREATE POLICY %I ON %I FOR SELECT");
            sql.Should().Contain("CREATE POLICY %I ON %I FOR INSERT");
            sql.Should().Contain("CREATE POLICY %I ON %I FOR UPDATE");
            sql.Should().Contain("CREATE POLICY %I ON %I FOR DELETE");

            // Discovery must be dynamic so identifier casing and later tables are handled.
            sql.Should().Contain("information_schema.columns");
        }

        /// <summary>
        /// The deliberate deferral: RLS must NOT be enabled yet, because the application
        /// role bypasses RLS and switching it on would not actually isolate tenants.
        /// </summary>
        [Fact]
        public void Up_DoesNotInvokeTheRlsEnableHelper_BecauseAppRoleBypassesIt()
        {
            var sql = RunMigration(new AddTenantRowLevelSecurityPolicies(), up: true);

            // The helper is defined (its body legitimately contains the ALTER TABLE
            // statement) but must never be invoked by the migration itself.
            sql.Should().Contain("CREATE OR REPLACE FUNCTION app.enable_tenant_rls");
            sql.Should().NotContain("SELECT app.enable_tenant_rls",
                "RLS is inert for a superuser/BYPASSRLS role. Enable it only after the " +
                "application is moved onto a NOBYPASSRLS role, otherwise it provides no " +
                "tenant isolation while risking an outage.");
            sql.Should().NotContain("PERFORM app.enable_tenant_rls");
        }

        /// <summary>Down() must fully reverse the migration.</summary>
        [Fact]
        public void Down_RemovesPoliciesFunctionsAndSchema()
        {
            var sql = RunMigration(new AddTenantRowLevelSecurityPolicies(), up: false);

            sql.Should().Contain("DROP POLICY IF EXISTS");
            sql.Should().Contain("DROP FUNCTION IF EXISTS app.enable_tenant_rls(text)");
            sql.Should().Contain("DROP FUNCTION IF EXISTS app.current_tenant_id()");
            sql.Should().Contain("DROP SCHEMA IF EXISTS app");
        }
    }
}
