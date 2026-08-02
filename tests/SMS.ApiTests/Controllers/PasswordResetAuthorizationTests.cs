using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Regression tests for the ADMIN-RESET security fix. The
    /// PasswordResetController previously had NO [Authorize] attribute, so any
    /// anonymous caller could list, fulfill, or reject password-reset requests
    /// (arbitrary account takeover). The controller now requires the
    /// "AdministratorAccess" policy (Administrator role).
    /// </summary>
    public class PasswordResetAuthorizationTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public PasswordResetAuthorizationTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetRequests_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange — anonymous client (no bearer token)
            using var client = _fixture.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/admin/password-resets");

            // Assert — the endpoint must reject anonymous callers
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetRequests_WithAdministratorToken_ShouldReturnOk()
        {
            // Arrange — authenticated admin client
            using var client = await _fixture.CreateAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/api/v1/admin/password-resets");

            // Assert — the endpoint must be reachable by an Administrator
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task FulfillRequest_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange — anonymous client (no bearer token)
            using var client = _fixture.CreateClient();

            // Act — attempt to fulfill a password reset without authentication
            var response = await client.PostAsJsonAsync(
                "/api/v1/admin/password-resets/{00000000-0000-0000-0000-000000000000}/fulfill",
                new { adminUserId = "admin-1", resolutionNote = "hacked" });

            // Assert — the endpoint must reject anonymous callers
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RejectRequest_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange — anonymous client (no bearer token)
            using var client = _fixture.CreateClient();

            // Act — attempt to reject a password reset without authentication
            var response = await client.PostAsJsonAsync(
                "/api/v1/admin/password-resets/{00000000-0000-0000-0000-000000000000}/reject",
                new { adminUserId = "admin-1", resolutionNote = "hacked" });

            // Assert — the endpoint must reject anonymous callers
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
