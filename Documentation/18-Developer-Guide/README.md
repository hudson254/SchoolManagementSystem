# Developer Guide

## Table of Contents
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Coding Standards](#coding-standards)
- [Naming Conventions](#naming-conventions)
- [Database Conventions](#database-conventions)
- [Testing](#testing)
- [Dependency Injection](#dependency-injection)
- [CQRS Pattern](#cqrs-pattern)
- [Repository Pattern](#repository-pattern)
- [Unit of Work](#unit-of-work)
- [Logging](#logging)
- [Security](#security)
- [Contribution Guide](#contribution-guide)
- [Related Documentation](#related-documentation)

---

## Development Setup

### Prerequisites
- .NET SDK 9.0
- Node.js 20+
- PostgreSQL 16 (or Docker Desktop)
- Visual Studio 2022 / VS Code / Rider
- Git

### Clone and Setup
```bash
git clone https://github.com/your-org/school-management-system.git
cd school-management-system
```

### Backend Setup
```bash
# Restore dependencies
dotnet restore

# Navigate to API project
cd src/SMS.API

# Apply migrations
dotnet run -- migrate-database

# Seed data
dotnet run -- seed-data

# Start development server
dotnet run
```

### Frontend Setup
```bash
cd frontend/sms-web

# Install dependencies
npm install

# Set environment variables
echo "VITE_API_URL=http://localhost:5000" > .env

# Start development server
npm run dev
```

---

## Project Structure

```
SchoolManagementSystem/
├── src/
│   ├── SMS.API/             # REST API, Controllers, Middleware
│   ├── SMS.Application/     # CQRS Handlers, Commands, Queries
│   ├── SMS.Domain/          # Entities, Enums, Interfaces
│   ├── SMS.Infrastructure/  # Services, File Storage, Token Management
│   ├── SMS.Persistence/     # EF Core DbContext, Migrations, Repositories
│   ├── SMS.Identity/        # JWT Service, User Management
│   ├── SMS.Certificates/    # Certificate Generation, Verification
│   ├── SMS.Reporting/       # PDF/Excel Generation, Report Auth
│   ├── SMS.Notifications/   # SignalR Hub, SMS Service
│   ├── SMS.Multitenancy/    # Tenant Resolution, Context
│   └── SMS.Shared/          # Shared Utilities
├── frontend/
│   └── sms-web/             # React Frontend
├── docker/                   # Docker Configuration
├── scripts/                  # Build and Deployment Scripts
├── tests/
│   ├── SMS.ApiTests/        # API Integration Tests
│   ├── SMS.UnitTests/       # Unit Tests
│   └── SMS.IntegrationTests/ # Database Integration Tests
└── Documentation/           # System Documentation
```

---

## Coding Standards

### C# Standards
- Use file-scoped namespaces
- Use explicit types where clarity is needed, `var` otherwise
- Follow Microsoft .NET coding conventions
- Use expression-bodied members where appropriate
- Prefer async/await over synchronous methods
- Use nullable reference types

### TypeScript/React Standards
- Use functional components with hooks
- Use TypeScript strict mode
- Follow ESLint and Prettier configurations
- Use Material UI components where possible
- Use TanStack Query for server state

### General Standards
- Write meaningful comments for complex logic
- Use XML documentation comments for public APIs
- Keep methods focused and small (max 30 lines)
- Use dependency injection
- Follow SOLID principles

---

## Naming Conventions

### C# Naming
| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `StudentService` |
| Methods | PascalCase | `GetStudentAsync()` |
| Properties | PascalCase | `FirstName` |
| Parameters | camelCase | `studentId` |
| Variables | camelCase | `totalCount` |
| Interfaces | IPascalCase | `IStudentRepository` |
| Enums | PascalCase | `RegistrationStatus` |
| Constants | PascalCase | `MaxRetryCount` |

### TypeScript Naming
| Element | Convention | Example |
|---------|------------|---------|
| Components | PascalCase | `StudentList.tsx` |
| Functions | camelCase | `getStudents()` |
| Interfaces | PascalCase | `StudentDto` |
| Types | PascalCase | `UserRole` |
| Variables | camelCase | `studentCount` |
| Files | camelCase | `student.service.ts` |

---

## Database Conventions

### Table Naming
- Use PascalCase matching entity names
- Singular names: `Student`, `Course`, `Enrollment`
- Join tables: `CourseUnit`, `UserRole`

### Column Naming
- Use PascalCase
- Primary key: `Id`
- Foreign key: `RelatedEntityId` (e.g., `StudentId`)
- Created timestamp: `CreatedAt`
- Updated timestamp: `UpdatedAt`

### Migration Naming
- Descriptive names: `AddStudentEmailIndex`
- Prefix with timestamp by EF Core

---

## Testing

### Test Projects
| Project | Type | Framework |
|---------|------|-----------|
| SMS.UnitTests | Unit tests | xUnit |
| SMS.ApiTests | API integration tests | xUnit + TestServer |
| SMS.IntegrationTests | Database integration tests | xUnit + Testcontainers |

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SMS.UnitTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Dependency Injection

### Registration Pattern
Services are registered in the composition root (`Program.cs`) or in dedicated extension methods:

```csharp
// Program.cs
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### Module Registration
```csharp
// SMS.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
```

---

## CQRS Pattern

### Creating a Command
```csharp
public record CreateStudentCommand : IRequest<Result<Guid>>
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Email { get; init; }
}
```

### Creating a Handler
```csharp
public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Result<Guid>>
{
    private readonly IStudentRepository _repository;
    
    public async Task<Result<Guid>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        // Business logic
        var student = new Student { ... };
        await _repository.AddAsync(student);
        return Result<Guid>.Success(student.Id);
    }
}
```

---

## Repository Pattern

### Interface
```csharp
public interface IStudentRepository
{
    Task<Student> GetByIdAsync(Guid id);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student> AddAsync(Student student);
    Task UpdateAsync(Student student);
    Task DeleteAsync(Guid id);
}
```

---

## Logging

### Structured Logging
The system uses Serilog for structured JSON logging:
```csharp
Log.Information("Creating student {StudentName} with ID {StudentId}", name, studentId);
```

### Log Levels
- **Verbose**: Debugging details
- **Debug**: Development information
- **Information**: Normal operations
- **Warning**: Unexpected but handled issues
- **Error**: Failed operations
- **Fatal**: Application crashes

---

## Security

### Secure Coding Practices
1. Validate all input using FluentValidation
2. Use parameterized queries (EF Core)
3. Do not log sensitive data (passwords, tokens)
4. Implement proper error handling
5. Use dependency injection
6. Follow CORS best practices
7. Keep dependencies updated

---

## Contribution Guide

### Branch Strategy
- `main` - Production-ready code
- `develop` - Integration branch
- `feature/*` - New features
- `fix/*` - Bug fixes
- `release/*` - Release preparation

### Pull Request Process
1. Create a feature branch from `develop`
2. Implement changes with tests
3. Ensure all tests pass
4. Create a pull request to `develop`
5. Request code review
6. Address review feedback
7. Merge after approval

### Commit Messages
Follow conventional commits:
- `feat: Add student enrollment`
- `fix: Fix grade calculation`
- `docs: Update API documentation`
- `refactor: Extract validation logic`
- `test: Add unit tests for authentication`

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Architecture](../02-Architecture/README.md) | System architecture |
| [Testing Guide](../19-Testing/README.md) | Testing guidelines |
| [API Documentation](../17-API/README.md) | API endpoints |
| [Database Guide](../13-Database/README.md) | Database conventions |
| [Security Guide](../12-Security/README.md) | Security practices |
