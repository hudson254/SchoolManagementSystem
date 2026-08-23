using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SMS.Application.DTOs;
using Xunit;

namespace SMS.ApiTests.Integration
{
    public class FullFlowTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public FullFlowTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AuthFlow_RegisterLoginGetProfile_ShouldSucceed()
        {
            using var client = _fixture.CreateClient();
            var email = $"flow.{Guid.NewGuid()}@example.com";
            var registerRequest = new
            {
                firstName = "Flow",
                lastName = "Student",
                email,
                password = "Xyz789!@#SecurePass",
                confirmPassword = "Xyz789!@#SecurePass",
                phoneNumber = "+254712345678",
                organization = "Flow Test",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            registerResult.Should().NotBeNull();
            registerResult!.Email.Should().Be(email);

            // RISK-08: the register response sets the access_token as an
            // httpOnly cookie on this client. The /auth/me request below
            // automatically carries it (cookie auth), so no Authorization
            // header is needed.
            var meResponse = await client.GetAsync("/api/v1/auth/me");
            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
            logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task AuthFlow_RegisterWithTitle_UsernameShouldNotContainTitle()
        {
            using var client = _fixture.CreateClient();
            var email = $"titled.{Guid.NewGuid()}@example.com";
            var registerRequest = new
            {
                title = "Dr.",
                firstName = "John",
                lastName = "Mwangi",
                email,
                password = "Xyz789!@#SecurePass",
                confirmPassword = "Xyz789!@#SecurePass",
                phoneNumber = "+254712345678",
                organization = "Title Test",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            registerResult.Should().NotBeNull();
            registerResult!.Email.Should().Be(email);

            // Username should NOT contain the title "Dr" or "dr"
            var username = registerResult.Username ?? string.Empty;
            username.Should().NotBeNullOrEmpty();
            username.ToLowerInvariant().Should().NotContain("dr");

            // The full name should include the title
            registerResult.FullName.Should().Contain("Dr.");

            // Get profile to verify title is stored separately
            var meResponse = await client.GetAsync("/api/v1/auth/me");
            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
            logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task StudentFlow_CreateStudentGetStudents_ShouldSucceed()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var email = $"student.{Guid.NewGuid()}@example.com";
            var createCommand = new
            {
                firstName = "Create",
                lastName = "Student",
                email,
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Female"
            };

            var createResponse = await client.PostAsJsonAsync("/api/v1/students", createCommand);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var student = await createResponse.Content.ReadFromJsonAsync<StudentDto>();
            student.Should().NotBeNull();
            student!.Email.Should().Be(email);
            student.FirstName.Should().Be(createCommand.firstName);

            var getResponse = await client.GetAsync($"/api/v1/students/{student.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var deleteResponse = await client.DeleteAsync($"/api/v1/students/{student.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}

