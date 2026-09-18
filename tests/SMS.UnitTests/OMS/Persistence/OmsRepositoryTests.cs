using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// Phase 2A tests for the OMS repository implementations
    /// (OrderRepository, RoadAccountRepository, OrderImportRepository) plus the
    /// UnitOfWork OMS wiring. Uses the EF InMemory provider with the real
    /// ApplicationDbContext so tenant/soft-delete query filters apply exactly
    /// as in production query paths.
    /// </summary>
    public class OmsRepositoryTests : IDisposable
    {
        private static readonly Guid TenantA = Guid.NewGuid();
        private static readonly Guid TenantB = Guid.NewGuid();

        private readonly ApplicationDbContext _context;
        private readonly OrderRepository _orders;
        private readonly RoadAccountRepository _roadAccounts;
        private readonly OrderImportRepository _imports;

        private readonly string _storeName = $"OmsRepo-{Guid.NewGuid():N}";

        public OmsRepositoryTests()
        {
            _context = CreateContext(TenantA, _storeName);
            _orders = new OrderRepository(_context, Mock.Of<ILogger<OrderRepository>>());
            _roadAccounts = new RoadAccountRepository(_context, Mock.Of<ILogger<RoadAccountRepository>>());
            _imports = new OrderImportRepository(_context, Mock.Of<ILogger<OrderImportRepository>>());
        }

        public void Dispose() => _context.Dispose();

        private static ApplicationDbContext CreateContext(Guid tenantId, string storeName)
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

        private static Order CreateOrder(string number, string userId) =>
            Order.Create(number, "Test title", null, userId, OmsMoney.DefaultCurrency, null, null);

        // ---- CRUD (Add / GetById / Update / soft Delete) ----

        [Fact]
        public async Task AddAsync_ThenGetByIdAsync_ReturnsOrder()
        {
            var order = CreateOrder("ORD-2026-000001", "user-1");
            await _orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var loaded = await _orders.GetByIdAsync(order.Id);
            loaded.Should().NotBeNull();
            loaded!.OrderNumber.Should().Be("ORD-2026-000001");
            loaded.Currency.Should().Be(OmsMoney.DefaultCurrency);
        }

        [Fact]
        public async Task GetByIdAsync_UnknownId_ReturnsNull()
        {
            (await _orders.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_PersistsTransition_WithoutGraphUpdateFailure()
        {
            // Regression guard: the overridden UpdateAsync must not mark the whole
            // object graph Modified (client-keyed child entities would make
            // SaveChanges throw DbUpdateConcurrencyException).
            var order = CreateOrder("ORD-2026-000002", "user-1");
            order.AddItem("CODE-1", "Widget", 2, 10.5m);
            await _orders.AddAsync(order);
            await _context.SaveChangesAsync();

            order.Submit("approver-1", "approver");
            await _orders.UpdateAsync(order);
            await _context.SaveChangesAsync();

            _context.Entry(order).State = EntityState.Detached;
            var loaded = await _orders.GetByIdWithDetailsAsync(order.Id);
            loaded!.Status.Should().Be(OrderStatus.Submitted);
            loaded.StatusHistory.Should().ContainSingle(h => h.ToStatus == OrderStatus.Submitted);
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesAndHidesFromQueries()
        {
            var order = CreateOrder("ORD-2026-000003", "user-1");
            await _orders.AddAsync(order);
            await _context.SaveChangesAsync();

            await _orders.DeleteAsync(order);
            await _context.SaveChangesAsync();

            order.IsDeleted.Should().BeTrue();
            (await _orders.GetByIdAsync(order.Id)).Should().BeNull();
            var (_, totalCount) = await _orders.GetPagedAsync(null, null, null, null, 1, 20);
            totalCount.Should().Be(0);
        }

        // ---- Business key & detail reads ----

        [Fact]
        public async Task GetByOrderNumberAsync_MatchesBusinessKey()
        {
            var order = CreateOrder("ORD-2026-000004", "user-1");
            await _orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var found = await _orders.GetByOrderNumberAsync("ORD-2026-000004");
            found!.Id.Should().Be(order.Id);
            (await _orders.GetByOrderNumberAsync("ORD-2026-999999")).Should().BeNull();
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_IncludesItems()
        {
            var order = CreateOrder("ORD-2026-000005", "user-1");
            order.AddItem("CODE-1", "Widget", 2, 10.5m);
            await _orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var details = await _orders.GetByIdWithDetailsAsync(order.Id);
            details.Should().NotBeNull();
            details!.Items.Should().HaveCount(1);
            details.TotalAmount.Should().Be(21m);
            (await _orders.GetByIdWithDetailsAsync(Guid.NewGuid())).Should().BeNull();
        }

        [Fact]
        public async Task GetStatusHistoryAsync_ReturnsTransitionEntries()
        {
            var order = CreateOrder("ORD-2026-000006", "user-1");
            order.AddItem("CODE-1", "Widget", 1, 5m);
            order.Submit("user-1", "submitter");
            await _orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var history = await _orders.GetStatusHistoryAsync(order.Id);
            history.Should().HaveCount(1);
            history[0].FromStatus.Should().Be(OrderStatus.Draft);
            history[0].ToStatus.Should().Be(OrderStatus.Submitted);
        }

        [Fact]
        public async Task GetAttachmentsAndManifests_ReturnLinkedChildren()
        {
            var order = CreateOrder("ORD-2026-000010", "user-1");
            await _orders.AddAsync(order);
            var attachment = new OrderAttachment
            {
                OrderId = order.Id,
                UploadFileId = Guid.NewGuid(),
                OriginalFileName = "quote.pdf",
                StoragePath = "oms/quote.pdf",
                FileSizeBytes = 123
            };
            var manifest = new OrderManifest
            {
                OrderId = order.Id,
                FileName = "manifest-000010.csv",
                StoragePath = "oms/manifest-000010.csv"
            };
            _context.OrderAttachments.Add(attachment);
            _context.OrderManifests.Add(manifest);
            await _context.SaveChangesAsync();

            (await _orders.GetAttachmentsAsync(order.Id)).Should().ContainSingle(a => a.Id == attachment.Id);
            (await _orders.GetManifestsAsync(order.Id)).Should().ContainSingle(m => m.Id == manifest.Id);
        }

        // ---- Paged listing & OMS reporting ----

        [Fact]
        public async Task GetPagedAsync_FiltersByStatus_AndReturnsTotalCount()
        {
            var draft = CreateOrder("ORD-2026-000007", "user-1");
            var submitted = CreateOrder("ORD-2026-000017", "user-1");
            submitted.AddItem("CODE-1", "Widget", 1, 1m);
            submitted.Submit("user-1", "submitter");
            await _orders.AddAsync(draft);
            await _orders.AddAsync(submitted);
            await _context.SaveChangesAsync();

            var (page, totalCount) = await _orders.GetPagedAsync(OrderStatus.Submitted, null, null, null, 1, 20);
            totalCount.Should().Be(1);
            page.Should().ContainSingle(o => o.OrderNumber == "ORD-2026-000017");

            var (_, allCount) = await _orders.GetPagedAsync(null, null, null, null, 1, 20);
            allCount.Should().Be(2);
        }

        [Fact]
        public async Task GetStatusCountsAsync_GroupsByStatus()
        {
            var draft = CreateOrder("ORD-2026-000020", "user-1");
            var submitted = CreateOrder("ORD-2026-000021", "user-1");
            submitted.AddItem("CODE-1", "Widget", 1, 1m);
            submitted.Submit("user-1", "submitter");
            await _orders.AddAsync(draft);
            await _orders.AddAsync(submitted);
            await _context.SaveChangesAsync();

            var counts = await _orders.GetStatusCountsAsync();
            counts[OrderStatus.Draft].Should().Be(1);
            counts[OrderStatus.Submitted].Should().Be(1);
            counts.ContainsKey(OrderStatus.Approved).Should().BeFalse();
        }

        [Fact]
        public async Task GetTotalByCurrencyAsync_SumsWithinDateWindow()
        {
            var older = CreateOrder("ORD-2026-000008", "user-1");
            older.AddItem("CODE-1", "Widget", 1, 10m);
            await _orders.AddAsync(older);
            await _context.SaveChangesAsync();
            var olderAt = older.CreatedAt;

            await Task.Delay(1100);

            var newer = CreateOrder("ORD-2026-000009", "user-1");
            newer.AddItem("CODE-2", "Gadget", 1, 20m);
            await _orders.AddAsync(newer);
            await _context.SaveChangesAsync();

            var all = await _orders.GetTotalByCurrencyAsync(null, null);
            all[OmsMoney.DefaultCurrency].Should().Be(30m);

            var recentOnly = await _orders.GetTotalByCurrencyAsync(olderAt.AddSeconds(1), null);
            recentOnly[OmsMoney.DefaultCurrency].Should().Be(20m);
        }

        // ---- Road accounts (persistence model, object initializer) ----

        [Fact]
        public async Task RoadAccounts_AddFindByCode_ListActive()
        {
            await _roadAccounts.AddAsync(new RoadAccount
            {
                Code = "ROAD-01",
                Name = "Nairobi Ring",
                AccountType = RoadAccountType.Road,
                Balance = 1000m
            });
            await _roadAccounts.AddAsync(new RoadAccount
            {
                Code = "ROAD-02",
                Name = "Retired Route",
                AccountType = RoadAccountType.Road,
                IsActive = false
            });
            await _context.SaveChangesAsync();

            var byCode = await _roadAccounts.GetByCodeAsync("ROAD-01");
            byCode!.Name.Should().Be("Nairobi Ring");
            byCode.Balance.Should().Be(1000m);

            var active = await _roadAccounts.GetActiveAsync();
            active.Should().ContainSingle(a => a.Code == "ROAD-01");
            (await _roadAccounts.GetByCodeAsync("ROAD-XX")).Should().BeNull();
        }

        // ---- Imports (persistence model, object initializer) ----

        [Fact]
        public async Task OrderImports_StageRows_GetRowsAndRecent()
        {
            var import = new OrderImport
            {
                FileName = "orders.csv",
                TotalRows = 2,
                ValidRows = 1,
                InvalidRows = 1,
                UploadedByUserId = "user-1"
            };
            await _imports.AddAsync(import);
            await _context.SaveChangesAsync();

            _context.OrderImportRows.Add(new OrderImportRow
            {
                ImportId = import.Id,
                RowNumber = 1,
                RawJson = "{ " + '"' + "row" + '"' + ": 1 }",
                IsValid = true
            });
            _context.OrderImportRows.Add(new OrderImportRow
            {
                ImportId = import.Id,
                RowNumber = 2,
                RawJson = "{ " + '"' + "row" + '"' + ": 2 }",
                IsValid = false,
                ErrorMessage = "Invalid quantity"
            });
            await _context.SaveChangesAsync();

            var rows = await _imports.GetRowsAsync(import.Id);
            rows.Select(r => r.RowNumber).Should().Equal(1, 2);
            rows.Last().IsValid.Should().BeFalse();
            rows.Last().ErrorMessage.Should().Be("Invalid quantity");

            var recent = await _imports.GetRecentAsync(10);
            recent.Should().Contain(i => i.Id == import.Id);
            recent.First().FileName.Should().Be("orders.csv");
        }

        // ---- Tenant scoping through the repositories ----

        [Fact]
        public async Task Repository_Queries_AreTenantScoped()
        {
            await _orders.AddAsync(CreateOrder("ORD-2026-000040", "user-A"));
            await _context.SaveChangesAsync();


            await using var ctxB = CreateContext(TenantB, _storeName);
            var repoB = new OrderRepository(ctxB, Mock.Of<ILogger<OrderRepository>>());

            (await repoB.GetByOrderNumberAsync("ORD-2026-000040")).Should().BeNull();
            var (_, tenantBCount) = await repoB.GetPagedAsync(null, null, null, null, 1, 20);
            tenantBCount.Should().Be(0);

            var (_, tenantACount) = await _orders.GetPagedAsync(null, null, null, null, 1, 20);
            tenantACount.Should().Be(1);
        }

        // ---- UnitOfWork OMS wiring ----

        [Fact]
        public void UnitOfWork_ExposesOmsRepositories_LazilyAndStably()
        {
            var uow = new UnitOfWork(
                _context,
                Mock.Of<ILogger<UnitOfWork>>(),
                Mock.Of<ILoggerFactory>());

            uow.Orders.Should().BeAssignableTo<IOrderRepository>();
            uow.RoadAccounts.Should().BeAssignableTo<IRoadAccountRepository>();
            uow.OrderImports.Should().BeAssignableTo<IOrderImportRepository>();

            uow.Orders.Should().BeSameAs(uow.Orders);
            uow.RoadAccounts.Should().BeSameAs(uow.RoadAccounts);
            uow.OrderImports.Should().BeSameAs(uow.OrderImports);
        }
    }
}
