# Dependency Map - School Management System Repair

---

## Task Dependency Graph

```
Critical Path (must be sequential):
─────────────────────────────────────────────────────────────────────────
T001-T021 ──▶ T022-T026 ──▶ T027-T031 ──▶ T032-T035 ──▶ T039-T049
(Build Core)   (Fix Domain)   (Backend)      (Database)     (API)

T036-T038 ──▶ T053-T055 ──▶ T066-T068
(Security Quick)   (Security)      (Security Deep)

T056 ──▶ T057 ──▶ T058 ──▶ T073
(Unit Tests)   (Int. Tests)   (API Tests)   (Coverage)

T059 ──▶ T060 ──▶ T074 ──▶ T075 ──▶ T076
(Dockerfile)  (Compose)     (.env)   (SSL)    (Scripts)

T069 ──▶ T070 ──▶ T071 ──▶ T072 ──▶ T077 ──▶ T078
(Docs)                                           
```

---

## Level 0: No Dependencies (Can Start Immediately)

| Task ID | Description | Phase | Effort |
|---------|-------------|-------|--------|
| T001 | Fix 50 files with missing `using` | A | 15 min |
| T002 | Remove stray line in DI | A | 1 min |
| T004 | Create IBaseEntity | A | 10 min |
| T005 | Create ISoftDelete | A | 10 min |
| T006 | Create NotFoundException | A | 10 min |
| T007 | Create UnauthorizedException | A | 10 min |
| T008 | Create ConflictException | A | 10 min |
| T009 | Create ValidationException | A | 10 min |
| T010 | Create AuthResponseDto | A | 15 min |
| T011 | Create CourseDetailsDto | A | 15 min |
| T012 | Create UnitDto | A | 10 min |
| T013 | Create ProgrammeSummaryDto | A | 10 min |
| T014 | Fix StudentDto | A | 15 min |
| T016 | Create LoggingBehavior | A | 20 min |
| T017 | Create LoginHistory entity | A | 15 min |
| T018 | Create Notification entity | A | 15 min |
| T019 | Create AuditLog entity | A | 15 min |
| T020 | Create Department entity | A | 15 min |
| T021 | Create Programme entity | A | 15 min |
| T027 | Fix Program.cs imports | B | 15 min |
| T029 | Create LocalFileStorageService | B | 30 min |
| T031 | Create TenantResolutionMiddleware | B | 30 min |
| T036 | Fix CORS policy | F | 15 min |
| T037 | Remove hardcoded password | F | 15 min |
| T038 | Fix AutoMapper vulnerability | F | 15 min |
| T046 | Create appsettings.json | D | 30 min |
| T048 | Add API response envelope | D | 30 min |
| T050 | Audit frontend project | E | 1 hr |
| T053 | Add password policy validation | F | 30 min |
| T061 | Add DbContext pooling | G | 15 min |
| T063 | Add pagination defaults | G | 30 min |
| T064 | Add response compression | G | 15 min |
| T065 | Fix async patterns | G | 1 hr |

---

## Level 1: Single Dependency

| Task ID | Description | Depends On | Phase |
|---------|-------------|------------|-------|
| T003 | Fix ICurrentUserService | T004 (IBaseEntity) | A |
| T015 | Create ValidationBehavior | T006-T009 (Exceptions) | A |
| T022 | Fix BaseEntity.Id to Guid | T004 (IBaseEntity) | A |
| T028 | Register ICurrentUserService | T003 | B |
| T030 | Create ExceptionHandlingMiddleware | T006-T009 | B |
| T047 | Create appsettings.Development.json | T046 | D |
| T055 | Add rate limiting | T036 (CORS) | F |
| T066 | Add CSRF protection | T053 | F |
| T067 | JWT secret protection | T046 | F |
| T068 | Add input sanitization | T030 | F |
| T074 | Create .env.example | T046 | I |
| T075 | Configure SSL in Nginx | T059 | I |

---

## Level 2: Two Dependencies

| Task ID | Description | Depends On | Phase |
|---------|-------------|------------|-------|
| T023 | Update entity FK properties to Guid | T022 | A |
| T024 | Fix Student entity properties | T022 | B |
| T025 | Fix Course entity properties | T022 | B |
| T026 | Fix Unit entity properties | T022 | B |
| T032 | Implement OnModelCreating | T024-T026 | C |
| T033 | Implement SaveChangesAsync | T032 | C |
| T034 | Add DbSet declarations | T032 | C |
| T035 | Create fresh migration | T032-T034 | C |
| T039 | Create AuthController | T010, T030, T031 | D |
| T040 | Create StudentsController | T014, T030, T031 | D |
| T041 | Create CoursesController | T011, T030, T031 | D |
| T042 | Create UnitsController | T012, T030, T031 | D |
| T043 | Create AccommodationController | T030, T031 | D |
| T044 | Create AssignmentsController | T030, T031 | D |
| T045 | Create DashboardController | T030, T031 | D |
| T049 | Configure API versioning | T039-T045 | D |
| T054 | Add security headers middleware | T036 | F |
| T062 | Implement query caching | T039-T045 | G |

---

## Level 3: Three or More Dependencies

| Task ID | Description | Depends On | Phase |
|---------|-------------|------------|-------|
| T051 | Create API client library | T039-T045 | E |
| T052 | Fix frontend structure | T050, T051 | E |
| T056 | Fix unit tests | T001-T038 (Phase A+B) | H |
| T057 | Fix integration tests | T032-T035 (Database) | H |
| T058 | Fix API tests | T039-T045 (API) | H |
| T059 | Fix Dockerfiles | T001-T058 (All above) | I |
| T060 | Fix Docker Compose | T059 | I |
| T076 | Verify deployment scripts | T059, T060 | I |
| T069 | Create Installation Guide | T001-T068 (All above) | J |
| T070 | Create Administrator Guide | T001-T068 | J |
| T071 | Create Deployment Guide | T059, T060 | J |
| T072 | Create Database Schema Doc | T032-T035 | J |
| T073 | Add missing test coverage | T056-T058 | H |
| T077 | Create Troubleshooting Guide | T001-T076 | J |
| T078 | Update Swagger annotations | T039-T045 | J |

---

## Parallel Execution Opportunities

### Can Be Done in Parallel with Phase A:
- T027: Fix Program.cs imports (API project, no dependencies)
- T029: Create LocalFileStorageService (Infrastructure, no dependencies)
- T031: Create TenantResolutionMiddleware (API, no dependencies)
- T036-T038: Security quick wins (no dependencies)
- T046: Create appsettings.json (no dependencies)
- T050: Audit frontend (no dependencies)
- T061, T063-T065: Performance (no dependencies)

### Can Be Done in Parallel with Phase B:
- T035: Database migration (depends only on Phase B entity fixes)
- T066-T068: Deep security (depends on Phase B middleware)

### Can Be Done in Parallel with Phase D:
- Phase E Frontend: Can start API client after controllers are designed
- Phase F Security: Can start after Program.cs is stable

### Can Be Done in Parallel with Phase H:
- Phase I Deployment: Dockerfiles can be prepared while testing
- Phase J Documentation: Can start writing docs while testing

---

## Critical Blockers

The following tasks block ALL subsequent work:

1. **T001** (Fix 50 files) - Blocks Application layer compilation
2. **T022** (Fix BaseEntity ID type) - Blocks all entity alignment
3. **T032** (OnModelCreating) - Blocks all database work
4. **T039** (AuthController) - Blocks frontend API integration

---

## Dependency Chain Summary

```
Independent ──▶ Level 1 ──▶ Level 2 ──▶ Level 3 ──▶ Level 4+
   32 tasks       14 tasks    16 tasks    15 tasks     1 task
    
Legend:
    ───▶  Sequential dependency
    ───▶  Parallel opportunity
    Blocking: T001, T022, T032, T039
