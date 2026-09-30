using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Database-layer proof that row level security protects WRITES, and that
    /// a pooled connection cannot carry one tenant's context into another
    /// tenant's request.
    ///
    /// <para>The read tests in <see cref="TenantRowLevelSecurityTests"/> would
    /// still pass if the policies only filtered SELECT. These assert the other
    /// three commands - and, just as importantly, assert the <i>positive</i>
    /// case for each one. A policy that rejected every write would satisfy a
    /// naive "writes are blocked" test while leaving the application
    /// completely broken.</para>
    /// <para>Joined to <see cref="TenantRowLevelSecurityCollection"/> rather
    /// than declaring an <c>IClassFixture</c> of its own. This class INSERTs,
    /// UPDATEs and soft-DELETEs <c>Lanes</c> rows for both tenants while
    /// <see cref="TenantRowLevelSecurityTests"/> asserts on live row counts in
    /// the same database, so the two must share one fixture instance and must
    /// not run concurrently - see that collection for the full reasoning.</para>
    /// </summary>
    [Collection(TenantRowLevelSecurityCollection.Name)]
    public class TenantRowLevelSecurityWriteTests
    {
        private readonly TenantRowLevelSecurityFixture _fixture;

        public TenantRowLevelSecurityWriteTests(TenantRowLevelSecurityFixture fixture)
        {
            _fixture = fixture;
        }

        private static Guid TenantA => TenantRowLevelSecurityFixture.TenantA;
        private static Guid TenantB => TenantRowLevelSecurityFixture.TenantB;

        /// <summary>The INSERT the runtime role issues for a lane.</summary>
        private const string InsertLaneSql =
            @"INSERT INTO ""Lanes"" (id, tenant_id, ""LaneName"", ""Description"", ""IsActive"",
                                 ""NumberingFormat"", ""StartingHouseNumber"", created_at, updated_at, is_deleted)
                  VALUES (@id, @tenant, @name, 'rls write test', true, 'A-000', 1, now(), now(), false)";

        /// <summary>
        /// A fresh lane id per test.
        ///
        /// <para>A single shared id does not work: soft-deleted rows keep
        /// their primary key, so the second test to use the same id collides
        /// with the first test's leftover row and fails with a duplicate-key
        /// error that has nothing to do with row level security.</para>
        /// </summary>
        private static Guid NewLaneId() => Guid.NewGuid();

        /// <summary>
        /// A lane name that is unique per run.
        ///
        /// <para>Lanes carry a unique index on (LaneName, TenantId) and a soft
        /// delete only flips a flag, so a fixed name collides with the
        /// leftover row from a previous run. Deriving the name from the id
        /// keeps the tests independent of what earlier runs left behind.</para>
        /// </summary>
        private static string UniqueName(string prefix, Guid laneId) => $"{prefix}-{laneId:N}";

        private static Lane NewLane(Guid id, string name) => new()
        {
            Id = id,
            LaneName = name,
            IsActive = true,
            NumberingFormat = "A-000",
            StartingHouseNumber = 1
        };

        private Task<long> CountLanesOwnedByAsync(Guid laneId)
            => _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where id = @id", ("id", laneId));

        private Task<long> CountLanesOwnedByAsyncFor(Guid laneId, Guid tenantId)
            => _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where id = @id and tenant_id = @t",
                ("id", laneId), ("t", tenantId));

        private async Task SeedLaneAsync(Guid laneId, string name)
        {
            await using var context = _fixture.CreateContext(TenantA);
            context.Lanes.Add(NewLane(laneId, name));
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-deletes a row this class created. Uses raw SQL because the
        /// tests assert on physical presence and a soft delete only flips a
        /// flag.
        /// </summary>
        private Task SoftDeleteAsync(Guid laneId)
            => _fixture.ExecuteRawNonQueryAsync(
                @"UPDATE ""Lanes"" SET is_deleted = true, ""Description"" = 'cleaned up'
                   WHERE id = @id AND tenant_id = @t",
                TenantA, ("id", laneId), ("t", TenantA));

        // ==================================================================
        // INSERT
        // ==================================================================

        [Fact]
        public async Task Insert_IntoOwnTenant_Succeeds()
        {
            var laneId = NewLaneId();
            await using var context = _fixture.CreateContext(TenantA);

            context.Lanes.Add(NewLane(laneId, UniqueName("OWN-TENANT-INSERT", laneId)));

            var act = async () => await context.SaveChangesAsync();
            await act.Should().NotThrowAsync(
                "the application must still be able to write its own tenant's data");

            await SoftDeleteAsync(laneId);
        }

        [Fact]
        public async Task Insert_CannotBeExpressedForAnotherTenant_EvenThroughTheOrm()
        {
            var laneId = NewLaneId();
            await using var context = _fixture.CreateContext(TenantA);

            context.Lanes.Add(new Lane
            {
                Id = laneId,
                LaneName = UniqueName("CROSS-TENANT-INJECT", laneId),
                // Ask for Tenant B explicitly.
                TenantId = TenantB,
                IsActive = true,
                NumberingFormat = "A-000",
                StartingHouseNumber = 1
            });

            await context.SaveChangesAsync();

            // SaveChanges re-stamps TenantId from the resolved tenant
            // context, so the row cannot be written for another tenant even
            // if a caller asks for it. The database-level guarantee for
            // hand-written SQL is covered by the RawSql test below.
            var owner = await _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where id = @id and tenant_id = @t",
                ("id", laneId), ("t", TenantA));

            owner.Should().Be(1, "the row must have been written for Tenant A, the resolved tenant");
            (await CountLanesOwnedByAsyncFor(laneId, TenantB)).Should().Be(0,
                "no row may exist for the tenant the caller asked for");
        }

        [Fact]
        public async Task RawSql_Insert_ForAnotherTenant_IsRejectedByTheDatabase()
        {
            var laneId = NewLaneId();

            var act = () => _fixture.ExecuteRawNonQueryAsync(
                InsertLaneSql, TenantA,
                ("id", laneId), ("tenant", TenantB), ("name", "RAW-CROSS-TENANT"));

            await act.Should().ThrowAsync<PostgresException>(
                "raw SQL is subject to the same WITH CHECK; no application filter is involved");

            (await CountLanesOwnedByAsync(laneId)).Should().Be(0);
        }

        // ==================================================================
        // UPDATE
        // ==================================================================

        [Fact]
        public async Task Update_OfOwnTenantsRow_Succeeds()
        {
            var laneId = NewLaneId();
            await SeedLaneAsync(laneId, UniqueName("BEFORE", laneId));

            await using (var context = _fixture.CreateContext(TenantA))
            {
                var lane = await context.Lanes.FirstAsync(l => l.Id == laneId);
                lane.Description = "updated by its own tenant";

                var act = async () => await context.SaveChangesAsync();
                await act.Should().NotThrowAsync("ordinary updates must keep working under RLS");
            }

            await using (var verify = _fixture.CreateContext(TenantA))
            {
                var lane = await verify.Lanes.FirstAsync(l => l.Id == laneId);
                lane.Description.Should().Be("updated by its own tenant");
            }

            await SoftDeleteAsync(laneId);
        }

        [Fact]
        public async Task Update_OfAnotherTenantsRow_AffectsNothing()
        {
            var rows = await _fixture.ExecuteRawNonQueryAsync(
                "UPDATE \"Lanes\" SET \"Description\" = 'HIJACKED' WHERE tenant_id = @t",
                TenantA, ("t", TenantB));

            rows.Should().Be(0,
                "another tenant's rows must be invisible to the UPDATE, so zero rows can match");

            var hijacked = await _fixture.ExecuteAsOwnerScalarAsync(
                @"select count(*) from ""Lanes""
                   where tenant_id = @t and coalesce(""Description"", '') = 'HIJACKED'",
                ("t", TenantB));

            hijacked.Should().Be(0, "no Tenant B row may have been modified");
        }

        [Fact]
        public async Task Update_MovingOwnRowIntoAnotherTenant_IsRejectedByTheDatabase()
        {
            var laneId = NewLaneId();
            await SeedLaneAsync(laneId, UniqueName("MOVES-OUT", laneId));

            await using (var context = _fixture.CreateContext(TenantA))
            {
                var lane = await context.Lanes.FirstAsync(l => l.Id == laneId);
                lane.TenantId = TenantB;

                var act = async () => await context.SaveChangesAsync();
                await act.Should().ThrowAsync<DbUpdateException>(
                    "the UPDATE policy's WITH CHECK must prevent a row being moved into another tenant");
            }

            var stillOurs = await _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where id = @id and tenant_id = @t",
                ("id", laneId), ("t", TenantA));

            stillOurs.Should().Be(1, "the row must still belong to Tenant A");

            await SoftDeleteAsync(laneId);
        }

        // ==================================================================
        // DELETE
        // ==================================================================

        [Fact]
        public async Task Delete_OfAnotherTenantsRow_AffectsNothing()
        {
            var before = await _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where tenant_id = @t", ("t", TenantB));

            var deleted = await _fixture.ExecuteRawNonQueryAsync(
                "DELETE FROM \"Lanes\" WHERE tenant_id = @t", TenantA, ("t", TenantB));

            deleted.Should().Be(0, "another tenant's rows must be invisible to DELETE");

            var after = await _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where tenant_id = @t", ("t", TenantB));

            after.Should().Be(before, "Tenant B's rows must all still be present");
        }

        [Fact]
        public async Task Delete_OfOwnTenantsRow_Succeeds()
        {
            var laneId = NewLaneId();
            await SeedLaneAsync(laneId, UniqueName("DELETE-ME", laneId));

            await using var context = _fixture.CreateContext(TenantA);

            var lane = await context.Lanes.FirstAsync(l => l.Id == laneId);
            context.Lanes.Remove(lane);

            var act = async () => await context.SaveChangesAsync();
            await act.Should().NotThrowAsync("ordinary deletes must keep working under RLS");

            // Delete is a soft delete: SaveChangesAsync rewrites EntityState
            // .Deleted into .Modified with IsDeleted set. The assertion is
            // therefore on the flag, not on physical absence.
            var softDeleted = await _fixture.ExecuteAsOwnerScalarAsync(
                "select count(*) from \"Lanes\" where id = @id and is_deleted",
                ("id", laneId));

            softDeleted.Should().Be(1, "the tenant's own row must have been soft deleted");
        }

        // ==================================================================
        // Connection pooling. A session-scoped setting survives on a physical
        // connection after it is returned to the pool, so this is the failure
        // mode most likely to leak a tenant across requests.
        // ==================================================================

        [Fact]
        public async Task PooledConnection_TenantB_DoesNotInheritTenantAsContext()
        {
            // Tenant A first: warms a pooled connection and stamps it.
            await using (var contextA = _fixture.CreateContext(TenantA))
            {
                var lanes = await contextA.Lanes.IgnoreQueryFilters().ToListAsync();
                lanes.Should().NotBeEmpty();
                lanes.Should().OnlyContain(l => l.TenantId == TenantA);
            }
            // The context is disposed here; the physical connection returns to
            // the pool still carrying A's app.tenant_id.

            // Tenant B now borrows a connection from that same pool.
            await using var contextB = _fixture.CreateContext(TenantB);

            var lanesB = await contextB.Lanes.IgnoreQueryFilters().ToListAsync();

            lanesB.Should().NotBeEmpty();
            lanesB.Should().OnlyContain(l => l.TenantId == TenantB,
                "Tenant B must never observe Tenant A's rows through a reused pooled connection");
            lanesB.Should().NotContain(l => l.Id == _fixture.TenantALaneId);
            lanesB.Should().Contain(l => l.Id == _fixture.TenantBLaneId);
        }

        [Fact]
        public async Task PooledConnection_TenantA_DoesNotInheritTenantBsContext()
        {
            // Reverse order, so the leak would be A seeing B's rows.
            await using (var contextB = _fixture.CreateContext(TenantB))
            {
                (await contextB.Lanes.IgnoreQueryFilters().ToListAsync()).Should().NotBeEmpty();
            }

            await using var contextA = _fixture.CreateContext(TenantA);

            var lanesA = await contextA.Lanes.IgnoreQueryFilters().ToListAsync();

            lanesA.Should().NotBeEmpty();
            lanesA.Should().OnlyContain(l => l.TenantId == TenantA,
                "Tenant A must never observe Tenant B's rows through a reused pooled connection");
            lanesA.Should().NotContain(l => l.Id == _fixture.TenantBLaneId);
        }

        [Fact]
        public async Task PooledConnection_AfterATenantRequest_AnUnresolvedContextSeesNothing()
        {
            // The worst case: Tenant A used the connection, then something runs
            // with no tenant at all - background work, a scheduler, an
            // unauthenticated endpoint. It must see nothing rather than
            // inheriting A.
            await using (var contextA = _fixture.CreateContext(TenantA))
            {
                (await contextA.Lanes.IgnoreQueryFilters().ToListAsync()).Should().NotBeEmpty();
            }

            await using var unresolved = _fixture.CreateContextWithRawTenantId(string.Empty);

            var lanes = await unresolved.Lanes.IgnoreQueryFilters().ToListAsync();

            lanes.Should().BeEmpty(
                "a pooled connection must never let an unresolved request inherit the previous " +
                "tenant's context");
        }

        [Fact]
        public async Task RawSql_PooledConnection_RepublishesTheContextOnEveryRental()
        {
            // Raw ADO.NET, pooled, alternating tenants on one physical
            // connection. Each rental republishes before any command runs.
            await using var connection = new NpgsqlConnection(TenantRowLevelSecurityFixture.RuntimeConnectionString);
            await connection.OpenAsync();

            await TenantRowLevelSecurityFixture.PublishTenantAsync(connection, TenantA);
            (await ScalarAsync(connection,
                "select count(*) from \"Lanes\" where tenant_id = @t", ("t", TenantA)))
                .Should().BeGreaterThan(0);

            await TenantRowLevelSecurityFixture.PublishTenantAsync(connection, TenantB);
            (await ScalarAsync(connection,
                "select count(*) from \"Lanes\" where tenant_id = @t", ("t", TenantA)))
                .Should().Be(0, "re-publishing must switch the effective tenant");
            (await ScalarAsync(connection,
                "select count(*) from \"Lanes\" where tenant_id = @t", ("t", TenantB)))
                .Should().BeGreaterThan(0);
        }

        private static async Task<long> ScalarAsync(
            NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);

            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
    }
}
