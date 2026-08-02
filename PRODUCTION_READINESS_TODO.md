# Production Readiness Remediation - Execution TODO

## Phase A: Backend Clean Build & Baseline
- [ ] Delete all bin/obj directories
- [ ] dotnet restore
- [ ] dotnet build - verify 0 errors
- [ ] Document remaining warnings

## Phase B: Replace All Placeholder Code
- [ ] Fix ForgotPasswordCommand (always returns 204 regardless of SMTP)
- [ ] Verify SMS.Reporting vs SMS.Infrastructure reporting implementations
- [ ] Remove dead ServiceExtensions/AddReporting DI or wire correctly
- [ ] Verify no NotImplementedException / placeholder stubs remain in src/

## Phase C: Database/Migration Audit
- [ ] Resolve shadow FK warnings in ApplicationDbContext
- [ ] Add missing FK indexes
- [ ] Create initial EF Core migration
- [ ] Create seed data (roles, admin user)
- [ ] Verify seed script invocation

## Phase D: Frontend Repair (169 TypeScript errors)
- [ ] Create missing service files (grade, report, timetable, user, calendar)
- [ ] Add @fullcalendar/* dependencies to package.json
- [ ] Fix ProtectedRoute/useApi/theme.ts type errors
- [ ] Fix all TS6133 unused-import errors
- [ ] Add missing StudentDetails.accommodation type
- [ ] Fix courseService.getProgrammes() signature
- [ ] Fix duplicate JSX attribute errors
- [ ] Verify npm run build passes

## Phase E: Security Hardening
- [ ] Verify SMTP credentials via env vars
- [ ] Verify CSRF posture documented
- [ ] Verify rate limiting config
- [ ] Verify response compression + security headers

## Phase F: Testing
- [ ] Re-run Unit Tests
- [ ] Re-run API Tests
- [ ] Re-run Integration Tests
- [ ] Fix any regressions

## Phase G: Documentation
- [ ] Update README with accurate status
- [ ] Generate/refresh Production Readiness Checklist
- [ ] Update Deployment + Environment docs

## Phase H: Final Production Validation
- [ ] Full clean build
- [ ] All tests pass
- [ ] Frontend builds
- [ ] Docker config verified
- [ ] Final audit report produced

