using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SMS.Application.DTOs;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    public class AuthControllerTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public AuthControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// RISK-08: tokens are no longer returned in the JSON body — they are
        /// set as httpOnly Set-Cookie headers. Extract the access_token cookie
        /// value for assertions.
        /// </summary>
        private static string GetAccessTokenCookie(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                foreach (var header in values)
                {
                    var firstPart = header.Split(';')[0].Trim();
                    var eq = firstPart.IndexOf('=');
                    if (eq > 0 && firstPart.Substring(0, eq).Trim() == "access_token")
                        return firstPart.Substring(eq + 1).Trim();
                }
            }
            return string.Empty;
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOk()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var loginRequest = new
            {
                email = "admin@school.com",
                password = "Admin123!",
                rememberMe = true
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result!.Email.Should().Be(loginRequest.email);
            // Token is in the httpOnly cookie, not the body.
            GetAccessTokenCookie(response).Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var loginRequest = new
            {
                email = "admin@school.com",
                password = "WrongPassword!",
                rememberMe = true
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var loginRequest = new
            {
                email = "nonexistent@school.com",
                password = "SomePassword!",
                rememberMe = true
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var registerRequest = new
            {
                firstName = "Register",
                lastName = "Test",
                email = $"register.{System.Guid.NewGuid()}@example.com",
                password = "Test123!@#Abc",
                confirmPassword = "Test123!@#Abc",
                phoneNumber = "+254712345678",
                organization = "Test School",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result!.Email.Should().Be(registerRequest.email);
            // Token is in the httpOnly cookie, not the body.
            GetAccessTokenCookie(response).Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var email = $"duplicate.{System.Guid.NewGuid()}@example.com";

            // First register a user
            var firstRequest = new
            {
                firstName = "First",
                lastName = "User",
                email = email,
                password = "Test123!@#Abc",
                confirmPassword = "Test123!@#Abc",
                phoneNumber = "+254712345678",
                organization = "Test School",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };
            await client.PostAsJsonAsync("/api/v1/auth/register", firstRequest);

            // Try to register again with same email
            var duplicateRequest = new
            {
                firstName = "Second",
                lastName = "User",
                email = email,
                password = "Test123!@#Abc",
                confirmPassword = "Test123!@#Abc",
                phoneNumber = "+254712345679",
                organization = "Test School",
                role = "Student",
                courseId = ApiTestFixture.TestCourseId
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/register", duplicateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var registerRequest = new
            {
                firstName = "",
                lastName = "",
                email = "invalid-email",
                password = "weak",
                confirmPassword = "different",
                phoneNumber = "",
                role = "InvalidRole"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetCurrentUser_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            using var client = _fixture.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_ShouldReturnNoContent()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var request = new
            {
                email = "admin@school.com"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ForgotPassword_WithNonExistentEmail_ShouldReturnNoContent()
        {
            // Arrange
            using var client = _fixture.CreateClient();
            var request = new
            {
                email = "nonexistent@school.com"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}

