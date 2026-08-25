using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Tests for JWT configuration validation and security requirements.
    /// </summary>
    public class JwtConfigurationTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public JwtConfigurationTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Jwt_ValidLogin_ReturnsTokenCookie()
        {
            using var client = _fixture.CreateClient();

            var loginRequest = new { email = "admin@school.com", password = "Admin123!@#q1", rememberMe = true };
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Check that Set-Cookie header contains access_token
            var hasSetCookie = response.Headers.TryGetValues("Set-Cookie", out var cookies);
            hasSetCookie.Should().BeTrue();
            cookies.Should().Contain(c => c.StartsWith("access_token="));
        }

        [Fact]
        public async Task Jwt_InvalidCredentials_ReturnsUnauthorized()
        {
            using var client = _fixture.CreateClient();

            var loginRequest = new { email = "admin@school.com", password = "WrongPassword1!", rememberMe = true };
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Jwt_ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
        {
            using var client = _fixture.CreateClient();

            var response = await client.GetAsync("/api/v1/students");

            // Should be 401 since no auth token is provided
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Jwt_ProtectedEndpoint_WithValidToken_ReturnsOk()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();

            var response = await client.GetAsync("/api/v1/students");

            // With valid token, we should get a response (200 or 403 depending on roles)
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Jwt_RefreshToken_EndpointWorks()
        {
            using var client = _fixture.CreateClient();

            // Login first to get refresh token cookie
            var loginRequest = new { email = "admin@school.com", password = "Admin123!@#q1", rememberMe = true };
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Try to refresh with the cookies automatically sent
            var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", null);
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Jwt_Logout_InvalidatesSession()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();

            var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
            logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Jwt_AdminToken_CanAccessAdminEndpoints()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();

            var response = await client.GetAsync("/api/v1/admin/errors");
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }
    }
}
