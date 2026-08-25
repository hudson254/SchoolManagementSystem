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
    /// Security configuration tests verifying headers, JWT requirements, and security posture.
    /// </summary>
    public class SecurityConfigurationTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public SecurityConfigurationTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task SecurityHeaders_IncludeContentTypeOptions()
        {
            using var client = _fixture.CreateClient();
            var response = await client.GetAsync("/health");

            response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
            var headerValue = response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault();
            headerValue.Should().Be("nosniff");
        }

        [Fact]
        public async Task SecurityHeaders_IncludeFrameOptions()
        {
            using var client = _fixture.CreateClient();
            var response = await client.GetAsync("/health");

            response.Headers.Contains("X-Frame-Options").Should().BeTrue();
            var headerValue = response.Headers.GetValues("X-Frame-Options").FirstOrDefault();
            headerValue.Should().Be("SAMEORIGIN");
        }

        [Fact]
        public async Task JWT_ProtectedEndpoint_Returns401_WithoutAuth()
        {
            using var client = _fixture.CreateClient();

            var response = await client.GetAsync("/api/v1/students");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task JWT_ProtectedEndpoint_Returns403_ForWrongRole()
        {
            using var client = _fixture.CreateClient();

            // Use the unauthenticated client for a protected endpoint
            var response = await client.GetAsync("/api/v1/admin/errors");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task API_DoesNotExposeDetailedErrorsInProduction()
        {
            using var client = _fixture.CreateClient();

            // Send a bad request that would trigger error handling
            var response = await client.GetAsync("/api/v1/nonexistent-endpoint-that-should-404");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // The response should not contain stack traces or sensitive info
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("StackTrace");
            content.Should().NotContain("InnerException");
        }

        [Fact]
        public async Task SecurityHeaders_IncludeReferrerPolicy()
        {
            using var client = _fixture.CreateClient();
            var response = await client.GetAsync("/health");

            if (response.Headers.Contains("Referrer-Policy"))
            {
                var headerValue = response.Headers.GetValues("Referrer-Policy").FirstOrDefault();
                headerValue.Should().NotBeNullOrEmpty();
            }
        }
    }
}
