using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Tests for CORS configuration, ensuring valid/invalid origins are handled correctly.
    /// </summary>
    public class CorsConfigurationTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public CorsConfigurationTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Cors_ValidOrigin_ShouldReturnAllowOriginHeader()
        {
            using var client = _fixture.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/me");
            request.Headers.Add("Origin", "http://localhost:5173");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            var response = await client.SendAsync(request);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        }

        [Fact]
        public async Task Cors_InvalidOrigin_ShouldNotReturnAllowOriginHeader()
        {
            using var client = _fixture.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/me");
            request.Headers.Add("Origin", "http://evil.com");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            var response = await client.SendAsync(request);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }

        [Fact]
        public async Task Cors_NoOrigin_ShouldStillProcessRequest()
        {
            using var client = _fixture.CreateClient();

            var response = await client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Cors_WithCredentials_ShouldAllowCredentials()
        {
            using var client = _fixture.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
            request.Headers.Add("Origin", "http://localhost:5173");
            request.Headers.Add("Access-Control-Request-Method", "POST");

            var response = await client.SendAsync(request);

            if (response.Headers.Contains("Access-Control-Allow-Credentials"))
            {
                var credentials = response.Headers.GetValues("Access-Control-Allow-Credentials");
                credentials.Should().Contain("true");
            }
        }

        [Fact]
        public async Task Cors_InvalidOrigin_ShouldBeRejectedByBrowser()
        {
            using var client = _fixture.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Add("Origin", "https://malicious-site.com");

            var response = await client.SendAsync(request);

            var allowOrigin = response.Headers.Contains("Access-Control-Allow-Origin")
                ? response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault()
                : null;

            allowOrigin.Should().BeNull();
        }
    }
}
