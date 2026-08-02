# Phase 1 - Test Infrastructure Repair TODO

**Goal:** Make `dotnet test` pass cleanly for `SMS.ApiTests` and `SMS.IntegrationTests` with no `ObjectDisposedException` and no stale Testcontainers reference.

## Root Causes Identified

1. **ApiTestFixture ObjectDisposedException (20/20 API tests fail)**
   - `DisposeAsync()` (both `WebApplicationFactory` override and `IAsyncLifetime` explicit implementation) called `Services.CreateScope()` after the factory was disposed.
   - No double-dispose guard existed.

2. **Integration Tests fail (4/4)**
   - `DatabaseFixture` had a Testcontainers PostgreSql fallback but Docker is unavailable; fixture already falls back to InMemory, but stale `Testcontainers.PostgreSql 4.1.0` reference remained in `SMS.IntegrationTests.csproj` and `Directory.Build.props`.

3. **AutoMapper** — already pinned to 13.0.1 in `Directory.Build.props`. NO ACTION NEEDED.

## Fix Items

### 1. ApiTestFixture.cs — FIXED ✅
- [x] Added `_disposed` double-dispose guard flag.
- [x] Cached admin token (`_cachedAdminToken`) to avoid post-dispose `Services` access.
- [x] `DisposeAsync()` now guards scope creation with try/catch on `ObjectDisposedException`.
- [x] Added `CreateAuthenticatedClientAsync()`; kept sync `CreateAuthenticatedClient()` for existing callers.
- [x] Fixed `NormalizedName` → `NormalizedUserName` property on `User` entity.

### 2. Remove stale Testcontainers reference — FIXED ✅
- [x] Removed `Testcontainers.PostgreSql` from `tests/SMS.IntegrationTests/SMS.IntegrationTests.csproj`.
- [x] Removed `Testcontainers.PostgreSql` update pin from `Directory.Build.props`.

### 3. AutoMapper — NO ACTION ✅
- [x] Verified 13.0.1 pinned in Directory.Build.props (already aligned).

## Verification Steps
- [ ] `dotnet build SchoolManagementSystem.sln` → 0 errors
- [ ] `dotnet test tests/SMS.ApiTests` → all pass
- [ ] `dotnet test tests/SMS.IntegrationTests` → all pass (InMemory fallback)
- [ ] `dotnet test tests/SMS.UnitTests` → 47/47 still pass

## Acceptance Criteria
- No `ObjectDisposedException` in API test output.
- No leftover `Testcontainers` package reference anywhere in solution.

