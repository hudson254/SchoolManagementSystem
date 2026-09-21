using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Phase 2C integration tests for OMS order cancellation authorization
    /// (creator-only cancellation vs. the broader Administrator permission)
    /// and cross-tenant isolation.
    /// </summary>
    public class OmsOrderCancellationApiTests : IClassFixture<ApiTestFixture>
    {
        private const string BaseUrl = "/api/v1/oms/orders";
        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OtherTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private readonly ApiTestFixture _fixture;

        public OmsOrderCancellationApiTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        // Separate sequence range from OmsOrdersApiTests so the two classes can
        // never collide on the unique (TenantId, OrderNumber) index.
        private static int _orderNumberSeq = Random.Shared.Next(550000, 999000);
        private static string NewOrderNumber() =>
            $"ORD-{DateTime.UtcNow.Year}-{System.Threading.Interlocked.Increment(ref _orderNumberSeq) % 1000000:D6}";

        // The API serializes enums as strings (Program.cs registers
        // JsonStringEnumConverter), so client-side reads need matching options.
        private static readonly System.Text.Json.JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        private async Task<Guid> SeedOrderAsync(string creatorUserId, Guid? tenantId = null, bool cancelled = false)
        {
            var targetTenant = tenantId ?? DefaultTenantId;

            using var scope = _fixture.Services.CreateScope();
            var order = Order.Create(
                NewOrderNumber(), "Cancellation test order", null, creatorUserId,
                OmsMoney.DefaultCurrency, null, null);
            order.AddItem("ITM-C1", "Cancellation item", 1, 10m);
            order.TenantId = targetTenant;
            if (cancelled)
            {
                order.Cancel("seed-admin", "seedadmin", "seeded cancellation");
            }

            if (targetTenant == DefaultTenantId)
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Orders.Add(order);
                await db.SaveChangesAsync();
            }
            else
            {
                // SaveChanges stamps tenant-aware entities with the tenant
                // resolved from ITenantContext, so a foreign-tenant row must be
                // written through a context whose tenant context reports that
                // tenant (same pattern as the OMS unit tenant-isolation tests).
                var connectionString = scope.ServiceProvider.GetRequiredService<IConfiguration>()
                    .GetConnectionString("DefaultConnection");
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;
                var userMock = new Mock<SMS.Domain.Interfaces.ICurrentUserService>();
                userMock.Setup(u => u.UserId).Returns("seed-user");
                userMock.Setup(u => u.IsAuthenticated).Returns(true);
                var tenantMock = new Mock<SMS.Domain.Interfaces.ITenantContext>();
                tenantMock.Setup(t => t.TenantId).Returns(targetTenant.ToString());
                await using var tenantDb = new ApplicationDbContext(options, userMock.Object, tenantMock.Object);
                // The TenantContextInterceptor is registered on the host's
                // DbContext options and is NOT applied to this manually-built
                // context, so set the RLS session variable explicitly for the
                // INSERT to satisfy the tenant_ policies (FORCE ROW LEVEL SECURITY).
                await tenantDb.Database.OpenConnectionAsync();
                await tenantDb.Database.ExecuteSqlRawAsync(
                    "SELECT set_config('app.tenant_id', '" + targetTenant + "', false)");
                tenantDb.Orders.Add(order);
                await tenantDb.SaveChangesAsync();
            }

            return order.Id;
        }

        private static Task<HttpResponseMessage> CancelAsync(
            HttpClient client, Guid orderId, object body) =>
            client.PostAsJsonAsync($"{BaseUrl}/{orderId}/cancel", body);

        [Fact]
        public async Task CancelOwn_CreatorCancelsOwnDraftOrder_Returns200Cancelled()
        {
            var orderId = await SeedOrderAsync("test-user-id");
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await CancelAsync(client, orderId, new { reason = "No longer needed" });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await response.Content.ReadFromJsonAsync<OrderDto>(ApiJsonOptions);
            dto!.StatusName.Should().Be("Cancelled");
            dto.CancellationReason.Should().Be("No longer needed");
            dto.CancelledByUserId.Should().Be("test-user-id");
        }

        [Fact]
        public async Task CancelOwn_NonCreatorOrder_Returns403Forbidden()
        {
            var orderId = await SeedOrderAsync("oms-other-creator-" + Guid.NewGuid().ToString("N")[..8]);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await CancelAsync(client, orderId, new { reason = "Attempt by non-creator" });
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CancelAny_AdministratorCancelsOtherUsersOrder_Returns200()
        {
            var orderId = await SeedOrderAsync("oms-other-creator-" + Guid.NewGuid().ToString("N")[..8]);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.PostAsJsonAsync(
                $"{BaseUrl}/{orderId}/cancel-any", new { reason = "Administrative cancellation" });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await response.Content.ReadFromJsonAsync<OrderDto>(ApiJsonOptions);
            dto!.StatusName.Should().Be("Cancelled");
        }

        [Fact]
        public async Task Cancel_AlreadyCancelledOrder_Returns400()
        {
            var orderId = await SeedOrderAsync("test-user-id", cancelled: true);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await CancelAsync(client, orderId, new { reason = "Second cancel attempt" });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Cancel_WithMissingReason_Returns400()
        {
            var orderId = await SeedOrderAsync("test-user-id");
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await CancelAsync(client, orderId, new { reason = "" });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Cancel_UnknownOrder_Returns404()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await CancelAsync(client, Guid.NewGuid(), new { reason = "Ghost order" });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_OrderFromAnotherTenant_Returns404()
        {
            var orderId = await SeedOrderAsync("test-user-id", OtherTenantId);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.GetAsync($"{BaseUrl}/{orderId}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Cancel_OrderFromAnotherTenant_Returns404()
        {
            var orderId = await SeedOrderAsync("test-user-id", OtherTenantId);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await CancelAsync(client, orderId, new { reason = "Cross tenant attempt" });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}