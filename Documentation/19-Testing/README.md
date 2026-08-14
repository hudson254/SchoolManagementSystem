# Testing Guide

## Table of Contents
- [Testing Overview](#testing-overview)
- [Test Types](#test-types)
- [Test Projects](#test-projects)
- [Running Tests](#running-tests)
- [Writing Unit Tests](#writing-unit-tests)
- [Writing API Tests](#writing-api-tests)
- [Writing Integration Tests](#writing-integration-tests)
- [Test Coverage](#test-coverage)
- [Testing Best Practices](#testing-best-practices)
- [Related Documentation](#related-documentation)

---

## Testing Overview

The School Management System uses xUnit as the primary test framework with a focus on automation and comprehensive coverage.

### Test Framework
- **xUnit**: Primary test framework
- **FluentAssertions**: Assertion library
- **Moq**: Mocking framework
- **Testcontainers**: Database testing with Docker containers
- **TestServer**: API integration testing

---

## Test Types

| Test Type | Description | Project |
|-----------|-------------|---------|
| Unit Tests | Test individual components in isolation | SMS.UnitTests |
| API Tests | Test API endpoints and middleware | SMS.ApiTests |
| Integration Tests | Test database interactions | SMS.IntegrationTests |

---

## Test Projects

### SMS.UnitTests
Tests for:
- Command/Query handlers
- Validators
- Domain services
- Infrastructure services

**Key test files:**
- `Auth/LoginCommandTests.cs`
- `Auth/RegisterCommandTests.cs`
- `Auth/SecurityRegressionTests.cs`
- `Students/CreateStudentCommandTests.cs`
- `CourseOfferings/CourseOfferingCommandTests.cs`
- `Accommodation/AssignHouseCommandTests.cs`
- `Certificates/CertificateEligibilityServiceTests.cs`
- `Names/NameParserTests.cs`
- `Names/UsernameGeneratorTests.cs`
- `Assessments/AssessmentEngineTests.cs`

### SMS.ApiTests
Tests for:
- API controllers
- Middleware
- Authorization
- Full request/response pipelines

**Key test files:**
- `Controllers/AuthControllerTests.cs`
- `Controllers/StudentAuthorizationTests.cs`
- `Controllers/CourseOfferingControllerTests.cs`
- `Middleware/ExceptionHandlingMiddlewareTests.cs`
- `Middleware/SecurityHeadersMiddlewareTests.cs`
- `Logging/ErrorLoggingServiceTests.cs`
- `Integration/FullFlowTests.cs`

### SMS.IntegrationTests
Tests for:
- Repository implementations
- Database operations
- Data integrity
- Tenant isolation

**Key test files:**
- `Database/StudentRepositoryTests.cs`
- `Database/CourseOfferingRepositoryTests.cs`
- `Database/UnitAllocationRepositoryTests.cs`
- `Database/LoginHistoryRepositoryTests.cs`
- `Database/TenantIsolationTests.cs`
- `PasswordReset/PasswordResetControllerTests.cs`

---

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Project
```bash
dotnet test tests/SMS.UnitTests
dotnet test tests/SMS.ApiTests
dotnet test tests/SMS.IntegrationTests
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~LoginCommandTests"
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run with Verbose Output
```bash
dotnet test -v n
```

### Run Integration Tests
Integration tests require Docker (for Testcontainers):
```bash
# Ensure Docker is running
dotnet test tests/SMS.IntegrationTests
```

---

## Writing Unit Tests

### Test Structure
```csharp
public class CreateStudentCommandTests
{
    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var command = new CreateStudentCommand { ... };
        var handler = new CreateStudentCommandHandler(mockRepo.Object);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

### Mocking
```csharp
var mockRepo = new Mock<IStudentRepository>();
mockRepo.Setup(r => r.AddAsync(It.IsAny<Student>()))
        .ReturnsAsync(new Student { Id = Guid.NewGuid() });
```

### Test Naming Convention
`[MethodName]_[Scenario]_[ExpectedResult]`

Examples:
- `Handle_ValidRequest_ReturnsSuccessResult`
- `Handle_InvalidEmail_ReturnsValidationError`
- `Handle_NullInput_ThrowsArgumentNullException`

---

## Writing API Tests

### Test Setup
```csharp
public class AuthControllerTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;
    
    public AuthControllerTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }
}
```

### Testing Endpoints
```csharp
[Fact]
public async Task Login_ValidCredentials_ReturnsOk()
{
    // Arrange
    var loginRequest = new LoginRequest { ... };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Test Fixture
```csharp
public class ApiTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
```

---

## Writing Integration Tests

### Using Testcontainers
```csharp
public class StudentRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Configure DbContext with container connection string
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

---

## Test Coverage

### Current Coverage Areas
| Area | Unit Tests | API Tests | Integration Tests |
|------|-----------|-----------|-------------------|
| Authentication | ✅ | ✅ | - |
| Student Management | ✅ | ✅ | ✅ |
| Course Management | ✅ | ✅ | - |
| Enrollments | - | - | - |
| Grades | - | - | - |
| Course Offerings | ✅ | ✅ | ✅ |
| Accommodation | ✅ | - | - |
| Certificates | ✅ | - | - |
| Names/Usernames | ✅ | - | - |
| Password Reset | ✅ | ✅ | - |
| Security | ✅ | ✅ | - |
| Assessments | ✅ | - | - |
| Tenant Isolation | - | - | ✅ |

---

## Testing Best Practices

1. **Arrange-Act-Assert**: Structure tests clearly
2. **Test one thing**: Each test should verify one behavior
3. **Use descriptive names**: Names should describe scenario and expectation
4. **Avoid test interdependence**: Tests should run independently
5. **Mock external dependencies**: Keep unit tests fast and isolated
6. **Test edge cases**: Empty inputs, null values, boundary conditions
7. **Test error scenarios**: Verify proper error handling
8. **Keep tests fast**: Unit tests < 100ms, integration < 1s
9. **Run tests frequently**: Integrate with CI/CD pipeline
10. **Maintain test code**: Treat test code with same standards as production code

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Developer Guide](../18-Developer-Guide/README.md) | Development setup and standards |
| [API Documentation](../17-API/README.md) | Endpoints to test |
| [Architecture](../02-Architecture/README.md) | System architecture |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | Test failures |
