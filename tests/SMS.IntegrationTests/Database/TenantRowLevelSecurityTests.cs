using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Database-layer proof that PostgreSQL - not the application - enforces
    /// tenant isolation on READS.
    ///
    /// <para><b>What makes these different from the application-layer tests.</b>
    /// <c>AccommodationIsolationTests</c> proves that an HTTP request issued
    /// as Tenant A never surfaces Tenant B's rows. It proves the EF Core
    /// query filter works. These prove the <i>database</i> refuses, by
    /// deliberately removing every application-layer defence:</para>
    /// <list type="bullet">
    /// <item><c>IgnoreQueryFilters()</c> strips the EF tenant filter and the
    /// foreign rows are still invisible.</item>
    /// <item>Raw SQL bypasses Entity Framework entirely - no filter, no
    /// interceptor - and is still invisible.</item>
    /// <item>A missing, empty or malformed tenant context yields nothing.</item>
    /// </list>
    ///
    /// <para>Every "cannot see" assertion is paired with a ground-truth read
    /// <i>as the table owner</i>, which bypasses the policies. Without it a
    /// zero-row result would be indistinguishable from a seed that silently
    /// failed, which is how vacuous isolation tests get written.</para>
    ///
    /// <para>Every test runs as <c>sms_app</c>: NOSUPERUSER, NOBYPASSRLS, and
    /// not the owner of any table. Those attributes are asserted first,
    /// because a suite that silently ran as a superuser would prove nothing.
    /// </para>
    /// <para>Joined to <see cref="TenantRowLevelSecurityCollection"/> rather
    /// than declaring an <c>IClassFixture</c> of its own. Both this class and
    /// <see cref="TenantRowLevelSecurityWriteTests"/> mutate the same seed
    /// rows in the same database, so they must share one fixture instance and
    /// must not run concurrently - see that collection for the full
    /// reasoning.</para>
    /// </summary>
    [Collection(TenantRowLevelSecurityCollection.Name)]
    public class TenantRowLevelSecurityTests
    {
        private readonly TenantRowLevelSecurityFixture _fixture;

        public TenantRowLevelSecurityTests(TenantRowLevelSecurityFixture fixture)
        {
            _fixture = fixture;
        }

        private static Guid TenantA => TenantRowLevelSecurityFixture.TenantA;
        private static Guid TenantB => TenantRowLevelSecurityFixture.TenantB;
        private static Guid TenantC => TenantRowLevelSecurityFixture.TenantC;

        /// <summary>
        /// Ground truth: the owning role is exempt from its own table's
        /// policies, so it sees every row regardless of tenant.
        /// </summary>
        private Task<long> OwnerLaneCountAsync(Guid? tenantId = null)
            => tenantId is null
                ? _fixture.ExecuteAsOwnerScalarAsync("select count(*) from \"Lanes\"")
                : _fixture.ExecuteAsOwnerScalarAsync(
                    "select count(*) from \"Lanes\" where tenant_id = @t", ("t", tenantId.Value));

        // ==================================================================
        // Preconditions. Without these the assertions below could pass for
        // entirely the wrong reason.
        // ==================================================================

        [Fact]
        public async Task RuntimeRole_IsLeastPrivilege_NotSuperuserAndNotBypassRls()
        {
            var self = await _fixture.ExecuteRawTextAsync(
                @"select case when rolsuper then 'super' else 'normal' end
                        || '|' ||
                        case when rolbypassrls then 'bypass' else 'nobypass' end
                        || '|' ||
                        case when rolcreatedb then 'createdb' else 'nocreatedb' end
                        || '|' ||
                        case when rolcreaterole then 'createrole' else 'nocreaterole' end
                   from pg_roles where rolname = current_user",
                TenantA);

            self.Should().Be("normal|nobypass|nocreatedb|nocreaterole",
                "every assertion in this class is meaningless unless the connection really is a " +
                "NOSUPERUSER, NOBYPASSRLS, NOCREATEDB, NOCREATEROLE role");

            var ownsProtectedTables = await _fixture.ExecuteRawScalarAsync(
                @"select count(*) from pg_class c
                    join pg_namespace n on n.oid = c.relnamespace
                   where n.nspname = 'public'
                     and c.relkind = 'r'
                     and c.relrowsecurity
                     and pg_get_userbyid(c.relowner) = current_user",
                TenantA);

            ownsProtectedTables.Should().Be(0,
                "the runtime role must not own any RLS-protected table, or it would be exempt from " +
                "the very policies this suite is testing");
        }

        [Fact]
        public async Task Rls_IsActuallyEnabled_OnTheTablesUnderTest()
        {
            foreach (var table in new[] { "Lanes", "Houses", "AccommodationAssignments", "Students", "AspNetUsers" })
            {
                var enabled = await _fixture.ExecuteRawScalarAsync(
                    "select count(*) from pg_class c join pg_namespace n on n.oid = c.relnamespace " +
                    "where n.nspname = 'public' and c.relname = @t and c.relkind = 'r' and c.relrowsecurity",
                    TenantA,
                    ("t", table));

                enabled.Should().Be(1, $"{table} must have row level security switched on");
            }
        }

        [Fact]
        public async Task Seed_IsReal_BothTenantsHaveRows_BeforeAnyIsolationIsAsserted()
        {
            (await OwnerLaneCountAsync(TenantA)).Should().BeGreaterThan(0,
                "Tenant A must own a lane for the positive controls to mean anything");

            (await OwnerLaneCountAsync(TenantB)).Should().BeGreaterThan(0,
                "Tenant B must own a lane, otherwise every 'cannot see' assertion would pass vacuously");
        }

        // ==================================================================
        // Positive controls. A model that returned nothing at all would also
        // pass every isolation assertion below, so own-tenant access is
        // asserted explicitly.
        // ==================================================================

        [Fact]
        public async Task TenantA_CanReadItsOwnRows()
        {
            await using var context = _fixture.CreateContext(TenantA);

            var lanes = await context.Lanes.IgnoreQueryFilters().ToListAsync();

            lanes.Should().NotBeEmpty(
                "positive control: a policy that hides everything passes no isolation test");
            lanes.Should().OnlyContain(l => l.TenantId == TenantA);
            lanes.Should().Contain(l => l.Id == _fixture.TenantALaneId);
        }

        [Fact]
        public async Task TenantB_CanReadItsOwnRows()
        {
            await using var context = _fixture.CreateContext(TenantB);

            var lanes = await context.Lanes.IgnoreQueryFilters().ToListAsync();

            lanes.Should().NotBeEmpty();
            lanes.Should().OnlyContain(l => l.TenantId == TenantB);
            lanes.Should().Contain(l => l.Id == _fixture.TenantBLaneId);
        }

        [Fact]
        public async Task RawSql_TenantA_SeesOnlyItsOwnRows()
        {
            var visible = await _fixture.ExecuteRawScalarAsync("select count(*) from \"Lanes\"", TenantA);
            var allLanes = await OwnerLaneCountAsync();

            visible.Should().BeGreaterThan(0);
            visible.Should().BeLessThan(allLanes,
                "raw SQL must be filtered by the policy even though it bypasses EF entirely");
        }

        // ==================================================================
        // IgnoreQueryFilters() cannot bypass RLS. This is the decisive
        // difference from the application-layer tests.
        // ==================================================================

        [Fact]
        public async Task IgnoreQueryFilters_CannotReachTheOtherTenantsRows()
        {
            await using var context = _fixture.CreateContext(TenantA);

            // Ground truth first: the row really is there.
            (await OwnerLaneCountAsync(TenantB)).Should().BeGreaterThan(0);

            var all = await context.Lanes.IgnoreQueryFilters().ToListAsync();

            all.Should().NotBeEmpty();
            all.Should().NotContain(l => l.Id == _fixture.TenantBLaneId,
                "EF query filters are disabled, so only the database can be holding the boundary");
            all.Should().OnlyContain(l => l.TenantId == TenantA);
        }

        [Fact]
        public async Task IgnoreQueryFilters_WithTheForeignTenantsOwnId_StillReturnsNothing()
        {
            await using var context = _fixture.CreateContext(TenantA);

            var foreign = await context.Lanes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == _fixture.TenantBLaneId);

            foreign.Should().BeNull(
                "addressing another tenant's row by primary key must not return it, even with every " +
                "application-layer filter removed");
        }

        [Fact]
        public async Task RawSql_AddressedDirectlyAtTheForeignTenantsRow_ReturnsNothing()
        {
            var byId = await _fixture.ExecuteRawScalarAsync(
                "select count(*) from \"Lanes\" where id = @id", TenantA, ("id", _fixture.TenantBLaneId));

            byId.Should().Be(0, "raw SQL naming the foreign row by primary key must still be filtered");
        }

        [Fact]
        public async Task RawSql_AsTheOwningRole_SeesTheForeignRow_ProvingTheSeedIsReal()
        {
            // The complementary half of the raw-SQL proof: as the OWNER the
            // policies do not apply, and the same query DOES return the row.
            // If this ever returned 0, the "Tenant A cannot see it" assertions
            // would be worthless.
            var asOwner = await _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where id = @id", ("id", _fixture.TenantBLaneId));

            asOwner.Should().Be(1,
                "the foreign lane must genuinely exist for the isolation assertions to mean anything");
        }

        // ==================================================================
        // Fail closed.
        // ==================================================================

        [Fact]
        public async Task MissingTenantContext_ReturnsNoRows()
        {
            await using var context = _fixture.CreateContextWithRawTenantId(string.Empty);

            var lanes = await context.Lanes.IgnoreQueryFilters().ToListAsync();

            lanes.Should().BeEmpty("no tenant context must mean no tenant rows - never 'all rows'");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-guid")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("'; DROP TABLE \"Lanes\"; --")]
        public async Task InvalidTenantContext_ReturnsNoRows(string malformed)
        {
            await using var context = _fixture.CreateContextWithRawTenantId(malformed);

            var lanes = await context.Lanes.IgnoreQueryFilters().ToListAsync();

            lanes.Should().BeEmpty(
                $"a malformed tenant context ('{malformed}') must resolve to the sentinel, not to a tenant");
        }

        [Fact]
        public async Task UnknownButWellFormedTenant_ReturnsNoRows()
        {
            await using var context = _fixture.CreateContext(TenantC);

            var lanes = await context.Lanes.IgnoreQueryFilters().ToListAsync();

            lanes.Should().BeEmpty("a syntactically valid tenant that owns nothing must see nothing");
        }

        [Fact]
        public async Task RawSql_WithNoTenantContext_ReturnsNoRows()
        {
            await using var connection = new NpgsqlConnection(TenantRowLevelSecurityFixture.RuntimeConnectionString);
            await connection.OpenAsync();

            // Publish the empty context explicitly, mirroring what the
            // interceptor does when no tenant has been resolved.
            await using (var publish = connection.CreateCommand())
            {
                publish.CommandText = "SELECT set_config('app.tenant_id', '', false)";
                await publish.ExecuteNonQueryAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "select count(*) from \"Lanes\"";

            Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Tenants_RegistryRemainsReadableWithoutATenantContext()
        {
            // The documented exception. Tenants is the bootstrap registry: it
            // is read to DISCOVER the tenant, so policing it would be circular
            // and every request would answer 400 "Invalid tenant". This pins
            // the deliberate exemption so it cannot be "fixed" by enabling RLS
            // on it.
            await using var connection = new NpgsqlConnection(TenantRowLevelSecurityFixture.RuntimeConnectionString);
            await connection.OpenAsync();

            await using (var publish = connection.CreateCommand())
            {
                publish.CommandText = "SELECT set_config('app.tenant_id', '', false)";
                await publish.ExecuteNonQueryAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "select count(*) from \"Tenants\"";

            Convert.ToInt64(await command.ExecuteScalarAsync()).Should().BeGreaterThan(0,
                "tenant resolution depends on this; without it every request would fail");
        }
    }
}
