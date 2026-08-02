# Production Readiness Remediation - TODO Tracker

## Current Status: In Progress

### Step 1: Fix EF Core Shadow FK Properties
- [ ] Fix `Student→Programme` relationship (`.WithMany(p => p.Students)`) to remove `ProgrammeId1`
- [ ] Fix `AccommodationAssignment→Student` duplicate relationship to remove `StudentId1`
- [ ] Configure `UserRole` navigations to use existing `UserId`/`RoleId` FKs to remove `UserId1`/`RoleId1`
- [ ] Resolve global query filter warning on User/UserRole navigation
- [ ] Regenerate EF Core migration to match corrected model

### Step 2: Disable SMTP Services Completely
- [ ] Add `Enabled` flag to `EmailOptions` (default false)
- [ ] Make `EmailService` a no-op with warning log when SMTP disabled
- [ ] Set `SMTP.Enabled=false` in all appsettings files
- [ ] Remove SMTP env configuration from Docker compose / Dockerfile
- [ ] Update docs referencing SMTP activation

### Step 3: Delete Obsolete Stub File
- [ ] Delete `src/SMS.Application/Features/_ControllerStubs.cs` (verified empty, stubs migrated)

### Step 4: Frontend Fixes
- [ ] Create missing `src/test/setup.ts` for Vitest
- [ ] Fix React 19 / @types/react version mismatch
- [ ] Verify frontend TypeScript build

### Step 5: Docker & Deployment Fixes
- [ ] Create missing `init-db.sql` for postgres init
- [ ] Create missing `nginx-frontend.conf`
- [ ] Create missing `prometheus.yml`, grafana provisioning files
- [ ] Create `Dockerfile.backup` if referenced
- [ ] Verify docker-compose.prod.yml consistency

### Step 6: Performance & Security
- [ ] Add Response Compression middleware
- [ ] Add Kestrel HTTPS config validation / .env template
- [ ] Verify rate limiting, security headers

### Step 7: Full Verification
- [ ] Clean build (delete bin/obj, restore, rebuild) - 0 errors
- [ ] Run Unit Tests - all pass
- [ ] Run API Tests - all pass
- [ ] Run Integration Tests - all pass
- [ ] Frontend build passes

### Step 8: Documentation & Reports
- [ ] Update REPAIR_PROGRESS.md
- [ ] Update production readiness checklist
- [ ] Write final Production Readiness Audit Report
- [ ] Produce deliverables report (issues, fixes, files modified)

