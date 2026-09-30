using System.Threading.Tasks;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Database-level proof of the least-privilege PRIVILEGE boundary, as
    /// distinct from the ROW boundary covered by the RLS tests.
    ///
    /// <para>RLS answers "which rows may this tenant see". Privilege answers
    /// "what may this role do to this table at all". Both are needed and they
    /// fail differently: a role with no SELECT on a table cannot read it
    /// regardless of RLS, and a role with full privilege on a table with no
    /// RLS can read everything regardless of policies. These tests therefore
    /// assert on <c>has_table_privilege</c> AND on the real outcome of the
    /// statement, as <c>sms_app</c>, against a real PostgreSQL server.</para>
    ///
    /// <para>Shares <see cref="TenantRowLevelSecurityCollection"/> with the RLS
    /// suites: same database, and the Tenants rows are shared seed data.</para>
    /// </summary>
    [Collection(TenantRowLevelSecurityCollection.Name)]
    public class LeastPrivilegeRolePrivilegeTests
    {
        private static Guid TenantA => TenantRowLevelSecurityFixture.TenantA;

        private readonly TenantRowLevelSecurityFixture _fixture;

        public LeastPrivilegeRolePrivilegeTests(TenantRowLevelSecurityFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Renders a boolean privilege check as the literal text 't'/'f'.
        ///
        /// <para>has_table_privilege returns a real PostgreSQL boolean, and
        /// Convert.ToString(bool) yields .NET's "True"/"False" rather than
        /// PostgreSQL's "t"/"f". Selecting the boolean directly would make the
        /// assertion depend on that formatting accident, so the mapping is
        /// done in SQL where it belongs.</para>
        /// </summary>
        private Task<string> PrivilegeAsync(string privilege) =>
            _fixture.ExecuteRawTextAsync(
                "select case when has_table_privilege('sms_app', 'public.\"Tenants\"', '" + privilege + "') " +
                "then 't' else 'f' end",
                TenantA);

        // ==================================================================
        // Tenants registry: readable, but read-only.
        // ==================================================================

        [Fact]
        public async Task Tenants_Select_IsAllowedForTheRuntimeRole()
        {
            // Tenant resolution reads the registry to DISCOVER which tenant the
            // request belongs to. Removing SELECT would make every request
            // answer 400 "Invalid tenant" and break login entirely.
            (await PrivilegeAsync("SELECT")).Should().Be("t");
        }

        [Theory]
        [InlineData("INSERT")]
        [InlineData("UPDATE")]
        [InlineData("DELETE")]
        [InlineData("TRUNCATE")]
        [InlineData("REFERENCES")]
        [InlineData("TRIGGER")]
        public async Task Tenants_WritePrivileges_AreDeniedForTheRuntimeRole(string privilege)
        {
            (await PrivilegeAsync(privilege)).Should().Be("f",
                $"sms_app must not hold {privilege} on the tenant registry: creating or rewriting a tenant row is a " +
                "privilege-escalation primitive, because it lets a compromised application repoint a subdomain or " +
                "flip IsActive for a tenant it does not own");
        }

        [Fact]
        public async Task Tenants_Insert_AsTheRuntimeRole_IsActuallyRefused()
        {
            // has_table_privilege is a catalogue read; this is the statement
            // itself, so the guarantee does not rest on trusting the query.
            var act = () => _fixture.ExecuteRawNonQueryAsync(
                @"insert into ""Tenants"" (id, ""Name"", ""Organization"", ""IsActive"", created_at) " +
                "values (gen_random_uuid(), 'x', 'x', true, now())",
                TenantA);

            await act.Should().ThrowAsync<PostgresException>(
                "the write must be refused by PostgreSQL, not merely absent from the grant list");
        }

        [Fact]
        public async Task Tenants_Update_AsTheRuntimeRole_IsActuallyRefused()
        {
            var act = () => _fixture.ExecuteRawNonQueryAsync(
                @"update ""Tenants"" set ""Name"" = 'hijacked'", TenantA);

            await act.Should().ThrowAsync<PostgresException>();
        }

        [Fact]
        public async Task Tenants_Delete_AsTheRuntimeRole_IsActuallyRefused()
        {
            var act = () => _fixture.ExecuteRawNonQueryAsync(
                @"delete from ""Tenants""", TenantA);

            await act.Should().ThrowAsync<PostgresException>();
        }

        [Fact]
        public async Task Tenants_RemainsReadableWithoutATenantContext()
        {
            // The documented exemption, re-asserted from the privilege side:
            // resolution happens with no tenant context yet, so the registry
            // must not be gated on app.tenant_id.
            var count = await _fixture.ExecuteRawScalarAsync(
                @"select count(*) from ""Tenants""", TenantA);

            count.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Tenants_IsNotProtectedByRls_AndIsNotForced()
        {
            // Pinning the design decision from both directions. It must not
            // have been "fixed" by enabling RLS on it (circular: the policy
            // would need app.tenant_id to decide who may read the table that
            // establishes app.tenant_id), and FORCE is out of scope and was
            // experimentally shown to break the migration/seed model.
            var rls = await _fixture.ExecuteRawTextAsync(
                @"select case when c.relrowsecurity then 't' else 'f' end || '|' ||
                        case when c.relforcerowsecurity then 't' else 'f' end
                    from pg_class c join pg_namespace n on n.oid = c.relnamespace
                   where n.nspname = 'public' and c.relname = 'Tenants'",
                TenantA);

            rls.Should().Be("f|f",
                "Tenants is protected by reduced privilege, not by RLS; adding RLS here would be a design change");
        }

        // ==================================================================
        // Role escalation. Every one of these must fail. A single success
        // means the runtime role is a key to the whole cluster and every
        // other control in this repository is decorative.
        // ==================================================================

        // Deliberately NOT in the list below: `REVOKE ... FROM sms_app`.
        // PostgreSQL lets any role revoke privileges GRANTED TO ITSELF without
        // being the owner, so that statement succeeds for sms_app. That is not
        // an escalation - revoking a privilege you hold only ever reduces your
        // own authority - and asserting that it throws would be asserting a
        // PostgreSQL fact that is simply false. Privilege GRANTS are the
        // escalation vector, and those are covered here.
        [Theory]
        [InlineData(@"create role esc_test")]
        [InlineData(@"create database esc_test")]
        [InlineData(@"create schema esc_test")]
        [InlineData(@"alter role sms_app superuser")]
        [InlineData(@"alter role sms_app bypassrls")]
        [InlineData(@"alter role sms_app createdb")]
        [InlineData(@"grant sms_migration to sms_app")]
        [InlineData(@"drop table ""Lanes""")]
        [InlineData(@"alter table ""Lanes"" disable row level security")]
        [InlineData(@"alter table ""Lanes"" force row level security")]
        [InlineData(@"drop table ""Tenants""")]
        [InlineData(@"select rolpassword from pg_authid")]
        [InlineData(@"select * from pg_authid")]
        [InlineData(@"copy (select rolname, rolpassword from pg_authid) to stdout")]
        [InlineData(@"select app.enable_tenant_rls('Lanes')")]
        [InlineData(@"set role sms_migration")]
        [InlineData(@"copy pg_shadow to stdout")]
        public async Task RoleEscalation_IsRefusedForTheRuntimeRole(string statement)
        {
            var act = () => _fixture.ExecuteRawNonQueryAsync(statement, TenantA);

            var thrown = await act.Should().ThrowAsync<PostgresException>(
                $"sms_app must not be able to run: {statement}");
            thrown.Which.SqlState.Should().Be(
                PostgresErrorCodes.InsufficientPrivilege,
                $"'{statement}' must fail specifically as a permission error, not for some incidental reason");
        }

        [Fact]
        public async Task RuntimeRole_CannotCreateObjectsInTheApplicationSchema()
        {
            var created = await _fixture.ExecuteRawTextAsync(
                "select case when has_schema_privilege('sms_app', 'public', 'CREATE') " +
                "then 't' else 'f' end", TenantA);

            created.Should().Be("f",
                "CREATE on the schema would let the runtime role define tables, functions and - crucially - policies");
        }

        [Fact]
        public async Task RuntimeRole_OwnsNoApplicationTables()
        {
            // Ownership is the strongest of the non-superuser powers: an owner
            // is exempt from that table's RLS policies, can alter and drop it,
            // and can hand the ownership on. If sms_app owned anything, "RLS is
            // enabled" would be meaningless for that table.
            var owned = await _fixture.ExecuteRawScalarAsync(
                @"select count(*) from pg_class c
                   join pg_namespace n on n.oid = c.relnamespace
                   join pg_roles r on r.oid = c.relowner
                  where n.nspname = 'public' and r.rolname = 'sms_app'",
                TenantA);

            owned.Should().Be(0, "sms_app must own nothing in the application schema");
        }

        [Fact]
        public async Task MigrationRole_RemainsNoBypassRls()
        {
            // sms_migration bypasses policies only because it OWNS the tables.
            // It is not BYPASSRLS, so the attribute must stay off: a leaked
            // migration credential must not be a walk-past-everything key.
            var flags = await _fixture.ExecuteRawTextAsync(
                @"select case when rolsuper then 'super' else 'normal' end || '|' ||
                        case when rolbypassrls then 'bypass' else 'nobypass' end || '|' ||
                        case when rolcreatedb then 'createdb' else 'nocreatedb' end
                   from pg_roles where rolname = 'sms_migration'",
                TenantA);

            flags.Should().Be("normal|nobypass|nocreatedb");
        }

        [Fact]
        public async Task RuntimeRole_IsNotAMemberOfTheMigrationRole()
        {
            var member = await _fixture.ExecuteRawTextAsync(
                "select case when pg_has_role('sms_app', 'sms_migration', 'MEMBER') " +
                "then 't' else 'f' end", TenantA);

            member.Should().Be("f",
                "membership would hand the runtime role the DDL capability directly");
        }

        [Fact]
        public async Task MigrationRole_CannotCreateRolesOrDatabases()
        {
            foreach (var statement in new[]
            {
                @"create role mig_esc",
                @"create database mig_esc",
                @"alter role sms_migration superuser",
                @"alter role sms_migration bypassrls",
            })
            {
                var act = () => _fixture.ExecuteAsOwnerNonQueryAsync(statement);
                var thrown = await act.Should().ThrowAsync<PostgresException>(
                    $"sms_migration must not be able to run: {statement}");
                thrown.Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
            }
        }
    }
}
