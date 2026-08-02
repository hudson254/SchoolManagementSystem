# Prioritized Task List - School Management System Repair

**Total Tasks:** 78  
**P0 (Critical):** 38  
**P1 (High):** 22  
**P2 (Medium):** 12  
**P3 (Low):** 6  

---

## P0 - Critical Tasks (Must Fix - Blocking Compilation/Startup/Security)

| ID | Task | Phase | File(s) | Effort | Dependencies |
|----|------|-------|---------|--------|-------------|
| T001 | Fix missing `using` keyword in 50 feature files | A | `Features/**/*.cs` (50 files) | 15 min | None |
| T002 | Remove stray line in DependencyInjection.cs | A | `DependencyInjection.cs:4` | 1 min | None |
| T003 | Fix ICurrentUserService namespace/registration | A | `DependencyInjection.cs:32` | 15 min | None |
| T004 | Create IBaseEntity interface | A | `Domain/Interfaces/IBaseEntity.cs` | 10 min | None |
| T005 | Create ISoftDelete interface | A | `Domain/Interfaces/ISoftDelete.cs` | 10 min | None |
| T006 | Create NotFoundException | A | `Application/Exceptions/NotFoundException.cs` | 10 min | None |
| T007 | Create UnauthorizedException | A | `Application/Exceptions/UnauthorizedException.cs` | 10 min | None |
| T008 | Create ConflictException | A | `Application/Exceptions/ConflictException.cs` | 10 min | None |
| T009 | Create ValidationException | A | `Application/Exceptions/ValidationException.cs` | 10 min | None |
| T010 | Create AuthResponseDto | A | `Application/DTOs/AuthResponseDto.cs` | 15 min | None |
| T011 | Create CourseDetailsDto | A | `Application/DTOs/CourseDetailsDto.cs` | 15 min | None |
| T012 | Create UnitDto | A | `Application/DTOs/UnitDto.cs` | 10 min | None |
| T013 | Create ProgrammeSummaryDto | A | `Application/DTOs/ProgrammeSummaryDto.cs` | 10 min | None |
| T014 | Fix StudentDto to match entity | A | `Application/DTOs/StudentDto.cs` | 15 min | None |
| T015 | Create ValidationBehavior | A | `Application/Behaviors/ValidationBehavior.cs` | 30 min | T006-T009 |
| T016 | Create LoggingBehavior | A | `Application/Behaviors/LoggingBehavior.cs` | 20 min | None |
| T017 | Create LoginHistory entity | A | `Domain/Entities/LoginHistory.cs` | 15 min | None |
| T018 | Create Notification entity | A | `Domain/Entities/Notification.cs` | 15 min | None |
| T019 | Create AuditLog entity | A | `Domain/Entities/AuditLog.cs` | 15 min | None |
| T020 | Create Department entity | A | `Domain/Entities/Department.cs` | 15 min | None |
| T021 | Create Programme entity | A | `Domain/Entities/Programme.cs` | 15 min | None |
| T022 | Fix BaseEntity.Id from int to Guid | A | `Domain/Common/BaseEntity.cs` | 30 min | None |
| T023 | Update all entity FK properties to Guid | A | `Domain/Entities/*.cs` | 1 hr | T022 |
| T024 | Fix Student entity - add missing properties | B | `Domain/Entities/Student.cs` | 30 min | T022 |
| T025 | Fix Course entity - add missing properties | B | `Domain/Entities/Course.cs` | 20 min | T022 |
| T026 | Fix Unit entity - add missing properties | B | `Domain/Entities/Unit.cs` | 15 min | T022 |
| T027 | Fix Program.cs namespace imports | B | `API/Program.cs` | 15 min | None |
| T028 | Register ICurrentUserService in DI | B | `API/Program.cs` | 10 min | T003 |
| T029 | Create LocalFileStorageService | B | `Infrastructure/Services/LocalFileStorageService.cs` | 30 min | None |
| T030 | Create ExceptionHandlingMiddleware | B | `API/Middleware/ExceptionHandlingMiddleware.cs` | 45 min | T006-T009 |
| T031 | Create TenantResolutionMiddleware | B | `API/Middleware/TenantResolutionMiddleware.cs` | 30 min | None |
| T032 | Implement OnModelCreating with full config | C | `Persistence/Data/ApplicationDbContext.cs` | 2 hr | T024-T026 |
| T033 | Implement SaveChangesAsync with auditing | C | `Persistence/Data/ApplicationDbContext.cs` | 1 hr | T032 |
| T034 | Add missing DbSet declarations | C | `Persistence/Data/ApplicationDbContext.cs` | 30 min | T032 |
| T035 | Create fresh database migration | C | `Persistence/Migrations/` | 30 min | T032-T034 |
| T036 | Fix CORS policy (remove AllowAnyOrigin) | F | `API/Program.cs` | 15 min | None |
| T037 | Remove hardcoded password | F | `Application/Features/Students/Commands/CreateStudentCommand.cs` | 15 min | None |
| T038 | Fix AutoMapper vulnerability | F | `Application/SMS.Application.csproj` | 15 min | None |

---

## P1 - High Priority Tasks (Core Functionality)

| ID | Task | Phase | File(s) | Effort | Dependencies |
|----|------|-------|---------|--------|-------------|
| T039 | Create AuthController | D | `API/Controllers/AuthController.cs` | 1 hr | T010, T030, T031 |
| T040 | Create StudentsController | D | `API/Controllers/StudentsController.cs` | 1.5 hr | T014, T030, T031 |
| T041 | Create CoursesController | D | `API/Controllers/CoursesController.cs` | 1 hr | T011, T030, T031 |
| T042 | Create UnitsController | D | `API/Controllers/UnitsController.cs` | 45 min | T012, T030, T031 |
| T043 | Create AccommodationController | D | `API/Controllers/AccommodationController.cs` | 1 hr | T030, T031 |
| T044 | Create AssignmentsController | D | `API/Controllers/AssignmentsController.cs` | 1 hr | T030, T031 |
| T045 | Create DashboardController | D | `API/Controllers/DashboardController.cs` | 45 min | T030, T031 |
| T046 | Create appsettings.json | D | `API/appsettings.json` | 30 min | None |
| T047 | Create appsettings.Development.json | D | `API/appsettings.Development.json` | 15 min | T046 |
| T048 | Add API response envelope | D | `API/Models/ApiResponse.cs` | 30 min | None |
| T049 | Configure API versioning in routes | D | `API/Controllers/*.cs` | 30 min | T039-T045 |
| T050 | Audit frontend project | E | `frontend/sms-web/` | 1 hr | None |
| T051 | Create API client library | E | `frontend/sms-web/src/api/` | 2 hr | T039-T045 |
| T052 | Fix frontend component structure | E | `frontend/sms-web/src/` | 2 hr | T050 |
| T053 | Add password policy validation | F | `Application/Features/Auth/Commands/LoginCommand.cs` | 30 min | None |
| T054 | Add security headers middleware | F | `API/Middleware/SecurityHeadersMiddleware.cs` | 30 min | None |
| T055 | Add rate limiting | F | `API/Program.cs` | 30 min | None |
| T056 | Fix unit tests | H | `tests/SMS.UnitTests/` | 2 hr | T001-T038 |
| T057 | Fix integration tests | H | `tests/SMS.IntegrationTests/` | 2 hr | T032-T035 |
| T058 | Fix API tests | H | `tests/SMS.ApiTests/` | 2 hr | T039-T045 |
| T059 | Fix Dockerfiles | I | `docker/Dockerfile.*` | 1 hr | T001-T058 |
| T060 | Fix Docker Compose files | I | `docker/docker-compose*.yml` | 1 hr | T059 |

---

## P2 - Medium Priority Tasks (Quality/Performance/Docs)

| ID | Task | Phase | File(s) | Effort | Dependencies |
|----|------|-------|---------|--------|-------------|
| T061 | Add DbContext pooling | G | `API/Program.cs` | 15 min | T032 |
| T062 | Implement query caching | G | `API/Program.cs` | 30 min | T039-T045 |
| T063 | Add pagination defaults | G | `Application/Features/*/Queries/` | 30 min | None |
| T064 | Add response compression | G | `API/Program.cs` | 15 min | None |
| T065 | Fix async patterns | G | Various | 1 hr | None |
| T066 | Add CSRF protection | F | `API/Program.cs` | 30 min | None |
| T067 | JWT secret protection | F | `API/appsettings.json` | 15 min | T046 |
| T068 | Add input sanitization | F | `API/Middleware/InputSanitizationMiddleware.cs` | 30 min | None |
| T069 | Create Installation Guide | J | `docs/INSTALLATION.md` | 1 hr | T001-T068 |
| T070 | Create Administrator Guide | J | `docs/ADMIN_GUIDE.md` | 1 hr | T001-T068 |
| T071 | Create Deployment Guide | J | `docs/DEPLOYMENT.md` | 1 hr | T059, T060 |
| T072 | Create Database Schema Documentation | J | `docs/DATABASE_SCHEMA.md` | 1 hr | T032-T035 |

---

## P3 - Low Priority Tasks (Enhancements)

| ID | Task | Phase | File(s) | Effort | Dependencies |
|----|------|-------|---------|--------|-------------|
| T073 | Add missing test coverage | H | `tests/` | 4 hr | T056-T058 |
| T074 | Create .env.example | I | `.env.example` | 30 min | T046 |
| T075 | Configure SSL in Nginx | I | `docker/nginx.conf` | 1 hr | T059 |
| T076 | Verify deployment scripts | I | `scripts/*.sh` | 1 hr | T059, T060 |
| T077 | Create Troubleshooting Guide | J | `docs/TROUBLESHOOTING.md` | 1 hr | T001-T076 |
| T078 | Update Swagger annotations | J | `API/Controllers/*.cs` | 1 hr | T039-T045 |

---

## Task Summary by Phase

| Phase | P0 | P1 | P2 | P3 | Total |
|-------|----|----|----|----|-------|
| A - Build Stability | 23 | 0 | 0 | 0 | 23 |
| B - Backend Repair | 8 | 0 | 0 | 0 | 8 |
| C - Database Repair | 4 | 0 | 0 | 0 | 4 |
| D - API Repair | 0 | 11 | 0 | 0 | 11 |
| E - Frontend Repair | 0 | 3 | 0 | 0 | 3 |
| F - Security Hardening | 3 | 2 | 3 | 0 | 8 |
| G - Performance | 0 | 0 | 5 | 0 | 5 |
| H - Testing | 0 | 3 | 0 | 1 | 4 |
| I - Deployment | 0 | 2 | 0 | 3 | 5 |
| J - Documentation | 0 | 0 | 4 | 2 | 6 |
| **Total** | **38** | **21** | **12** | **6** | **77** |
