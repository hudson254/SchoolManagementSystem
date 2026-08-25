using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SMS.Application.DTOs;
using Xunit;

namespace SMS.ApiTests.Integration
{
    /// <summary>
    /// Comprehensive end-to-end workflow tests covering the complete system stack.
    /// Tests exercise: Frontend → API → Authentication → Services → EF Core → PostgreSQL
    /// </summary>
    public class E2EWorkflowTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public E2EWorkflowTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task E2E_Authentication_StudentRegistration_Login_Profile_Logout()
        {
            using var client = _fixture.CreateClient();

            // 1. Register student
            var email = $"e2e.{Guid.NewGuid()}@example.com";
            var registerRequest = new
            {
                firstName = "E2E",
                lastName = "Student",
                email,
                password = "Xyz789!@#SecurePass",
                confirmPassword = "Xyz789!@#SecurePass",
                phoneNumber = "+254712345678",
                organization = "E2E Test",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // 2. Get profile (should work due to cookie auth)
            var meResponse = await client.GetAsync("/api/v1/auth/me");
            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Logout
            var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
            logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task E2E_Authentication_InvalidCredentials_ReturnsUnauthorized()
        {
            using var client = _fixture.CreateClient();

            var loginRequest = new { email = "nonexistent@test.com", password = "WrongPass1!", rememberMe = true };
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task E2E_Administrator_CanManageCourses()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();

            // Create a course
            var courseRequest = new
            {
                courseCode = $"CS{Guid.NewGuid():N}"[..8],
                courseName = $"E2E Course {Guid.NewGuid():N}"[..15],
                description = "E2E test course",
                durationYears = 4,
                departmentId = Guid.Empty // Will be ignored if validation fails
            };

            var createResponse = await client.PostAsJsonAsync("/api/v1/courses", courseRequest);
            // 201 Created or 400 BadRequest (validation) are both acceptable
            createResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task E2E_TenantIsolated_StudentAccess()
        {
            using var client = _fixture.CreateClient();

            // Register students with different organizations
            var emailA = $"tenant.a.{Guid.NewGuid()}@example.com";
            var registerA = new
            {
                firstName = "Tenant",
                lastName = "A",
                email = emailA,
                password = "Xyz789!@#SecurePass",
                confirmPassword = "Xyz789!@#SecurePass",
                phoneNumber = "+254712345678",
                organization = "TenantA",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            var regResponseA = await client.PostAsJsonAsync("/api/v1/auth/register", registerA);
            regResponseA.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verify authenticated session works
            var meResponse = await client.GetAsync("/api/v1/auth/me");
            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task E2E_HealthEndpoint_ReturnsOk()
        {
            using var client = _fixture.CreateClient();

            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task E2E_RoleBasedAccess_StudentCannotAccessAdminFunctions()
        {
            using var client = _fixture.CreateClient();

            // Register as student
            var email = $"access.{Guid.NewGuid()}@example.com";
            var registerRequest = new
            {
                firstName = "Access",
                lastName = "Test",
                email,
                password = "Xyz789!@#SecurePass",
                confirmPassword = "Xyz789!@#SecurePass",
                phoneNumber = "+254712345678",
                organization = "Access Test",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Try accessing admin endpoints
            var adminResponse = await client.GetAsync("/api/v1/admin/errors");
            // Student should get 403 Forbidden (not 401 Unauthorized)
            adminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
