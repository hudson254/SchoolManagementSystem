# Architecture Documentation

## Table of Contents
- [Architecture Overview](#architecture-overview)
- [Clean Architecture Layers](#clean-architecture-layers)
- [CQRS Pattern](#cqrs-pattern)
- [Repository Pattern](#repository-pattern)
- [Unit of Work](#unit-of-work)
- [Dependency Injection](#dependency-injection)
- [Multi-Tenancy Architecture](#multi-tenancy-architecture)
- [Request Pipeline](#request-pipeline)
- [Authentication Architecture](#authentication-architecture)
- [Module Architecture](#module-architecture)
- [Related Documentation](#related-documentation)

---

## Architecture Overview

The School Management System (SMS) is built using **Clean Architecture** principles, which separates the application into distinct layers with clear dependency rules. The core principle is that dependencies point inward - the Domain layer has no external dependencies, and higher layers depend on abstractions rather than concrete implementations.

```
┌────────────────────────────────────────────────────────────────┐
│                     Presentation Layer                          │
│                      SMS.API (REST API)                         │
│           Controllers, Middleware, Hubs, Health Checks         │
├────────────────────────────────────────────────────────────────┤
│                    Application Layer                            │
│                    SMS.Application                              │
│         Commands, Queries, Handlers, DTOs, Validators          │
├────────────────────────────────────────────────────────────────┤
│                     Domain Layer                                │
│                     SMS.Domain                                  │
│           Entities, Enums, Interfaces, Value Objects           │
├────────────────────────────────────────────────────────────────┤
│                 Infrastructure Layer                            │
│  SMS.Infrastructure  │  SMS.Persistence  │  SMS.Identity        │
│  SMS.Certificates    │  SMS.Reporting    │  SMS.Notifications   │
│  SMS.Multitenancy    │  SMS.Shared       │                      │
└────────────────────────────────────────────────────────────────┘
```

---

## Clean Architecture Layers

### 1. Domain Layer (SMS.Domain)

The innermost layer containing enterprise-wide business rules and entities.

**Responsibilities:**
- Define core entities (User, Student, Lecturer, Course, Unit, Enrollment, Grade, etc.)
- Define enums for system statuses and types
- Define repository interfaces
- Define domain services interfaces
- Contain no external dependencies

**Key Files:**
- `Entities/` - All domain entities
- `Enums/` - All system enums
- `Interfaces/` - Repository and service interfaces
- `Common/` - Base entity, value objects

### 2. Application Layer (SMS.Application)

Contains application-specific business rules and use case orchestration using CQRS.

**Responsibilities:**
- Implement CQRS commands and queries
- Define DTOs (Data Transfer Objects)
- Implement FluentValidation validators
- Handle business logic and use cases
- Define application-level interfaces
- Coordinate between domain and infrastructure

**Key Patterns:**
- Command/Query handlers (MediatR)
- Validation (FluentValidation)
- AutoMapper profiles
- Pipeline behaviors

### 3. Infrastructure Layer

Contains implementations of domain and application interfaces.

**Sub-projects:**

| Project | Responsibility |
|---------|---------------|
| **SMS.Infrastructure** | Services, file storage, token management, audit, error handling |
| **SMS.Persistence** | EF Core DbContext, repositories, migrations |
| **SMS.Identity** | JWT service, user management, authentication |
| **SMS.Certificates** | Certificate generation, verification, templates |
| **SMS.Reporting** | PDF generation, Excel export, report authentication |
| **SMS.Notifications** | SignalR hub, SMS service, notification management |
| **SMS.Multitenancy** | Tenant resolution, tenant context |
| **SMS.Shared** | Shared utilities and helpers |

### 4. Presentation Layer (SMS.API)

The outermost layer that handles HTTP requests and responses.

**Responsibilities:**
- Controllers for API endpoints
- Middleware pipeline (correlation, logging, exception handling, security, CSRF, tenant, rate limiting, metrics)
- Health checks
- SignalR hub endpoint
- Swagger documentation
- Static file serving for uploads
- Configuration binding

---

## CQRS Pattern

The system uses **Command Query Responsibility Segregation (CQRS)** with MediatR to separate read and write operations.

### Commands
Commands are used for operations that change state (CREATE, UPDATE, DELETE).

```
Command → CommandHandler → Repository/UnitOfWork → Database
```

**Examples:**
- `CreateStudentCommand`
- `UpdateGradeCommand`
- `AssignHouseCommand`
- `SubmitStudentEnrollmentCommand`
- `ApproveRegistrationCommand`

### Queries
Queries are used for read operations that return data without changing state.

```
Query → QueryHandler → Repository → Database → DTO
```

**Examples:**
- `GetStudentsQuery`
- `GetGradesQuery`
- `GetHousesQuery`
- `GetCourseOfferingsQuery`

### Result Pattern
Commands typically return `Result<T>` objects that indicate success or failure with optional error messages and validation errors.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public IEnumerable<string> Errors { get; }
}
```

---

## Repository Pattern

The system uses the **Repository Pattern** to abstract data access.

### Interface Definition
```csharp
public interface IStudentRepository
{
    Task<Student> GetByIdAsync(Guid id);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student> AddAsync(Student student);
    Task UpdateAsync(Student student);
    Task DeleteAsync(Guid id);
    // ... domain-specific queries
}
```

### Implementation
```csharp
public class StudentRepository : IStudentRepository
{
    private readonly ApplicationDbContext _context;
    
    public StudentRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // ... implementations using EF Core
}
```

### Registered Repositories
| Repository | Purpose |
|------------|---------|
| StudentRepository | Student data access |
| CourseRepository | Course data access |
| UnitRepository | Unit data access |
| EnrollmentRepository | Enrollment data access |
| GradeRepository | Grade data access |
| LecturerRepository | Lecturer data access |
| TimetableRepository | Timetable data access |
| AccommodationRepository | Accommodation data access |
| DepartmentRepository | Department data access |
| NotificationRepository | Notification data access |
| ReportVerificationRepository | Report verification data |
| AuditLogRepository | Audit log data |
| CourseOfferingRepository | Course offering data |
| AssessmentRepository | Assessment data |
| CertificateRepository | Certificate data |
| CertificateTemplateRepository | Certificate template data |

---

## Unit of Work

The **Unit of Work** pattern ensures multiple operations are committed atomically.

```
Handler → Resolves multiple Repositories from UnitOfWork → Performs Operations → SaveChanges() → Transaction
```

```csharp
public interface IUnitOfWork
{
    IStudentRepository Students { get; }
    ICourseRepository Courses { get; }
    // ... other repositories
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

---

## Dependency Injection

The system uses ASP.NET Core's built-in dependency injection container.

### Registration Files
- `SMS.API/Program.cs` - Main DI container setup
- `SMS.Application/DependencyInjection.cs` - Application layer services
- `SMS.Certificates/DependencyInjection.cs` - Certificate module services
- `SMS.Reporting/DependencyInjection.cs` - Reporting services
- `SMS.Notifications/DependencyInjection.cs` - Notification services

### Lifetime Rules
| Lifetime | When to Use | Example |
|----------|-------------|---------|
| **Singleton** | Services with no state or shared state | Token revocation, Error repository |
| **Scoped** | Services per HTTP request | DbContext, Repositories, MediatR handlers |
| **Transient** | Lightweight stateless services | Validators |

---

## Multi-Tenancy Architecture

The system supports multi-tenancy using **Row Level Security (RLS)** in PostgreSQL.

### Components
1. **TenantContext** - Holds current tenant information per request
2. **TenantResolver** - Resolves tenant from request
3. **TenantResolutionMiddleware** - Injects tenant context into pipeline
4. **Row Level Security** - PostgreSQL enforces tenant isolation at database level

### Flow
```
Request → TenantResolutionMiddleware → Resolve Tenant → Set TenantContext → Database queries filtered by RLS
```

### Tenant Isolation
- Every entity has a TenantId
- PostgreSQL RLS policies enforce `WHERE TenantId = CurrentTenant`
- Prevents cross-tenant data access

---

## Request Pipeline

### Middleware Order
```
1. CorrelationIdMiddleware      → Add correlation ID to requests/logs
2. LoggingEnrichmentMiddleware  → Enrich logs with request context
3. ExceptionHandlingMiddleware  → Handle exceptions, return consistent errors
4. SecurityHeadersMiddleware    → Add security headers (HSTS, XSS, etc.)
5. CsrfProtectionMiddleware     → Validate CSRF tokens for state-changing requests
6. TenantResolutionMiddleware   → Resolve and set tenant context
7. RateLimitingMiddleware       → Enforce rate limits
8. MetricsMiddleware            → Collect Prometheus metrics
9. UseAuthentication            → Authenticate JWT tokens
10. UseAuthorization            → Authorize based on roles
```

### Controller Flow
```
HTTP Request → Controller → MediatR Send (Command/Query) → Pipeline Behaviors → Handler → Repository → Database
```

### Pipeline Behaviors
- Validation behavior (FluentValidation)
- Logging behavior
- Transaction behavior (if applicable)

---

## Authentication Architecture

### Token Flow
```
Login → Authenticate Credentials → Generate JWT → Store in httpOnly Cookie → Return to Client
```

### Components
1. **JwtService** - Generates and validates JWT tokens
2. **UserManagerService** - Manages user identity operations
3. **Token Revocation** - In-memory or Redis-based deny-list
4. **Cookie Storage** - JWT stored in httpOnly cookie
5. **Authorization Policies** - Role-based access control

### JWT Contents
- Subject (user ID)
- Name (username)
- Role claims
- Issuer
- Audience
- Expiration
- Issued at

---

## Module Architecture

### Feature Folder Structure
```
SMS.Application/Features/
├── Auth/
│   ├── Commands/
│   ├── Queries/
│   └── DTOs/
├── Students/
│   ├── Commands/
│   ├── Queries/
│   └── DTOs/
├── Grades/
├── Enrollments/
├── Accommodation/
├── Certificates/
└── ...
```

Each feature folder contains:
- **Commands** - Write operations
- **Queries** - Read operations
- **Handlers** - Operation logic
- **DTOs** - Data transfer objects
- **Validators** - Input validation

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [System Overview](../01-System-Overview/README.md) | System overview and modules |
| [Developer Guide](../18-Developer-Guide/README.md) | Development setup and standards |
| [Database Guide](../13-Database/README.md) | Database schema and architecture |
| [API Documentation](../17-API/README.md) | API endpoints reference |
| [Security Guide](../12-Security/README.md) | Security architecture |
| [Authentication](../11-Authentication/README.md) | Authentication mechanisms |
