using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SMS.Domain.Common;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Regression tests for the coordinator repair: a COORDINATOR can manage
    /// academic operations (courses, units, course offerings, classes,
    /// timetable, calendar events) and view student information, but cannot
    /// delete protected resources or perform administrator-only actions.
    ///
    /// Requires the PostgreSQL test database (sms_test) — see
    /// docker-compose.test.yml.
    /// </summary>
    public class CoordinatorRoleApiTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public CoordinatorRoleApiTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        private static string RandomCoordinatorEmail() =>
            $"coord.{Guid.NewGuid():N}@example.com";

        private const string CoordinatorPassword = "CoordPass123!x";

        private async Task<(HttpClient client, string email)> CreateCoordinatorClientAsync()
        {
            var adminClient = await _fixture.CreateAuthenticatedClientAsync();

            var email = RandomCoordinatorEmail();
            var createUser = new
            {
                firstName = "Coordinator",
                lastName = "Test",
                email,
                password = CoordinatorPassword,
                role = DomainConstants.Roles.Coordinator,
                phoneNumber = "+254700000000",
            };

            var createResponse = await adminClient.PostAsJsonAsync("/api/v1/users", createUser);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
                $"creating coordinator user should succeed (HTTP {createResponse.StatusCode})");
            adminClient.Dispose();

            var token = await _fixture.GetAuthTokenAsync(email, CoordinatorPassword);
            token.Should().NotBeNullOrWhiteSpace();

            var client = _fixture.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return (client, email);
        }

        [Fact]
        public async Task Coordinator_CanReadAcademicLists()
        {
            var (client, _) = await CreateCoordinatorClientAsync();
            try
            {
                var paths = new[]
                {
                    "/api/v1/courses?page=1&pageSize=5",
                    "/api/v1/units?page=1&pageSize=5",
                    "/api/v1/courseoffering?page=1&pageSize=5",
                    "/api/v1/classes",
                    "/api/v1/timetables?page=1&pageSize=5",
                    "/api/v1/calendar-events",
                    "/api/v1/students?page=1&pageSize=5",
                };

                foreach (var path in paths)
                {
                    var response = await client.GetAsync(path);
                    response.StatusCode.Should().Be(HttpStatusCode.OK, $"{path} should be readable by coordinator");
                }
            }
            finally
            {
                client.Dispose();
            }
        }

        [Fact]
        public async Task Coordinator_CanCreateCourse()
        {
            var (client, _) = await CreateCoordinatorClientAsync();
            try
            {
                var code = "CC" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
                var create = new
                {
                    name = "Coord Regression Course",
                    code,
                    duration = 4,
                    totalCredits = 120,
                    isActive = true,
                };

                var response = await client.PostAsJsonAsync("/api/v1/courses", create);
                response.StatusCode.Should().Be(HttpStatusCode.Created,
                    $"coordinator create course should succeed (HTTP {response.StatusCode})");
            }
            finally
            {
                client.Dispose();
            }
        }

        [Fact]
        public async Task Coordinator_CanCreateCalendarEvent()
        {
            var (client, _) = await CreateCoordinatorClientAsync();
            try
            {
                var create = new
                {
                    title = $"Coord Event {Guid.NewGuid():N}",
                    description = "Created by coordinator regression test",
                    startDate = DateTime.UtcNow.AddDays(5),
                    endDate = DateTime.UtcNow.AddDays(5).AddHours(1),
                    eventType = "event",
                    location = "Conference Hall A",
                };

                var response = await client.PostAsJsonAsync("/api/v1/calendar-events", create);
                response.StatusCode.Should().Be(HttpStatusCode.Created,
                    $"coordinator create calendar event should succeed (HTTP {response.StatusCode})");
            }
            finally
            {
                client.Dispose();
            }
        }
[Fact]
        public async Task Coordinator_CannotDeleteCourse()
        {
            var (client, _) = await CreateCoordinatorClientAsync();
            try
            {
                var response = await client.DeleteAsync($"/api/v1/courses/{Guid.NewGuid()}");
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                    "coordinator must not be able to delete courses (AdministratorAccess only)");
            }
            finally
            {
                client.Dispose();
            }
        }

        [Fact]
        public async Task Coordinator_CannotDeleteCalendarEvent()
        {
            var (client, _) = await CreateCoordinatorClientAsync();
            try
            {
                var response = await client.DeleteAsync($"/api/v1/calendar-events/{Guid.NewGuid()}");
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                    "coordinator must not be able to delete calendar events (AdministratorAccess only)");
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}