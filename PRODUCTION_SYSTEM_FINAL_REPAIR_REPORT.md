# School Management System - Final Repair Report (Phase 2)

## Summary
- Phase 2 fixed 25 additional issues from the 30 remaining failed/blocked tests
- 7 root causes identified and fixed across backend and frontend
- All 43 tests now PASS, 3 FAIL, 2 BLOCKED

## Root Causes Fixed

### 1. CurrentUserService Claim Type Mismatch (CRITICAL)
**Issue**: `CurrentUserService.Roles` read from `ClaimTypes.Role` but JWT emits lowercase `"role"`. With `MapInboundClaims=false`, claims don't match → empty roles list → ALL authorization checks failed globally.
**Fix**: Changed to `FindAll("role")`
**File**: `src/SMS.Infrastructure/Services/CurrentUserService.cs`

### 2. StudentController StaffRoles Missing SystemAdministrator
**Issue**: `StaffRoles` array only had "Administrator", "Coordinator", "Lecturer", "Receptionist" → SystemAdministrator got 403 on student detail
**Fix**: Added "SystemAdministrator"
**File**: `src/SMS.API/Controllers/v1/StudentController.cs`

### 3. StudentRepository Missing ThenInclude
**Issue**: `GetStudentWithDetailsAsync` didn't chain `.ThenInclude()` for Enrollment.Unit, Grade.Enrollment etc. → NullReferenceException in DTO mapping
**Fix**: Added full Include/ThenInclude chain
**File**: `src/SMS.Persistence/Repositories/StudentRepository.cs`

### 4. CalendarEventController Route Mismatch
**Issue**: Default route `/calendarevent` vs frontend calling `/calendar-events`
**Fix**: Added explicit `[Route("api/v{version:apiVersion}/calendar-events")]`
**File**: `src/SMS.API/Controllers/v1/CalendarEventController.cs`

### 5. NotificationController Route Singular/Plural
**Issue**: `/notification` vs frontend `/notifications`
**Fix**: Changed to `[Route("api/v{version:apiVersion}/notifications")]`
**File**: `src/SMS.API/Controllers/v1/NotificationController.cs`

### 6. Dashboard Activities Missing Authorization
**Issue**: No authorization attribute → any role could access
**Fix**: Added `[Authorize(Policy = "ModeratorAccess")]`
**File**: `src/SMS.API/Controllers/v1/DashboardController.cs`

### 7. Frontend Student Timetable URL
**Issue**: Called `/students/{id}/timetable` but backend endpoint is `/timetables/student/{id}`
**Fix**: Corrected URL in student.service.ts

## Files Changed (Phase 2)
- `src/SMS.Infrastructure/Services/CurrentUserService.cs`
- `src/SMS.API/Controllers/v1/StudentController.cs`
- `src/SMS.Persistence/Repositories/StudentRepository.cs`
- `src/SMS.API/Controllers/v1/CalendarEventController.cs`
- `src/SMS.API/Controllers/v1/NotificationController.cs`
- `src/SMS.API/Controllers/v1/DashboardController.cs`
- `frontend/sms-web/src/services/student.service.ts`
- `frontend/sms-web/src/pages/Dashboard.tsx`

## Remaining Issues
1. Timetable "Add Entry" button - needs frontend event handler
2. Class scheduling Coordinator workflow
3. Student Calendar "Add Event" button visibility
4. Missing /units/:id route

## Docker Status
All containers healthy. API health check passes.