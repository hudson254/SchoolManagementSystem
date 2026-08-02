# Detailed Technical Repair Plan - School Management System

---

## Phase A: Restore Build Stability (Priority: Critical | Effort: 4-6 hours)

### A.1 Fix 50 Feature Files with Missing `using` Keyword
**Files:** All files under `src/SMS.Application/Features/**/*.cs`
**Issue:** Line 8 has ` Microsoft.Extensions.Logging;` instead of `using Microsoft.Extensions.Logging;`
**Fix:** Replace line 8 in all 50 files with `using Microsoft.Extensions.Logging;`
**Dependencies:** None
**Verification:** Solution compiles without CS8802 errors

### A.2 Fix DependencyInjection.cs
**File:** `src/SMS.Application/DependencyInjection.cs`
**Issues:**
1. Line 4: Remove stray `Microsoft.AspNetCore.Http;`
2. Line 32: Fix `ICurrentUserService` reference - needs proper namespace
**Fix:** 
- Remove line 4 completely
- Add `using SMS.Infrastructure.Interfaces;` and implement proper interface mapping, OR move `ICurrentUserService` to Domain layer
**Dependencies:** None
**Verification:** No CS0246 errors in Application project

### A.3 Create Missing Core Interfaces
**Files to create:**
- `src/SMS.Domain/Interfaces/IBaseEntity.cs` - Interface that User.cs implements
- `src/SMS.Domain/Interfaces/ISoftDelete.cs` - For `SoftDelete()` method used in repositories

**IBaseEntity.cs content:**
```csharp
namespace SMS.Domain.Interfaces
{
    public interface IBaseEntity
    {
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
        string? ModifiedBy { get; set; }
        DateTime? ModifiedDate { get; set; }
        string? DeletedBy { get; set; }
        DateTime? DeletedDate { get; set; }
        bool IsDeleted { get; set; }
        byte[]? RowVersion { get; set; }
    }
}
```

**ISoftDelete.cs content:**
```csharp
namespace SMS.Domain.Interfaces
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        string? DeletedBy { get; set; }
        DateTime? DeletedDate { get; set; }
        void SoftDelete(string deletedBy);
    }
}
```

**Dependencies:** None
**Verification:** Domain project compiles

### A.4 Create Missing Exception Classes
**Files to create in** `src/SMS.Application/Exceptions/`:
1. `NotFoundException.cs`
2. `UnauthorizedException.cs`  
3. `ConflictException.cs`
4. `ValidationException.cs`

**Dependencies:** A.3 (IBaseEntity)
**Verification:** Application project compiles

### A.5 Create Missing DTOs
**Files to create in** `src/SMS.Application/DTOs/`:
1. `AuthResponseDto.cs`
2. `CourseDetailsDto.cs`
3. `UnitDto.cs`
4. `ProgrammeSummaryDto.cs`
5. Fix existing `StudentDto.cs` to match entity properties

**Dependencies:** A.3
**Verification:** Application project compiles

### A.6 Create Missing MediatR Behaviors
**Files to create in** `src/SMS.Application/Behaviors/`:
1. `ValidationBehavior.cs`
2. `LoggingBehavior.cs`

**Dependencies:** A.4 (exceptions)
**Verification:** Application project compiles

### A.7 Create Missing Domain Entities
**Files to create in** `src/SMS.Domain/Entities/`:
1. `LoginHistory.cs`
2. `Notification.cs`
3. `AuditLog.cs`
4. `Department.cs`
5. `Programme.cs`

**Fix existing entities:**
- `User.cs`: Remove references to missing entities OR create them
- `Course.cs`: Remove navigation properties to missing entities OR create them
- `Student.cs`: Remove navigation properties to missing entities OR create them

**Dependencies:** A.3
**Verification:** Domain project compiles

### A.8 Fix ID Type Conflict (int vs Guid)
**Issue:** Entities use `int Id` from `BaseEntity`, but repositories use `Guid id`
**Fix:** Change `BaseEntity.Id` from `int` to `Guid`, update all foreign key properties
**Alternative:** Change repositories to use `int` instead of `Guid`
**Recommended:** Use `Guid` for distributed system compatibility

**Files affected:**
- `src/SMS.Domain/Common/BaseEntity.cs` - Change `int Id` to `Guid Id`
- `src/SMS.Domain/Entities/*.cs` - Update FK properties (StudentId, CourseId, etc.)
- All repository files - Already use Guid, no change needed
- All CQRS handler files - Already use Guid, no change needed

**Dependencies:** A.3
**Verification:** All projects compile

---

## Phase B: Repair Backend (Priority: Critical | Effort: 8-12 hours)

### B.1 Fix Entity Model Alignment
**Issue:** Repository references 15+ properties that don't exist on actual entities

**Fix Student entity:**
```csharp
public class Student : TenantAwareEntity
{
    // Add these missing properties:
    public string UserId { get; set; }
    public string StudentNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public Guid? ProgrammeId { get; set; }
    public Guid? CurrentSemesterId { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public bool IsEnrolled { get; set; }
    public string AcademicStatus { get; set; }
    public virtual User? User { get; set; }
    public virtual Programme? Programme { get; set; }
    public virtual Semester? CurrentSemester { get; set; }
    public virtual ICollection<Enrollment> Enrollments { get; set; }
    public virtual ICollection<Grade> Grades { get; set; }
    public virtual AccommodationAssignment? AccommodationAssignment { get; set; }
}
```

**Fix Course entity - Add:**
- `Department` navigation property
- `AdmissionRequirements`, `Objectives` fields
- `Programmes` navigation collection
- `CreatedDate` field

**Fix Unit entity - Add:**
- `ContactHours`, `Credits` fields
- `CreatedDate` field

**Dependencies:** Phase A complete
**Verification:** Domain project compiles

### B.2 Fix Namespace Imports in Program.cs
**File:** `src/SMS.API/Program.cs`
**Issues:**
- `using SMS.Infrastructure.Data;` - namespace doesn't exist
- `using SMS.Infrastructure.MultiTenancy;` - wrong namespace path
- Missing `using SMS.Infrastructure.Interfaces;`

**Fix:** Correct all import statements to match actual project namespaces

### B.3 Fix ICurrentUserService Registration
**Issue:** ApplicationDbContext requires ICurrentUserService, but it's not registered
**Fix:** 
1. Move `ICurrentUserService` to Domain layer
2. Register in Program.cs: `builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();`
3. Ensure `CurrentUserService` implementation exists in Infrastructure

### B.4 Create LocalFileStorageService
**File to create:** `src/SMS.Infrastructure/Services/LocalFileStorageService.cs`
**Implement** `IFileStorageService` interface with local file system storage

### B.5 Create Missing Middleware
**Files to create in** `src/SMS.API/Middleware/`:
1. `ExceptionHandlingMiddleware.cs` - Global exception handler
2. `TenantResolutionMiddleware.cs` - Extract tenant from request

**Dependencies:** B.2
**Verification:** API project compiles

---

## Phase C: Repair Database (Priority: Critical | Effort: 4-6 hours)

### C.1 Implement OnModelCreating
**File:** `src/SMS.Persistence/Data/ApplicationDbContext.cs`
**Task:** Add full entity configurations including:
- All primary keys (Guid)
- Foreign key relationships
- Required indexes
- Column mappings
- Table names (snake_case)
- Soft delete query filters
- Tenant isolation query filters
- Identity entity configurations (User, Role, UserRole)

### C.2 Implement SaveChangesAsync
**Task:** Add:
- Auto-set CreatedAt/UpdatedAt timestamps
- Auto-set CreatedBy/UpdatedBy from ICurrentUserService
- Tenant ID enforcement on save
- Soft delete handling
- Audit logging

### C.3 Add Missing DbSet Declarations
**Add to ApplicationDbContext:**
- `DbSet<Department> Departments`
- `DbSet<Programme> Programmes`
- `DbSet<AcademicYear> AcademicYears`
- `DbSet<Semester> Semesters`
- `DbSet<Grade> Grades`
- `DbSet<Attendance> Attendances`
- `DbSet<Assignment> Assignments`
- `DbSet<Timetable> Timetables`
- `DbSet<Lecturer> Lecturers`
- `DbSet<Room> Rooms`
- `DbSet<Accommodation> Accommodations`
- `DbSet<AccommodationAssignment> AccommodationAssignments`

### C.4 Create Proper Migrations
**Task:**
1. Delete existing migration `20240101000000_InitialCreate`
2. Generate fresh migration after entity fixes
3. Add seed data for roles, permissions, default admin user

### C.5 Implement Row-Level Security
**Task:**
1. Add tenant_id column to all tenant-scoped tables
2. Create PostgreSQL RLS policies
3. Ensure DbContext enforces tenant isolation

**Dependencies:** Phase B complete
**Verification:** Migration runs, seed data loads

---

## Phase D: Repair API (Priority: Critical | Effort: 6-8 hours)

### D.1 Create API Controllers
**Files to create in** `src/SMS.API/Controllers/`:
1. `AuthController.cs` - Login, Register, RefreshToken, ForgotPassword, ResetPassword, VerifyEmail, Logout
2. `StudentsController.cs` - CRUD + Search, Enroll, GetGrades, GetTranscript
3. `CoursesController.cs` - CRUD + GetWithDetails, GetUnits
4. `UnitsController.cs` - CRUD
5. `AccommodationController.cs` - AssignRoom, TransferRoom, VacateRoom, GetRooms, GetOccupancy
6. `AssignmentsController.cs` - CRUD + Submit, Grade
7. `DashboardController.cs` - Statistics, Trends, Activities, TopStudents, UpcomingEvents
8. `UsersController.cs` - User management (admin)

### D.2 Create appsettings.json
**File to create:** `src/SMS.API/appsettings.json`
**Content includes:**
- Connection strings (DefaultConnection, HangfireConnection)
- JWT configuration (Secret, Issuer, Audience, Expiry)
- Serilog configuration
- CORS origins
- File storage settings
- Email settings
- SMS settings

### D.3 Create appsettings.Development.json
**File to create:** `src/SMS.API/appsettings.Development.json`
**Content:** Development-specific overrides

### D.4 Add API Response Envelope
**Task:** Create consistent API response format:
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
}
```

### D.5 Configure API Versioning in Routes
**Task:** Apply `[ApiVersion("1.0")]` attribute to all controllers and configure route templates

**Dependencies:** Phase B, C complete
**Verification:** All endpoints respond correctly

---

## Phase E: Repair Frontend (Priority: High | Effort: 8-12 hours)

### E.1 Audit Frontend Project
**Task:** Run full audit of `frontend/sms-web/` including:
- Check npm package.json for missing/conflicting dependencies
- Verify TypeScript compilation
- Check all imports and component references

### E.2 Create API Client Library
**Task:** Generate or create TypeScript API client from Swagger spec

### E.3 Fix Component Structure
**Task:** Ensure all pages and components follow consistent patterns

### E.4 Implement Missing Features
**Task:** Create any missing pages, forms, or components

### E.5 Verify Frontend Build
**Task:** Ensure `npm run build` succeeds

**Dependencies:** Phase D complete
**Verification:** Frontend builds and connects to API

---

## Phase F: Security Hardening (Priority: Critical | Effort: 4-6 hours)

### F.1 Fix CORS Policy
**Change from:** `AllowAnyOrigin()`
**Change to:** Specific allowed origins from configuration

### F.2 Remove Hardcoded Password
**File:** `src/SMS.Application/Features/Students/Commands/CreateStudentCommand.cs`
**Fix:** Generate random password or require password in request

### F.3 Add Password Policy Validation
**Task:** Increase minimum password requirements and add validation:
- Minimum 8 characters
- Require uppercase, lowercase, number, special character
- Add password strength validation

### F.4 Fix AutoMapper Vulnerability
**Task:** Pin to secure AutoMapper version or switch to manual mapping

### F.5 Add Security Headers Middleware
**Task:** Add middleware for:
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Content-Security-Policy
- Strict-Transport-Security

### F.6 Add Rate Limiting
**Task:** Add rate limiting to authentication endpoints using ASP.NET Core rate limiting middleware

### F.7 Add CSRF Protection
**Task:** Configure anti-forgery tokens for state-changing operations

### F.8 JWT Secret Protection
**Task:** Ensure JWT secret is stored in secure configuration (not code), add validation

### F.9 Input Sanitization
**Task:** Add input sanitization to prevent XSS and SQL injection

**Dependencies:** Phase B, D complete
**Verification:** Security scan passes, OWASP findings resolved

---

## Phase G: Performance Optimization (Priority: Medium | Effort: 3-4 hours)

### G.1 Add DbContext Pooling
**Task:** Change `AddDbContext` to `AddDbContextPool` with appropriate pool size

### G.2 Implement Query Caching
**Task:** Add response caching for read-only endpoints (courses, units, etc.)

### G.3 Add Pagination Defaults
**Task:** Set default page size and max page size for all list endpoints

### G.4 Add Response Compression
**Task:** Configure `AddResponseCompression()` middleware

### G.5 Fix Async Patterns
**Task:** Ensure all async methods use proper async/await patterns (no sync-over-async)

**Dependencies:** Phase B, C, D complete
**Verification:** Performance benchmarks show improvement

---

## Phase H: Testing (Priority: High | Effort: 8-12 hours)

### H.1 Fix Unit Tests
**Task:** Update all unit test files to match corrected code:
- `tests/SMS.UnitTests/Auth/LoginCommandTests.cs`
- `tests/SMS.UnitTests/Auth/RegisterCommandTests.cs`
- `tests/SMS.UnitTests/Students/CreateStudentCommandTests.cs`
- `tests/SMS.UnitTests/Students/UpdateStudentCommandTests.cs`
- `tests/SMS.UnitTests/Courses/CreateCourseCommandTests.cs`
- `tests/SMS.UnitTests/Units/CreateUnitCommandTests.cs`
- `tests/SMS.UnitTests/Assignments/CreateAssignmentCommandTests.cs`
- `tests/SMS.UnitTests/Accommodation/AssignRoomCommandTests.cs`

### H.2 Fix Integration Tests
**Task:** Update `tests/SMS.IntegrationTests/` with working database tests

### H.3 Fix API Tests
**Task:** Update `tests/SMS.ApiTests/` with working endpoint tests

### H.4 Add Missing Test Coverage
**Task:** Add tests for:
- All authentication scenarios
- All CRUD operations
- Error handling and edge cases
- Multi-tenancy isolation
- Authorization policies

**Dependencies:** Phase A-G complete
**Verification:** All test suites pass with >70% coverage

---

## Phase I: Deployment (Priority: High | Effort: 4-6 hours)

### I.1 Fix Dockerfiles
**Task:** Update Dockerfiles to work with current project structure:
- `docker/Dockerfile.api`
- `docker/Dockerfile.frontend`
- `docker/Dockerfile.nginx`

### I.2 Fix Docker Compose Files
**Task:** Update:
- `docker/docker-compose.yml`
- `docker/docker-compose.dev.yml`
- `docker/docker-compose.prod.yml`

### I.3 Create .env.example
**Task:** Document all required environment variables

### I.4 Configure SSL in Nginx
**Task:** Update `docker/nginx.conf` with SSL configuration and certbot auto-renewal

### I.5 Verify Deployment Scripts
**Task:** Update and verify:
- `scripts/deploy.sh`
- `scripts/backup.sh`
- `scripts/restore.sh`
- `scripts/seed.sh`
- `scripts/health-check.sh`

**Dependencies:** Phase A-H complete
**Verification:** Docker compose up succeeds in dev and prod

---

## Phase J: Documentation (Priority: Medium | Effort: 4-6 hours)

### J.1 Create Installation Guide
**File:** `docs/INSTALLATION.md`
**Content:** Step-by-step setup instructions for development environment

### J.2 Create Administrator Guide
**File:** `docs/ADMIN_GUIDE.md`
**Content:** System administration, user management, configuration

### J.3 Create Deployment Guide
**File:** `docs/DEPLOYMENT.md`
**Content:** Production deployment instructions with Docker

### J.4 Update API Documentation
**Task:** Ensure Swagger annotations are complete on all endpoints

### J.5 Create Database Schema Documentation
**File:** `docs/DATABASE_SCHEMA.md`
**Content:** Entity relationship diagram, table descriptions, migration guide

### J.6 Create Troubleshooting Guide
**File:** `docs/TROUBLESHOOTING.md`
**Content:** Common issues and solutions

**Dependencies:** Phase A-I complete
**Verification:** Documentation is accurate and complete
