using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Phase 2C integration tests for the OMS order endpoints
    /// (/api/v1/oms/orders). Exercises the full HTTP -> MediatR -> Application
    /// -> PostgreSQL pipeline through the shared ApiTestFixture (admin
    /// principal, default tenant). Cancellation authorization and tenant
    /// isolation scenarios live in OmsOrderCancellationApiTests.
    /// </summary>
    public class OmsOrdersApiTests : IClassFixture<ApiTestFixture>
    {
        private const string BaseUrl = "/api/v1/oms/orders";
        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly ApiTestFixture _fixture;

        public OmsOrdersApiTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        private static object ValidOrderPayload(decimal unitPrice = 25m) => new
        {
            title = "Integration test order",
            description = "Created by OMS API integration tests",
            items = new object[]
            {
                new { itemCode = "ITM-001", description = "Chalk boxes", quantity = 10, unitPrice },
                new { itemCode = "ITM-002", description = "Whiteboard markers", quantity = 4, unitPrice = 50m }
            }
        };

        // The domain validates the canonical format ORD-yyyy-nnnnnn (see
        // Order.Create). Static sequence in a range reserved for this class so
        // the shared PostgreSQL database never sees a duplicate (unique index
        // on (TenantId, OrderNumber)).
        private static int _orderNumberSeq = Random.Shared.Next(100000, 550000);
        private static string NewOrderNumber() =>
            $"ORD-{DateTime.UtcNow.Year}-{System.Threading.Interlocked.Increment(ref _orderNumberSeq) % 1000000:D6}";

        // The API serializes enums as strings (Program.cs registers
        // JsonStringEnumConverter), so client-side reads need matching options.
        private static readonly System.Text.Json.JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<T>(ApiJsonOptions))!;

        private static async Task<T> GetJsonAsync<T>(HttpClient client, string url) =>
            (await client.GetFromJsonAsync<T>(url, ApiJsonOptions))!;

        private async Task<OrderDto> CreateOrderViaApiAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync(BaseUrl, ValidOrderPayload());
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Create order failed: {(int)response.StatusCode} {body}");
            }
            return (await response.Content.ReadFromJsonAsync<OrderDto>(ApiJsonOptions))!;
        }

        private async Task<Guid> SeedOrderAsync(string creatorUserId, bool submitted = false)
        {
            using var scope = _fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = Order.Create(
                NewOrderNumber(), "Seeded order " + NewOrderNumber(), null, creatorUserId,
                OmsMoney.DefaultCurrency, null, null);
            order.AddItem("ITM-S1", "Seeded item", 2, 100m);
            order.TenantId = DefaultTenantId;
            if (submitted)
            {
                order.Submit("seed-user", "seeduser");
            }
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        }

        private (string Email, string Password) SeedRoleUser(string role)
        {
            using var scope = _fixture.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var email = "oms-" + role.ToLowerInvariant() + "-" + Guid.NewGuid().ToString("N")[..8] + "@test.school";
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                NormalizedUserName = email.ToUpperInvariant(),
                NormalizedEmail = email.ToUpperInvariant(),
                FirstName = "OMS",
                LastName = "Tester",
                PhoneNumber = "555-0000",
                Organization = "Test Org",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                RefreshToken = string.Empty,
                TenantId = DefaultTenantId
            };
            var createResult = userManager.CreateAsync(user, "Test123!@#Xyz").GetAwaiter().GetResult();
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to create test user: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
            userManager.AddToRoleAsync(user, role).GetAwaiter().GetResult();
            return (email, "Test123!@#Xyz");
        }

        private async Task<HttpClient> CreateTokenClientAsync(string email, string password)
        {
            var token = await _fixture.GetAuthTokenAsync(email, password);
            var client = _fixture.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");
            return client;
        }

        [Fact]
        public async Task Create_WithValidPayload_Returns201WithDraftOrder()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.PostAsJsonAsync(BaseUrl, ValidOrderPayload());
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var dto = await response.Content.ReadFromJsonAsync<OrderDto>(ApiJsonOptions);
            dto.Should().NotBeNull();
            dto!.OrderNumber.Should().StartWith("ORD-");
            dto.StatusName.Should().Be("Draft");
            dto.RequestedByUserId.Should().Be("test-user-id");
            dto.Items.Should().HaveCount(2);
            dto.TotalAmount.Should().Be(10 * 25m + 4 * 50m);
        }

        [Fact]
        public async Task Create_WithMissingTitle_Returns400ValidationError()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.PostAsJsonAsync(BaseUrl, new { title = "", items = Array.Empty<object>() });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetById_ExistingOrder_Returns200WithOrder()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            var response = await client.GetAsync($"{BaseUrl}/{created.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await response.Content.ReadFromJsonAsync<OrderDto>(ApiJsonOptions);
            dto.Should().NotBeNull();
            dto!.Id.Should().Be(created.Id);
            dto.OrderNumber.Should().Be(created.OrderNumber);
        }

        [Fact]
        public async Task GetById_UnknownOrder_Returns404()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task List_ReturnsPagedEnvelope()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            await CreateOrderViaApiAsync(client);
            await CreateOrderViaApiAsync(client);
            var response = await client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=10");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>(ApiJsonOptions);
            page.Should().NotBeNull();
            page!.Items.Should().NotBeNull();
            page.TotalCount.Should().BeGreaterThanOrEqualTo(2);
            page.PageNumber.Should().Be(1);
            page.PageSize.Should().Be(10);
            page.TotalPages.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task List_FilterByRequestedByUser_ReturnsOnlyMatchingOrders()
        {
            var creator = "oms-list-" + Guid.NewGuid().ToString("N")[..8];
            await SeedOrderAsync(creator);
            await SeedOrderAsync(creator);
            await SeedOrderAsync("oms-list-other-" + Guid.NewGuid().ToString("N")[..8]);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.GetAsync($"{BaseUrl}?requestedByUserId={creator}&pageSize=50");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>(ApiJsonOptions);
            page!.TotalCount.Should().Be(2);
            page.Items.Should().OnlyContain(o => o.RequestedByUserId == creator);
        }

        [Fact]
        public async Task List_Pagination_SecondPageOfTwo_ReturnsSingleRemainingItem()
        {
            var creator = "oms-page-" + Guid.NewGuid().ToString("N")[..8];
            await SeedOrderAsync(creator);
            await SeedOrderAsync(creator);
            await SeedOrderAsync(creator);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.GetAsync($"{BaseUrl}?requestedByUserId={creator}&pageNumber=2&pageSize=2");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>(ApiJsonOptions);
            page!.TotalCount.Should().Be(3);
            page.Items.Should().HaveCount(1);
            page.HasPreviousPage.Should().BeTrue();
            page.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task List_FilterByStatus_ReturnsOnlyMatchingStatus()
        {
            var creator = "oms-status-" + Guid.NewGuid().ToString("N")[..8];
            await SeedOrderAsync(creator);
            var submittedId = await SeedOrderAsync(creator, submitted: true);
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.GetAsync($"{BaseUrl}?requestedByUserId={creator}&status=Submitted&pageSize=50");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>(ApiJsonOptions);
            page!.TotalCount.Should().Be(1);
            page.Items.Single().Id.Should().Be(submittedId);
        }

        [Fact]
        public async Task Submit_DraftOrder_Returns200WithSubmittedStatus()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            var response = await client.PostAsync($"{BaseUrl}/{created.Id}/submit", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await response.Content.ReadFromJsonAsync<OrderDto>(ApiJsonOptions);
            dto!.StatusName.Should().Be("Submitted");
            dto.SubmittedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task Submit_AlreadySubmittedOrder_Returns400()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            await client.PostAsync($"{BaseUrl}/{created.Id}/submit", null);
            var second = await client.PostAsync($"{BaseUrl}/{created.Id}/submit", null);
            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddItem_DraftOrder_Returns200WithComputedLineTotal()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            var response = await client.PostAsJsonAsync(
                $"{BaseUrl}/{created.Id}/items",
                new { description = "Projector bulbs", quantity = 3, unitPrice = 20m });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var item = await response.Content.ReadFromJsonAsync<OrderItemDto>(ApiJsonOptions);
            item.Should().NotBeNull();
            item!.LineTotal.Should().Be(60m);
            item.OrderId.Should().Be(created.Id);
            var reloaded = await GetJsonAsync<OrderDto>(client, $"{BaseUrl}/{created.Id}");
            reloaded!.ItemCount.Should().Be(3);
            reloaded.TotalAmount.Should().Be(450m + 60m);
        }

        [Fact]
        public async Task AddItem_NonDraftOrder_Returns400()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            await client.PostAsync($"{BaseUrl}/{created.Id}/submit", null);
            var response = await client.PostAsJsonAsync(
                $"{BaseUrl}/{created.Id}/items",
                new { description = "Late item", quantity = 1, unitPrice = 5m });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddItem_UnknownOrder_Returns404()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.PostAsJsonAsync(
                $"{BaseUrl}/{Guid.NewGuid()}/items",
                new { description = "Ghost item", quantity = 1, unitPrice = 5m });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AddItem_WithZeroQuantity_Returns400ValidationError()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            var response = await client.PostAsJsonAsync(
                $"{BaseUrl}/{created.Id}/items",
                new { description = "Bad quantity", quantity = 0, unitPrice = 5m });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RemoveItem_DraftOrder_Returns200AndRemovesItem()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            var itemId = created.Items.First().Id;
            var response = await client.DeleteAsync($"{BaseUrl}/{created.Id}/items/{itemId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var reloaded = await GetJsonAsync<OrderDto>(client, $"{BaseUrl}/{created.Id}");
            reloaded!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task RemoveItem_NonDraftOrder_Returns400()
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var created = await CreateOrderViaApiAsync(client);
            await client.PostAsync($"{BaseUrl}/{created.Id}/submit", null);
            var itemId = created.Items.First().Id;
            var response = await client.DeleteAsync($"{BaseUrl}/{created.Id}/items/{itemId}");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Unauthenticated_Request_Returns401()
        {
            using var client = _fixture.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");
            var response = await client.GetAsync(BaseUrl);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task StudentRole_CreateOrder_Returns403Forbidden()
        {
            var (email, password) = SeedRoleUser("Student");
            using var client = await CreateTokenClientAsync(email, password);
            var response = await client.PostAsJsonAsync(BaseUrl, ValidOrderPayload());
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CoordinatorRole_CancelAny_Returns403Forbidden()
        {
            var orderId = await SeedOrderAsync("test-user-id");
            var (email, password) = SeedRoleUser("Coordinator");
            using var client = await CreateTokenClientAsync(email, password);
            var response = await client.PostAsJsonAsync(
                $"{BaseUrl}/{orderId}/cancel-any", new { reason = "Coordinator escalation attempt" });
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}