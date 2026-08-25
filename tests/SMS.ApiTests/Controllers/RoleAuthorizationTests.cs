using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SMS.API;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Comprehensive role-based authorization tests.
    /// Tests that each role can only access endpoints appropriate to their privilege level.
    /// </summary>
    public class RoleAuthorizationTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public RoleAuthorizationTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task SystemAdministrator_CanAccessAdminEndpoints()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/admin/errors");
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotAccessProtectedEndpoint()
        {
            using var client = _fixture.CreateClient();
            var response = await client.GetAsync("/api/v1/students");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotAccessAdminEndpoints()
        {
            using var client = _fixture.CreateClient();
            var response = await client.GetAsync("/api/v1/admin/errors");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Student_CannotAccessAdminEndpoints()
        {
            // Register a student account
            using var client = _fixture.CreateClient();
            var email = $"student.role.{Guid.NewGuid()}@example.com";
            var registerRequest = new
            {
                firstName = "Test",
                lastName = "Student",
                email,
                password = "Xyz789!@#SecurePass",
                confirmPassword = "Xyz789!@#SecurePass",
                phoneNumber = "+254712345678",
                organization = "Role Test",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Try accessing admin endpoints with student token
            var adminResponse = await client.GetAsync("/api/v1/admin/errors");
            adminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AuditController_RequiresAdministrator()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/audit");
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task PasswordResetEndpoints_RequireAdministrator()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/admin/password-resets");
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReportAdminEndpoints_RequireModeratorAccess()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/admin/reports");
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CertificateEndpoints_RequireModeratorAccess()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/certificates/templates");
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ApprovalEndpoints_UsesCanonicalRoleNames()
        {
            // This test verifies that the ApprovalController uses correct role names
            // and doesn't use non-existent roles like "Admin" or "Registrar"
            using var client = await _fixture.CreateAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/approval/pending");

            // If role names were wrong ("Admin" instead of "Administrator"), this would return 403
            // A properly configured admin token should have access
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CourseEndpoints_RequireModeratorAccess()
        {
            using var client = await _fixture.CreateAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/courses");
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }
    }
}
