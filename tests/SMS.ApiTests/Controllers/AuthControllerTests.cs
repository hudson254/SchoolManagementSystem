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
        private readonly HttpClient _client;

        public AuthControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.CreateClient();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOk()
        {
            // Arrange
            var loginRequest = new
            {
                email = "admin@school.com",
                password = "Admin123!",
                rememberMe = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(loginRequest.email);
            result.Roles.Should().Contain("SystemAdministrator");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                email = "admin@school.com",
                password = "WrongPassword!",
                rememberMe = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                email = "nonexistent@school.com",
                password = "SomePassword!",
                rememberMe = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var registerRequest = new
            {
                firstName = "Register",
                lastName = "Test",
                email = $"register.{System.Guid.NewGuid()}@example.com",
                password = "Test123!@#",
                confirmPassword = "Test123!@#",
                phoneNumber = "+254712345678",
                organization = "Test School",
                role = "Student"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(registerRequest.email);
            result.FirstName.Should().Be(registerRequest.firstName);
            result.LastName.Should().Be(registerRequest.lastName);
            result.Roles.Should().Contain(registerRequest.role);
            result.RequiresEmailVerification.Should().BeTrue();
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
        {
            // Arrange
            var email = $"duplicate.{System.Guid.NewGuid()}@example.com";

            // First register a user
            var firstRequest = new
            {
                firstName = "First",
                lastName = "User",
                email = email,
                password = "Test123!@#",
                confirmPassword = "Test123!@#",
                phoneNumber = "+254712345678",
                role = "Student"
            };
            await _client.PostAsJsonAsync("/api/v1/auth/register", firstRequest);

            // Try to register again with same email
            var duplicateRequest = new
            {
                firstName = "Second",
                lastName = "User",
                email = email,
                password = "Test123!@#",
                confirmPassword = "Test123!@#",
                phoneNumber = "+254712345679",
                role = "Student"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", duplicateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
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
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldReturnOk()
        {
            // Arrange
            // First login to get tokens
            var loginRequest = new
            {
                email = "admin@school.com",
                password = "Admin123!",
                rememberMe = true
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            var refreshRequest = new
            {
                refreshToken = loginResult.RefreshToken
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.AccessToken.Should().NotBe(loginResult.AccessToken);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var refreshRequest = new
            {
                refreshToken = "invalid-refresh-token"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentUser_WithAuthentication_ShouldReturnOk()
        {
            // Arrange
            var token = await _fixture.GetAuthTokenAsync("admin@school.com", "Admin123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<UserProfileDto>();
            result.Should().NotBeNull();
            result.Email.Should().Be("admin@school.com");
            result.Roles.Should().Contain("SystemAdministrator");
        }

        [Fact]
        public async Task GetCurrentUser_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            // No authentication token

            // Act
            var response = await _client.GetAsync("/api/v1/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_ShouldReturnNoContent()
        {
            // Arrange
            var request = new
            {
                email = "admin@school.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ForgotPassword_WithNonExistentEmail_ShouldReturnNoContent()
        {
            // Arrange
            var request = new
            {
                email = "nonexistent@school.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}