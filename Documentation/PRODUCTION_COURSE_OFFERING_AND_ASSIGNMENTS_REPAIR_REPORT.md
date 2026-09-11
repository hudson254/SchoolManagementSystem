# PRODUCTION REPAIR REPORT — Course Offering Creation & Lecturer Assignments

**System:** School Management System (SMS)
**Server:** 192.168.110.161 (sms-server) · Debian 13 (trixie)
**Date:** 2026-09-11
**Deployed commit:** `6a4410c` (server branch `coordinator-repair`)
**DB backup:** `/opt/sms/app/backups/predeploy_courseoffering_repair.dump`

---

## 1. Issues

| # | Role | Function | Reported symptom |
|---|------|----------|------------------|
| 1 | System Administrator | Create Course Offering | HTTP 400 on `POST /api/v1/courseoffering` |
| 2 | Lecturer | Open Assignments | Browser 404 "Page Not Found … does not exist or has been moved" |
| 3 | Coordinator | Create Course Offering | HTTP 500 on `POST /api/v1/courseoffering` |

## 2. Root Causes

### Issue 1 — System Administrator HTTP 400
`frontend/sms-web/src/components/Forms/CourseOfferingForm.tsx` submitted the optional
registration dates as **empty strings** (`registrationStartDate: ""`,
`registrationEndDate: ""`) whenever the user left them blank. The backend command
`CreateCourseOfferingCommand.RegistrationStartDate/RegistrationEndDate` are nullable
`DateTime?` properties; the JSON deserialiser rejected `""` before the command handler
ran (production log: `400 in 9ms` with **no** `Handling CreateCourseOfferingCommand`
line; error body `"The JSON value could not be converted to System.Nullable'1[System.DateTime]"`).

### Issue 3 — Coordinator HTTP 500
The same form submitted the date fields as **date-only strings** (`"2026-09-01"`). These
bind to `DateTime` with `Kind = Unspecified`. Persisting them to the PostgreSQL
`timestamp with time zone` columns failed in the driver at
`CreateCourseOfferingCommandHandler.Handle` → `SaveChangesAsync`:

> `System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL
> type 'timestamp with time zone', only UTC is supported`

The 400/500 difference between roles is **not** an authorization difference: both roles
reach the same endpoint/policy (`ModeratorAccess`). It is determined by the entered
data — leaving the optional registration dates blank produced the binding 400; filling
them let binding pass and the save produce the 500.

### Issue 2 — Lecturer Assignments 404
The `/assignments` list route always existed, but every action offered on the Assignments
page navigated to routes that were **not declared** in `frontend/sms-web/src/App.tsx`:
- `Create Assignment` → `/assignments/new`
- row `View` → `/assignments/:id`
- row `Edit` → `/assignments/:id/edit`

React Router fell through to the catch-all `*` route → the NotFound page. The
`AssignmentForm` component existed (`src/components/Forms/AssignmentForm.tsx`) but was
never wired to any route/page. (The backend API is not at fault: `GET /api/v1/assignments`
exists and is covered by `LecturerAccess`.)
### Cross-role comparison (400 vs 500)
- Same endpoint, same request structure, same authorization policy (`ModeratorAccess`).
- No tenant/user-context difference (all test users belong to the default tenant).
- The split is purely data-dependent: blank optional date → binding 400; populated
  date-only dates → driver 500 on save.
- Both were caused by the **same frontend contract defect** (non-ISO date serialization),
  previously introduced when the CourseOffering model switched to free-text
  `AcademicYearName`/`SemesterName` columns.

### Additional defect discovered while verifying the required smoke tests
`CourseOfferings.tsx` / `Dashboard.tsx` consumed `getCourseOfferings()` as a paged
envelope (`data.items`), but the backend returns a **plain array**. The list therefore
rendered "No offerings" despite `200`. Normalised to the consumed shape in the frontend
service layer (backend contract unchanged).

### Pre-existing assignment-module gaps repaired as part of completing the workflow
- `Assignments.Description` is `NOT NULL`, but the DTO/entity treated it as optional →
  insert without a description returned 500. The handler now defaults to `""`.
- `CreateAssignmentCommand.SemesterId` was required but no UI surface supplies it; the
  seed migration comment already specified the server should resolve the current
  semester. `SemesterId` is now optional and resolved via
  `ISemesterRepository.GetCurrentOrDefaultAsync()` when absent.
- The web form sends `lecturerId = user?.id` (identity user id). `GetByIdAsync` was used
  directly; added a `GetByUserIdAsync` fallback in `ILecturerRepository`/
  `LecturerRepository`.
- Web-form datetimes (`yyyy-MM-ddTHH:mm`) bound as `Kind=Unspecified` → the Assignments
  commands now normalise dates to UTC (same `DateTimeUtc` helper as Course Offerings)
  and default `Description`/`Instructions` to `""`.
- The `CourseOfferingStatus` enum had **no `Scheduled` constant**, yet the frontend
  offers (and the Dashboard deliberately filters) `Scheduled`; choosing it produced a
  400. Appended `Scheduled = 5` **after** `Cancelled` so persisted ordinal values are
  not renumbered (production data used only 0 and 1).

## 3. Files Changed

Backend:
- `src/SMS.Application/Common/DateTimeUtc.cs` (new) — normalise DateTimes to UTC for
  `timestamp with time zone` columns.
- `src/SMS.Application/Features/CourseOfferings/Commands/CreateCourseOfferingCommand.cs` —
  normalise dates to UTC before persist.
- `src/SMS.Application/Features/CourseOfferings/Commands/UpdateCourseOfferingCommand.cs` —
  same normalisation for edits.
- `src/SMS.Domain/Enums/CourseOfferingStatus.cs` — add `Scheduled = 5`.
- `src/SMS.Application/Features/Assignments/Commands/CreateAssignmentCommand.cs` —
  `SemesterId` optional + server-side resolution, UTC dates, `Description` default,
  lecturer-by-user fallback, defensive lecturer display-name mapping.
- `src/SMS.Application/Features/Assignments/Commands/UpdateAssignmentCommand.cs` — UTC
  dates, `Description` default.
- `src/SMS.Domain/Interfaces/ILecturerRepository.cs` — add `GetByUserIdAsync`.
- `src/SMS.Persistence/Repositories/LecturerRepository.cs` — implement `GetByUserIdAsync`.

Frontend:
- `frontend/sms-web/src/components/Forms/CourseOfferingForm.tsx` — send ISO-8601 UTC
  datetimes; blank optional dates → `null`.
- `frontend/sms-web/src/services/course-offering.service.ts` — normalise the list
  endpoint's plain-array response to the consumed paged shape.
- `frontend/sms-web/src/pages/AssignmentFormPage.tsx` (new) — wraps `AssignmentForm`.
- `frontend/sms-web/src/pages/AssignmentDetailPage.tsx` (new) — assignment detail view.
- `frontend/sms-web/src/App.tsx` — register `assignments/new`, `assignments/:id`,
  `assignments/:id/edit`.
- `frontend/sms-web/src/components/Forms/AssignmentForm.tsx` — datetime normalisation,
  empty string defaults.

Tests:
- `tests/SMS.UnitTests/Common/DateTimeUtcTests.cs` (new, 4 tests).
- `tests/SMS.UnitTests/CourseOfferings/CourseOfferingCommandTests.cs` (+2 tests).
- `tests/SMS.UnitTests/Assignments/CreateAssignmentCommandTests.cs` (+2 tests; updated
  constructions for `ISemesterRepository`).

## 4. Database
- **No migration was required.** The production schema already contained
  `AcademicYearName`/`SemesterName` (migration `20260907160951_AddAcademicYearNameSemesterName`
  applied), and the API reports "Database is already up to date. No migrations to apply."
- `__EFMigrationsHistory` confirmed 12 applied migrations, including
  `20260907160951_AddAcademicYearNameSemesterName` and `20260909132333_SeedDefaultSemester`.
- The new `Scheduled` enum constant is stored as integer `5`; existing rows were only
  `0` (Draft) and `1` (Active), so no data shifting occurs.

## 5. Authorization
- No policy was weakened. Create/Update Offering remains `ModeratorAccess`
  (SystemAdministrator, Administrator, Coordinator); assignments remain `LecturerAccess`.
- Frontend visibility and backend enforcement were verified consistent for all roles.
- Lecturer assignments: list/detail/create confirm `200`/`201`; the role policy
  matrix from `Auth/CoordinatorAuthorizationTests` still passes.

## 6. Tests Performed
- Backend unit suite: **379 passed / 0 failed** (baseline 371; +8 new).
- Frontend type-check + production build: **PASS** (`tsc && vite build`).
- Frontend vitest (`AssignmentConfirm` etc.): PASS in this environment.
- Live production smoke matrix (13/13 PASS) and regression matrix (15/15 PASS) — see §8/§9.

## 7. Production Deployment Actions
1. DB backup `predeploy_courseoffering_repair.dump` taken before deployment.
2. Bundle `courseoffering-repair.bundle` applied; server at `6a4410c`.
3. `docker compose -f docker/docker-compose.prod.yml build api frontend` — PASS.
4. `docker compose -f docker/docker-compose.prod.yml up -d api frontend` — PASS.
5. Container health: `sms-api` (healthy), `sms-web` (healthy), `sms-postgres` (healthy).
6. `GET /health` → 200; frontend `/` → 200; Nginx up.
7. API log check after the final deployment window: **0** responses with HTTP 500.

## 8. Smoke Test Results (production, live — final matrix)

| Role                 | Function               | Before   | After        | Result |
| -------------------- | ---------------------- | -------- | ------------ | ------ |
| System Administrator | Create Course Offering | HTTP 400 | HTTP 201     | PASS   |
| Coordinator          | Create Course Offering | HTTP 500 | HTTP 201     | PASS   |
| Lecturer             | Open Assignments       | HTTP 404 | page loads   | PASS   |

Full matrix (13 steps): 13 PASS / 0 FAIL — login (SA, Coordinator, Lecturer), course
offering creation with the new frontend contract, defensive date-only payload,
offering presence in the list, lecturer assignments endpoint, lecturer `auth/me`.

## 9. Regression Results (production, live — final matrix)
15/15 PASS — course create/update/detail, unit create, offering create/update/detail
(update persists `Scheduled`), course detail, lecturer assignment create + detail,
coordinator login/list/offering-create.

## 10. Remaining Issues / Notes (pre-existing, NOT caused by this repair)
- `POST /api/v1/users` with a surname that is a recognised title/designation
  (e.g. `lastName = "Lecturer"`) can fail name parsing ("Only a single name part
  provided"). Existing behaviour; unrelated to this task.
- The `CourseOfferings.test.tsx` vitest file does not complete in this workstation's
  offline environment (pre-existing).
- The Assignments module has no delete-restriction policy (DELETE is `LecturerAccess`
  for any assignment); unchanged from the deployed contract, and out of scope here.
- `sw.js` service worker caches the SPA shell; browsers that cached the previous
  frontend build should hard-refresh once (not a code defect).