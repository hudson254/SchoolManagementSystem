using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SMS.Application.DTOs;
using SMS.Shared.DTOs;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    public class StudentControllerTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;

        public StudentControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetStudents_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            using var client = _fixture.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/students");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetStudents_WithAuthentication_ShouldReturnOk()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1/students?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateStudent_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            var command = new
            {
                firstName = "Create",
                lastName = "Test",
                email = $"create.{Guid.NewGuid()}@example.com",
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Female",
                address = "456 Create St"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/students", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<StudentDto>();
            result.Should().NotBeNull();
            result!.FirstName.Should().Be(command.firstName);
            result.LastName.Should().Be(command.lastName);
            result.Email.Should().Be(command.email);
        }

        [Fact]
        public async Task GetStudent_WithValidId_ShouldReturnOk()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            // First create a student
            var createCommand = new
            {
                firstName = "Test",
                lastName = "Student",
                email = $"test.{Guid.NewGuid()}@example.com",
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Male",
                address = "123 Test St"
            };

            var createResponse = await client.PostAsJsonAsync("/api/v1/students", createCommand);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdStudent = await createResponse.Content.ReadFromJsonAsync<StudentDto>();
            var createdStudentId = createdStudent!.Id;

            // Act
            var response = await client.GetAsync($"/api/v1/students/{createdStudentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<StudentDetailsDto>();
            result.Should().NotBeNull();
            result!.Id.Should().Be(createdStudentId);
            result.FirstName.Should().Be(createCommand.firstName);
            result.LastName.Should().Be(createCommand.lastName);
            result.Email.Should().Be(createCommand.email);
        }

        [Fact]
        public async Task GetStudent_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync($"/api/v1/students/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateStudent_WithDuplicateEmail_ShouldReturnConflict()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            var email = $"duplicate.{Guid.NewGuid()}@example.com";

            // First create a student
            var createCommand = new
            {
                firstName = "First",
                lastName = "Duplicate",
                email,
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Male"
            };
            await client.PostAsJsonAsync("/api/v1/students", createCommand);

            // Try to create another student with the same email
            var duplicateCommand = new
            {
                firstName = "Second",
                lastName = "Duplicate",
                email,
                phoneNumber = "+254712345679",
                dateOfBirth = "2000-01-02T00:00:00Z",
                gender = "Female"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/students", duplicateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task UpdateStudent_WithValidData_ShouldReturnOk()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            // Create a student first
            var createCommand = new
            {
                firstName = "Update",
                lastName = "Me",
                email = $"update.{Guid.NewGuid()}@example.com",
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Male"
            };
            var createResponse = await client.PostAsJsonAsync("/api/v1/students", createCommand);
            var student = await createResponse.Content.ReadFromJsonAsync<StudentDto>();

            // Update the student
            var updateCommand = new
            {
                id = student!.Id,
                firstName = "Updated",
                lastName = "Name",
                phoneNumber = "+254712345679",
                dateOfBirth = "2001-01-01T00:00:00Z",
                gender = "Female",
                address = "789 Updated St",
                isEnrolled = true
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1/students/{student.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<StudentDto>();
            result.Should().NotBeNull();
            result!.FirstName.Should().Be(updateCommand.firstName);
            result.LastName.Should().Be(updateCommand.lastName);
        }

        [Fact]
        public async Task DeleteStudent_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            // Create a student first
            var createCommand = new
            {
                firstName = "Delete",
                lastName = "Me",
                email = $"delete.{Guid.NewGuid()}@example.com",
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Male"
            };
            var createResponse = await client.PostAsJsonAsync("/api/v1/students", createCommand);
            var student = await createResponse.Content.ReadFromJsonAsync<StudentDto>();

            // Act
            var response = await client.DeleteAsync($"/api/v1/students/{student!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify student is soft-deleted
            var getResponse = await client.GetAsync($"/api/v1/students/{student.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetStudentEnrollments_ShouldReturnOk()
        {
            // Arrange
            using var client = _fixture.CreateAuthenticatedClient();

            // Create a student first
            var createCommand = new
            {
                firstName = "Enroll",
                lastName = "Test",
                email = $"enroll.{Guid.NewGuid()}@example.com",
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Male"
            };
            var createResponse = await client.PostAsJsonAsync("/api/v1/students", createCommand);
            var student = await createResponse.Content.ReadFromJsonAsync<StudentDto>();

            // Act
            var response = await client.GetAsync($"/api/v1/students/{student!.Id}/enrollments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<List<EnrollmentDto>>();
            result.Should().NotBeNull();
        }
    }
}

