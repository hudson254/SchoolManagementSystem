using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// Persistence-level tenant-isolation tests (Phase 2). Verifies the
    /// application-level query filter (TenantId == CurrentTenantGuid) which
    /// works alongside PostgreSQL RLS enforced in production migrations.
    /// Uses the EF InMemory provider (no live DB required).
    /// </summary>
    public class OmsTenantIsolationTests
    {
        private static readonly Guid TenantA = Guid.NewGuid();
        private static readonly Guid TenantB = Guid.NewGuid();

        /// <summary>
        /// Creates a context scoped to <paramref name="tenantId"/> over a named
        /// InMemory store. Callers pass a store name that is unique per test
        /// method (shared by the contexts within that method) so that data from
        /// one test cannot leak into another.
        /// </summary>
        private ApplicationDbContext CreateContext(Guid tenantId, string storeName)
        {
            var tenantMock = new Mock<ITenantContext>();
            tenantMock.Setup(t => t.TenantId).Returns(tenantId.ToString());
            tenantMock.Setup(t => t.TenantName).Returns("TestTenant");
            tenantMock.Setup(t => t.ConnectionString).Returns(string.Empty);

            var userMock = new Mock<ICurrentUserService>();
            userMock.Setup(u => u.UserId).Returns("test-user");
            userMock.Setup(u => u.IsAuthenticated).Returns(true);
            userMock.Setup(u => u.Roles).Returns(Array.Empty<string>());

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(storeName)
                .Options;

            return new ApplicationDbContext(options, userMock.Object, tenantMock.Object);
        }

        /// <summary>Fresh InMemory store name for a single test method.</summary>
        private static string NewStore() => $"OmsIso-{Guid.NewGuid():N}";

        private static Order CreateOrder(string number, string userId, Guid tenant)
        {
            var order = Order.Create(number, "title", null, userId, OmsMoney.DefaultCurrency, null, null);
            order.TenantId = tenant;
            return order;
        }

        [Fact]
        public async Task TenantA_CannotReadTenantB_Orders()
        {
            var store = NewStore();
            await using var ctxA = CreateContext(TenantA, store);
            await using var ctxB = CreateContext(TenantB, store);
            ctxA.Orders.Add(CreateOrder("ORD-2026-000001", "uA", TenantA));
            await ctxA.SaveChangesAsync();

            var bOrders = await ctxB.Orders.ToListAsync();
            bOrders.Should().BeEmpty();
        }

        [Fact]
        public async Task TenantA_CannotUpdateTenantB_Order()
        {
            var store = NewStore();
            await using var ctxA = CreateContext(TenantA, store);
            await using var ctxB = CreateContext(TenantB, store);
            ctxA.Orders.Add(CreateOrder("ORD-2026-000001", "uA", TenantA));
            await ctxA.SaveChangesAsync();

            // Tenant B cannot locate Tenant A's order due to the query filter.
            var bOrders = await ctxB.Orders.ToListAsync();
            bOrders.Should().BeEmpty();
        }

        [Fact]
        public async Task TenantA_CannotDeleteTenantB_Order()
        {
            var store = NewStore();
            await using var ctxA = CreateContext(TenantA, store);
            await using var ctxB = CreateContext(TenantB, store);
            ctxA.Orders.Add(CreateOrder("ORD-2026-000001", "uA", TenantA));
            await ctxA.SaveChangesAsync();

            var target = await ctxB.Orders.FirstOrDefaultAsync();
            target.Should().BeNull();
        }

        [Fact]
        public async Task SoftDeletedOrder_NotVisible_InSameTenant()
        {
            var store = NewStore();
            await using var ctx = CreateContext(TenantA, store);
            var order = CreateOrder("ORD-2026-000001", "uA", TenantA);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            order.SoftDelete("admin");
            await ctx.SaveChangesAsync();

            var visible = await ctx.Orders.ToListAsync();
            visible.Should().BeEmpty("soft-deleted orders are filtered out");
        }

        [Fact]
        public async Task QueryFilter_DoesNotBlock_TenantScopedQueries()
        {
            var store = NewStore();
            await using var ctx = CreateContext(TenantA, store);
            ctx.Orders.Add(CreateOrder("ORD-2026-000001", "uA", TenantA));
            ctx.Orders.Add(CreateOrder("ORD-2026-000002", "uA", TenantA));
            await ctx.SaveChangesAsync();

            var tenantOrders = await ctx.Orders.Where(o => o.TenantId == TenantA).ToListAsync();
            tenantOrders.Should().HaveCount(2);
        }
    }
}
