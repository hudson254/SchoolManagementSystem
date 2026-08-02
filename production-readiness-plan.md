# School Management System - Production Readiness Plan

## Current Status (as of latest audit)
- **Build**: ✅ 0 errors, 6 warnings (AutoMapper NU1608/NU1903 - user says ignore)
- **Unit Tests**: ✅ 68/68 pass
- **API Tests**: ❌ 20/20 fail (401 Unauthorized - JWT key mismatch suspected)
- **Integration Tests**: ❌ 4/4 fail (Docker required, no InMemory fallback)
- **Stubs**: Several handlers in `_ControllerStubs.cs` still need real implementations

---

## Phase 1: Fix JWT Auth Flow (Fix 401 on API Tests)
**Root cause hypothesis**: `Program.cs` token validation uses env var `JWT_SECRET` override, but `JwtService.cs` signs using config-only secret. If `JWT_SECRET` env var is set during tests, tokens are signed with one key but validated with another.

### Steps:
1. Read `Program.cs` and `JwtService.cs` to verify JWT secret handling
2. Align signing and validation to use the same key source
3. Re-run `dotnet test tests/SMS.ApiTests`
4. If still failing, debug further (auth controller, login endpoint)

## Phase 2: Fix Integration Tests (InMemory Fallback)
1. Read `DatabaseFixture.cs` 
2. Modify to fall back to InMemory when Docker is unavailable
3. Re-run `dotnet test tests/SMS.IntegrationTests`

## Phase 3: Replace All Stub Handlers
1. Inventory all stubs in `_ControllerStubs.cs`
2. Replace each with real implementation:
   - Building handlers (GetBuildings, GetBuilding)
   - Assignment handlers (GetSubmissions, GetSubmission, GetStudentAssignments, DeleteAssignment)
   - Course handlers (GetCourseUnits, GetCourseProgrammes)
   - Dashboard handlers (GetPerformanceMetrics, GetCourseStatistics)
   - And all others listed in REPAIR_PROGRESS.md

## Phase 4: Code Quality & Warnings
1. Fix CS8618, CS8601, CS8604 nullable warnings
2. Fix ApplicationDbContext.UserRoles hiding inherited member
3. Remove dead code and commented-out code
4. Remove unused usings

## Phase 5: Database & Migrations
1. Create EF Core initial migration
2. Add proper indexes on foreign keys
3. Add seed data script
4. Fix concurrency handling

## Phase 6: Security Hardening
1. Move JWT secret to environment variables (verify already done)
2. Move SMTP credentials to environment variables
3. Move database password to environment variables
4. Add CSRF protection
5. Add rate limiting configuration
6. Add input validation

## Phase 7: Frontend Verification
1. Verify React dependencies
2. Fix any import path issues
3. Verify build

## Phase 8: Docker & Deployment
1. Verify Docker configuration
2. Add health checks
3. Configure logging
4. Update documentation

## Phase 9: Final Verification
1. Full clean build
2. All tests pass
3. Production readiness checklist complete
