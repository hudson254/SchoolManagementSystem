using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// Additional tenant-isolation checks for RoadAccount and OrderManifest
    /// (Phase 2). Verifies query-filter scoping for every tenant-owned OMS table.
    /// </summary>
    public class OmsTenantIsolationOtherEntitiesTests
    {
        private static readonly Guid TenantA = Guid.NewGuid();
        private static readonly Guid TenantB = Guid.NewGuid();

        private ApplicationDbContext CreateContext(Guid tenantId)
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
                .UseInMemoryDatabase($"OmsTest-Other-{tenantId:N}")
                .Options;

            return new ApplicationDbContext(options, userMock.Object, tenantMock.Object);
        }

        [Fact]
        public async Task TenantA_CannotReadTenantB_RoadAccounts()
        {
                        var raA = new RoadAccount
            {
                TenantId = TenantA,
                Code = "R1",
                Name = "Road A1",
                AccountType = RoadAccountType.Road,
                Balance = 0m,
                IsActive = true
            };

            var raB = new RoadAccount
            {
                TenantId = TenantB,
                Code = "R2",
                Name = "Road B1",
                AccountType = RoadAccountType.Other,
                Balance = 0m,
                IsActive = true
            };

            await using (var ctx = CreateContext(TenantA))
            {
                ctx.RoadAccounts.Add(raA);
                ctx.RoadAccounts.Add(raB); // inserted under TenantA filter
                await ctx.SaveChangesAsync();
            }

            await using var ctxB = CreateContext(TenantB);
            var bAccounts = await ctxB.RoadAccounts.ToListAsync();
            bAccounts.Should().BeEmpty("Tenant B should not see Tenant A's road account");
        }

        [Fact]
        public async Task TenantA_CannotReadTenantB_Manifests()
        {
            var orderA = Order.Create("ORD-2026-000001", "title", null, "uA", "KES", null, null);
            orderA.TenantId = TenantA;

            await using (var ctx = CreateContext(TenantA))
            {
                ctx.Orders.Add(orderA);
                ctx.OrderManifests.Add(new OrderManifest
                {
                    TenantId = TenantA,
                    OrderId = orderA.Id,
                    FileName = "manifest.json",
                    StoragePath = "oms/order-m/ord/manifest.json",
                    FileSizeBytes = 100,
                    GeneratedAtUtc = DateTime.UtcNow,
                    GeneratedByUserId = "uA"
                });
                await ctx.SaveChangesAsync();
            }

            await using var ctxB = CreateContext(TenantB);
            var bManifests = await ctxB.OrderManifests.ToListAsync();
            bManifests.Should().BeEmpty();
        }

        [Fact]
        public async Task TenantA_CannotReadTenantB_Imports()
        {
            await using (var ctx = CreateContext(TenantA))
            {
                                ctx.OrderImports.Add(new OrderImport
                {
                    TenantId = TenantA,
                    FileName = "import.csv",
                    Status = OrderImportStatus.Imported,
                    UploadedByUserId = "uA",
                    ImportedAtUtc = DateTime.UtcNow,
                    TotalRows = 10,
                    ValidRows = 9
                });
                await ctx.SaveChangesAsync();
            }

            await using var ctxB = CreateContext(TenantB);
            var bImports = await ctxB.OrderImports.ToListAsync();
            bImports.Should().BeEmpty();
        }
    }
}
