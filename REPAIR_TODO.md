# SMS Repair Progress Tracker

## PHASE 1: CRITICAL BUILD FIXES (P0 - Immediate)

### 1.1 Handler int → Guid Conversions (~80 errors)
- [x] CreateStudentCommand.cs - Fix int ProgrammeId, CurrentSemesterId → Guid (FIXED)
- [x] UpdateStudentCommand.cs (FIXED)
- [x] CreateCourseCommand.cs (FIXED)
- [x] UpdateCourseCommand.cs (FIXED)
- [x] GetCoursesQuery.cs (FIXED)
- [x] GetCourseQuery.cs (FIXED)
- [x] CreateAssignmentCommand.cs (FIXED)
- [x] UpdateAssignmentCommand.cs (FIXED)
- [x] GetAssignmentQuery.cs (FIXED)
- [x] GetAssignmentsQuery.cs (FIXED)
- [x] GradeAssignmentCommand.cs (FIXED - IAuditService call)
- [x] SubmitAssignmentCommand.cs (FIXED)
- [x] GetStudentQuery.cs (FIXED)
- [x] GetStudentsQuery.cs (FIXED)
- [x] GetStudentByIdQuery.cs (FIXED)
- [x] GetStudentGradesQuery.cs (FIXED - Removed DomainConstants, fixed navigation)
- [x] GetStudentTranscriptQuery.cs (FIXED - Removed DomainConstants, fixed navigation, added GetGradePoints)
- [x] GetStudentEnrollmentsQuery.cs (FIXED - Null-safe navigation paths)
- [x] EnrollStudentCommand.cs (FIXED - GetEnrollmentAsync call, LogActivityAsync→LogAsync)
- [x] DropStudentCommand.cs (FIXED)
- [x] DeleteStudentCommand.cs (FIXED)
- [x] CreateStudentCommand.cs (FIXED - Already complete)

### 1.2 Missing Entity Properties (~60 errors)
- [x] Assignment.cs - Already has Status, ClosingDate, PublishedDate, etc. ✓
- [x] Course.cs - Already has TotalCredits, AdmissionRequirements, Objectives ✓
- [x] Student.cs - Already has Gender, CumulativeGPA, TotalCreditsEarned, etc. ✓
- [x] User.cs - Already has Organization, IsEmailVerified, etc. ✓
- [x] Enrollment.cs - Already has UnitId, Unit nav, DropDate ✓
- [x] Grade.cs - Already has EnrollmentId, Enrollment nav, GradedDate, IsPublished, PublishedDate ✓
- [x] Room.cs - Already has IsAvailable, IsOccupied ✓

### 1.3 LogAsync Signature Mismatch (~15 errors)
- [x] Fix IAuditService.LogAsync calls in all handlers (DONE - Changed to LogAsync)

### 1.4 Repository Method Signature Mismatch (~15 errors)
- [x] Fix handler calls to repository methods with wrong params (DONE)

### 1.5 DTO Property Mismatches (~80 errors)
- [x] CourseDto - Add TotalCredits, DepartmentCode, CreatedDate (DONE)
- [x] AssignmentDto - Add LecturerId, SemesterId, Weight, PublishedDate, etc. (DONE)
- [x] AssignmentSubmissionDto - Add missing properties (DONE)
- [x] StudentDto - Add FullName (DONE)
- [x] UserProfileDto - Already has all needed properties ✓
- [x] CourseDetailsDto - Add missing properties (DONE)
- [x] PagedResult<T> - Fix Page, TotalPages properties (DONE)
- [x] GradeDto, EnrollmentDto, UnitDto, UnitDetailsDto, LecturerDto (DONE)

### 1.6 Other Fixes (~18 errors)
- [x] Fix ValidationException ambiguity (FluentValidation vs custom) - Use fully qualified names (DONE)
- [x] Fix Unit ambiguity (MediatR.Unit vs SMS.Domain.Entities.Unit) - Use fully qualified names (DONE)
- [x] Fix StudentEnrollment vs Enrollment type mismatch (DONE)
- [x] Fix VerifyEmailCommand - add token param (DONE)
- [x] Fix ResetPasswordCommand - add newPassword param (DONE)
- [x] Fix RefreshTokenCommand - passing string instead of bool (DONE)
- [x] Fix GradeAssignmentCommand.Score int→decimal mismatch (DONE)

### 1.7 Missing Domain Entities (NEW - Critical)
- [ ] Create AssignmentSubmission entity
- [ ] Create AccommodationAssignment entity
- [ ] Create Block entity
- [ ] Create Semester entity
- [ ] Create AcademicYear entity
- [ ] Create AuditLog entity
- [ ] Create Notification entity
- [ ] Create RolePermission entity
- [ ] Create LoginHistory entity
- [ ] Create DomainConstants class

### 1.8 Missing Service Interfaces (NEW - Critical)
- [ ] Create IAuditService interface
- [ ] Create IUnitOfWork interface
- [ ] Create ValidationBehavior

## PHASE 2: RUNTIME & DI FIXES (P0 - Immediate)

### 2.1 Middleware Registration
- [ ] Register ExceptionHandlingMiddleware in Program.cs
- [ ] Register SecurityHeadersMiddleware in Program.cs
- [ ] Register RateLimitingMiddleware in Program.cs
- [ ] Register TenantResolutionMiddleware in Program.cs

### 2.2 Duplicate Services
- [ ] Remove duplicate IUserManagerService from SMS.Infrastructure
- [ ] Fix DI container registrations

### 2.3 DI Container Fixes
- [ ] Add missing service registrations (IUnitOfWork, IAuditService, ICurrentUserService)
- [ ] Fix UnitOfWork LoggerFactory to use DI
- [ ] Register ICalendarEventRepository

## PHASE 3: DATABASE FIXES (P1)

### 3.1 Migrations
- [ ] Create initial EF Core migration
- [ ] Create seed data script

### 3.2 Tenant Isolation
- [ ] Fix tenant query filter to throw on null TenantContext
- [ ] Add proper tenant resolution

### 3.3 Schema Fixes
- [ ] Add indexes on foreign key columns
- [ ] Consolidate CreatedDate/CreatedAt redundancy

## PHASE 4: SECURITY (P1)

### 4.1 Secrets Management
- [ ] Move secrets to environment variables
- [ ] Fix appsettings.json placeholder values

### 4.2 Security Middleware
- [ ] Enable security headers
- [ ] Configure rate limiting
- [ ] Enable conditional HTTPS requirement

### 4.3 CSRF Protection
- [ ] Add anti-CSRF tokens
- [ ] Fix tenant data isolation

## PHASE 5: API COMPLETENESS (P2)

### 5.1 API Fixes
- [ ] Add API versioning
- [ ] Ensure consistent response format
- [ ] Add FluentValidation validators for commands

## PHASE 6: FRONTEND (P2)

### 6.1 Dependency Fixes
- [ ] Fix React version / @types/react mismatch
- [ ] Fix import paths in App.tsx

## PHASE 7: PERFORMANCE (P3)

### 7.1 Optimizations
- [ ] Add pagination to list endpoints
- [ ] Use AddDbContextPool
- [ ] Remove async wrappers over sync methods

## PHASE 8: CODE QUALITY (P3)

### 8.1 Cleanup
- [ ] Remove unused usings
- [ ] Remove empty projects or implement
- [ ] Add null checks
- [ ] Fix misspelled Accomodation → Accommodation
- [ ] Remove duplicate Features/Features/ folder

## PHASE 9: TESTING (P3)

### 9.1 Tests
- [ ] Fix test references
- [ ] Add unit tests for repositories
- [ ] Add handler tests

