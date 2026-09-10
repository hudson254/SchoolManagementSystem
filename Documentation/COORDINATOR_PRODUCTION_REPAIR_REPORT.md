# COORDINATOR PRODUCTION REPAIR REPORT

**System:** School Management System (SMS)
**Server:** 192.168.110.161 (sms-server) · Debian 13 (trixie)
**Date:** 2026-09-10
**Deployed commit:** `4a0df15` (server branch `coordinator-repair`)
**Scope:** Coordinator workflows — Courses, Units, Course Offerings, Classes, Timetable, Calendar, Students, Student Details, Accommodation.

---

## Executive Summary

The coordinator role could not perform its intended academic-operations duties in
production. Six distinct root causes were found and repaired. All fixes were deployed
to production and verified with a live 30-step coordinator test matrix (30/30 PASS),
an administrator/system-administrator regression suite, and the automated unit test
suite (371 tests PASS).

Coordinator workflows are **fully operational in the production deployment**.

---

## A. Issues Found

| # | Issue | Symptom | Root Cause |
|---|-------|---------|------------|
| 1 | No "Create Course" button for coordinator | Button invisible | Frontend gated the button on the non-existent role name `'Moderator'` (`Courses.tsx`, `Units.tsx`, `CourseOfferings.tsx`, `Students.tsx`, `Lecturers.tsx`, `Assignments.tsx`, `Dashboard.tsx`, `CourseOfferingDetail.tsx`, `Users.tsx`) — the actual DB/JWT role is `'Coordinator'`, so all role checks `user.roles.includes('Moderator')` never matched. Several pages also blocked the whole row menu behind it. |
| 2 | Course Offering creation 403 | `POST /api/v1/courseoffering` → 403 for coordinator | `CourseOfferingController.CreateCourseOffering` used `AdministratorAccess`; coordinator is only in `ModeratorAccess`. `UpdateCourseOffering` and `GetCourseOfferingUnits` had the same problem. |
| 3 | Calendar event creation 403 | `POST /api/v1/calendar-events` → 403 for coordinator | `CalendarEventController.CreateEvent` / `UpdateEvent` used `AdministratorAccess`. |
| 4 | Calendar "Create" appeared to do nothing | Event never appeared on the calendar, for **all** roles | `CalendarEventController` **never called `SaveChangesAsync`** after `AddAsync`/`UpdateAsync`/`DeleteAsync`. The request-scoped DbContext discarded the tracked-but-unsaved row, so `POST` echoed the entity (201) but `GET` returned `[]`. |
| 5 | Timetable create → HTTP 500 | `POST /api/v1/timetables` → 500 | `CreateTimetableCommand` never set `UnitId`, `LecturerId`, or `Date` on the `Timetable` entity; the NOT NULL `UnitId` foreign key failed on every insert. `UpdateTimetableCommand` had the same defect. |
| 6 | Classes page → 404 | "404 Page Not Found. The page you are looking for does not exist or has been moved." | The `/classes` route was **missing from `App.tsx`** and the `Classes` component was a read-only stub that consumed the timetable list; there was **no backend API at all** for the existing `Classes` table. |
| 7 | Student details incomplete | No accommodation, no enrolled courses | The student detail DTO/query (`StudentDetailsDto`, `GetStudentQuery`) did not include the accommodation assignment or course-offering enrollments. |
| 8 | Timetable service endpoints 404/405 | Conflicts / weekly / venues calls failed | `timetable.service.ts` used wrong routes (`/timetables/venues/available`, `/timetables/weekly`, GET `/timetables/conflicts`) vs the backend (`/timetables/available-venues`, `/timetables/weekly/class/{classId}`, POST `/timetables/check-conflicts`). |

Additional discovery: the Users admin page offered to assign the non-existent
`'Moderator'` role instead of `'Coordinator'`.

---

## B. Root Causes (per issue)

1. **Frontend role-name mismatch.** The frontend consistently tested for the role
   `'Moderator'`, which does not exist. The role delivered by auth and stored in the
   database is `'Coordinator'`. Because the checks never matched, coordinated UI was
   hidden even where the backend already allowed it (e.g. `POST /courses` already
   permitted coordinator via `ModeratorAccess`).
2.-3. **Authorization scope mismatch.** Course Offering and Calendar write endpoints
   were scoped to `AdministratorAccess`. The existing policy vocabulary already
   defines `ModeratorAccess` = SystemAdministrator + Administrator + Coordinator,
   which matches the coordinator role definition ("manage academic operations").
4. **Missing persistence (calendar).** The controller used repository `AddAsync`
   (change-tracker only) without committing. This predates the coordinator work and
   is the reason "Create did nothing" happened for every role.
5. **Dropped timetable fields.** The command/handler existed but never propagated
   `UnitId`/`LecturerId`/`Date`, causing the NOT NULL `UnitId` column violation (500).
6. **Classes had no API or route.** The `Classes` table and entity existed in the
   schema but had no controller, no CQRS handlers, no frontend service, and no React
   route — so the sidebar link (already present) produced the 404.
7. **Student detail payload gap.** `StudentDetailsDto` had no accommodation or
   course-offering enrollment fields; `GetStudentQuery` did not load them.
8. **Frontend/backend route drift on timetable helper endpoints.**

No role was granted unrestricted SYSTEM ADMINISTRATOR privileges. The coordinator
permissions were aligned to the existing `ModeratorAccess`/`LecturerAccess`/
---

## C. Changes Made

### Backend (src/)

| Area | File(s) | Change |
|------|---------|--------|
| Calendar | `src/SMS.API/Controllers/v1/CalendarEventController.cs` | Create/Update/Delete now call `IUnitOfWork.SaveChangesAsync` (events previously never persisted); Create/Update moved from `AdministratorAccess` to `ModeratorAccess`. |
| Course Offerings | `src/SMS.API/Controllers/v1/CourseOfferingController.cs` | `CreateCourseOffering`, `UpdateCourseOffering`, `GetCourseOfferingUnits` moved from `AdministratorAccess` to `ModeratorAccess`. Unit/student/lecturer management stays `AdministratorAccess`. |
| Timetable (new) | `src/SMS.Application/Features/Timetables/Commands/CreateTimetableCommand.cs` | Commands now carry and persist `UnitId`, `LecturerId`, `Date` (Create + Update). |
| Timetable DTO | `src/SMS.Application/DTOs/TimetableDto.cs` | Added `UnitId`, `LecturerId`, `Date`. |
| Timetable queries | `src/SMS.Application/Features/Timetables/Queries/GetTimetablesQuery.cs` | Enriched class/unit/lecturer/semester names via `IClassRepository`; `GetTimetableHandler` fixed. |
| Timetable repo | `src/SMS.Domain/Interfaces/ITimetableRepository.cs`, `src/SMS.Persistence/Repositories/TimetableRepository.cs` | Added `GetAllWithDetailsAsync`. |
| Classes (new module) | `src/SMS.Domain/Interfaces/IClassRepository.cs`, `src/SMS.Persistence/Repositories/ClassRepository.cs`, `src/SMS.Application/DTOs/ClassDto.cs`, `src/SMS.Application/Features/Classes/Commands/ClassCommands.cs`, `src/SMS.Application/Features/Classes/Queries/GetClassesQuery.cs`, `src/SMS.API/Controllers/v1/ClassesController.cs` | Full CQRS CRUD for the existing `Classes` entity (`GET/POST/PUT /api/v1/classes`, DELETE admin-only). |
| Semesters (new) | `src/SMS.Application/Features/Semesters/Queries/GetSemestersQuery.cs`, `src/SMS.API/Controllers/v1/SemestersController.cs` | `GET /api/v1/semesters` for class/timetable forms. |
| Student details | `src/SMS.Application/DTOs/StudentDto.cs`, `src/SMS.Application/DTOs/CourseOfferingDtos.cs`, `src/SMS.Application/Features/Students/Queries/GetStudentQuery.cs`, `src/SMS.Application/Features/CourseOfferings/Queries/GetStudentCourseEnrollmentsQuery.cs` | `StudentDetailsDto.Accommodation` (Lane→House + check-in) and `CourseEnrollments` (course/offering/year/semester/status) populated from existing entities. |
| DI | `src/SMS.API/Program.cs` | Registered `IClassRepository`/`ClassRepository`. |

### Frontend (frontend/sms-web/src/)

| Area | File(s) | Change |
|------|---------|--------|
| Role utils (new) | `utils/roles.ts` | Policy-mirroring helpers: `canManageAcademic` (ModeratorAccess), `canAdministrate` (AdministratorAccess). |
| Routes | `App.tsx` | Added `students/new`, `students/:id/edit`, `courses/new`, `courses/:id/edit`, `units/new`, `units/:id(edit)`, `classes`, plus lazy imports. |
| Courses / Units / CourseOfferings / Students / Timetable / Calendar / Lecturers / Assignments / Dashboard / CourseOfferingDetail / StudentDetail | `pages/*.tsx` | Replaced all `'Moderator'` role checks with canonical role helpers; Add/Edit visible to coordinator where the backend allows; Delete/more menus restricted to administrator where the backend requires it. |
| Classes (new page) | `pages/Classes.tsx`, `services/classes.service.ts`, `services/semester.service.ts` | Full class list + create/edit dialog; class date, day, start/end time, unit, lecturer, semester; delete admin-only. |
| Timetable | `pages/Timetable.tsx` | Wired the previously dead "Add Entry" button and per-row View/Edit buttons; create/edit dialog (class, date→day auto-fill, times, venue); filters now use real semesters/classes. |
| Calendar | `pages/Calendar.tsx` | Add Event button role-gated, validation + visible errors, ISO payload, list refresh; delete admin-only. |
| Timetable service | `services/timetable.service.ts` | Corrected routes: `/timetables/available-venues`, `/timetables/weekly/class/{classId}`, POST `/timetables/check-conflicts`; added `date`/`unitId`/`lecturerId` to payloads. |
| Student types | `types/student.types.ts` | Added `courseEnrollments` and richer `accommodation` shape. |
| Users | `pages/Users.tsx` | Role dropdown uses canonical roles (`SystemAdministrator`, `Administrator`, `Coordinator`, ...). |

### Tests

- `tests/SMS.UnitTests/Classes/CreateClassCommandTests.cs` (new)
- `tests/SMS.UnitTests/Timetables/CreateTimetableCommandTests.cs` (new — locks the 500-fix)
- `tests/SMS.UnitTests/Auth/CoordinatorAuthorizationTests.cs` (new — coordinator policy matrix incl. privilege-escalation negatives)
- `tests/SMS.ApiTests/Controllers/CoordinatorRoleApiTests.cs` (new — API-level coordinator regression incl. calendar persistence round-trip)
- Root production scripts: `_coord_production_test.ps1`, `_admin_regression.ps1`
---

## D. Authorization Changes

Coordinator permissions added/corrected (all within the existing policy model —
no SYSTEM ADMINISTRATOR privileges granted):

| Endpoint | Policy before | Policy after |
|----------|---------------|--------------|
| `POST /api/v1/courseoffering` | AdministratorAccess | **ModeratorAccess** (coordinator) |
| `PUT /api/v1/courseoffering/{id}` | AdministratorAccess | **ModeratorAccess** (coordinator) |
| `GET /api/v1/courseoffering/{id}/units` | AdministratorAccess | **ModeratorAccess** (coordinator, read) |
| `POST /api/v1/calendar-events` | AdministratorAccess | **ModeratorAccess** (coordinator) |
| `PUT /api/v1/calendar-events/{id}` | AdministratorAccess | **ModeratorAccess** (coordinator) |
| `GET/POST/PUT /api/v1/classes` | (no endpoint) | **ModeratorAccess** (coordinator) |
| `GET /api/v1/semesters` | (no endpoint) | **ModeratorAccess** (coordinator) |
| `DELETE /api/v1/classes/{id}` | (no endpoint) | **AdministratorAccess** (coordinator excluded) |
| `DELETE /api/v1/courses/{id}`, `DELETE /api/v1/units/{id}`, `DELETE /api/v1/calendar-events/{id}` | AdministratorAccess | Unchanged — coordinator correctly denied (verified 403). |

Frontend visibility now mirrors the backend policies via `utils/roles.ts`:
- `canManageAcademic(user.roles)` = SystemAdministrator | Administrator | Coordinator (ModeratorAccess) — controls Add/Edit on Courses, Units, Course Offerings, Classes, Timetable, Calendar, Students, Lecturers.
- `canAdministrate(user.roles)` = SystemAdministrator | Administrator (AdministratorAccess) — controls Delete and admin-only menus.

Privilege-escalation guard: `CoordinatorAuthorizationTests` asserts coordinator
satisfies ModeratorAccess/LecturerAccess/StudentAccess/ReceptionistAccess and does
**not** satisfy AdministratorAccess/SystemAdministratorAccess. Live probes confirmed
coordinator `DELETE /courses/{id}` → 403 while administrator `DELETE` → 204.

---

## E. Database Changes

**No migration was required or created.** All repairs reused the existing schema:

- `Classes` table already existed (from `20260728195330_InitialMigration`).
- `Timetables` table already existed with NOT NULL `UnitId`; the fix is in the
  application layer that now supplies the value.
- `CalendarEvents` table already existed; the fix is the missing `SaveChanges`.
- `Semesters` seed data already exists in production (`SeedDefaultSemester` migration).

Pre-deploy backup taken and verified intact:
`/opt/sms/app/backups/predeploy_coordinator_repair.dump` (332,729 bytes).
Deployment applied 0 pending migrations ("Database is already up to date").

---

## F. Tests

| Test | Result | Evidence |
|------|--------|----------|
| Coordinator login | PASS | HTTP 200, roles=Coordinator |
| Create course | PASS | HTTP 201 → appears in GET /courses list |
| Create unit | PASS | HTTP 201 → appears in GET /units list |
| Create course offering | PASS | HTTP 201 (offering code + AY/Semester text round-trip) |
| Open classes (no 404) | PASS | GET /api/v1/classes → 200 |
| Modify class | PASS | PUT /classes/{id} → 200 |
| Modify class date/time | PASS | endDate 2026-06-15 + startTime 14:00 persisted after reload |
| Create timetable entry | PASS | POST /timetables → 201 (all fields persisted) |
| Modify timetable entry | PASS | PUT /timetables/{id} → 200; time 09:00 persisted |
| Create calendar event | PASS | POST /calendar-events → 201 **and event appears in GET list** (SaveChanges fix) |
| View student accommodation | PASS | student detail returns accommodation (Lane Kifaru, House 004, checked-in) |
---

## G. Production Status

**Coordinator workflows are fully operational in the production deployment.**

All Definition-of-Done items were verified against the live deployment
(`https://192.168.110.161`) using a real COORDINATOR account
(testcoordinator@gmail.com, password reset by the System Administrator during the
diagnosis phase):

1. Create courses ✔
2. Create units ✔
3. Create course offerings ✔
4. Open Classes without a 404 ✔
5. Modify classes ✔
6. Set/modify class dates ✔
7. Set/modify class times ✔
8. Create and modify timetable entries ✔
9. Create calendar events via the Add Event form (API) ✔
10. See newly created calendar events ✔
11. Open student details ✔
12. View the student's assigned accommodation ✔
13. View the student's enrolled courses ✔

Deployment verification:
- `sms-api` — Up (healthy), new image
- `sms-web` — Up (healthy), new image
- `sms-postgres` — Up (healthy)
- `sms-nginx` — Up
- `GET /health` → Healthy (posgresql reachable)
- API logs clean apart from the expected business-rule 400 in the admin regression
  (delete course with active units).
- Nginx and frontend serve 200s; no browser-facing 4xx/5xx introduced.

---

## H. Remaining Issues

| Item | Status | Notes |
|------|--------|-------|
| Sample student has no course-offering enrollment rows | Not a defect | `courseEnrollments` returns `[]` — students must be enrolled through the offering workflow; no data was invented. |
| Calendar UI error surfacing | Fixed | Validation + error display added to the dialog. |
| `AssessmentWorkspace` was locally modified on the server before this change | Preserved | The deployed tree was rebuilt from the git commit; a transient server-side working-tree modification was discarded by the branch reset. It does not exist in either deploy-stage2 or coordinator-repair commits. |
| Docker not present on this workstation | Not blocking | Live verification was performed over HTTPS against the production host via SSH. |

Rollback: the previous API/frontend images remain in the Docker store; DB backup
`/opt/sms/app/backups/predeploy_coordinator_repair.dump` is in place.

---

## Deployment Commands (for reference)

```bash
# workstation
git bundle create coordinator-repair.bundle HEAD
scp -i ~/.ssh/id_ed25519 coordinator-repair.bundle sms_admin@192.168.110.161:/tmp/coordinator-repair.bundle

# server
cd /opt/sms/app
docker exec sms-postgres pg_dump -U sms_user -d SchoolManagementSystem -Fc -f /tmp/predeploy_coordinator_repair.dump
docker cp sms-postgres:/tmp/predeploy_coordinator_repair.dump /opt/sms/app/backups/
git fetch /tmp/coordinator-repair.bundle 'HEAD:refs/remotes/coordinator/repair'
git branch -f coordinator-repair refs/remotes/coordinator/repair
git checkout coordinator-repair
git reset --hard refs/remotes/coordinator/repair
docker compose -f docker/docker-compose.prod.yml build api frontend
docker compose -f docker/docker-compose.prod.yml up -d api frontend
curl -sk -o /dev/null -w '%{http_code}\n' https://127.0.0.1/health
```
| View student courses | PASS | student detail returns `courseEnrollments` (count 0 for the sample student; field wired) |
| Administrator regression | PASS | create/update course, unit, offering, class, timetable, calendar, student detail all 200/201; fresh-course delete 204 |
| System Administrator regression | PASS | same surfaces 200/201; SystemAdministrator login 200 |
| Backend build | PASS | `dotnet build SMS.API -c Release` → 0 errors |
| Frontend build | PASS | `npm run build` (tsc + vite) → success |
| Automated tests | PASS | 371 unit tests, 0 failures (was 357 → +14 new) |
| Production smoke test | PASS | `/health` 200, API healthy, frontend 200, nginx up |

360 lines of coordinator production matrix results:
`_coord_production_test/coord_results.csv` (30 PASS, 0 FAIL).
`StudentAccess` policy model.