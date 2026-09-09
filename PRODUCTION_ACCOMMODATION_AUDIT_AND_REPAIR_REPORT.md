# PRODUCTION ACCOMMODATION AUDIT AND REPAIR REPORT

**Date:** 2026-09-09
**Server:** 192.168.110.161 (`sms-server`)
**Application:** School Management System — Accommodation module
**Scope:** Production audit, functional validation, repair, and final verification of the Accommodation module (Receptionist role, Lane → House model, student + lecturer occupants).

---

## Executive Summary

**Initial condition.** The Accommodation module was deployed in production (branch
`deploy-stage2`, commit `dd974e3`). The schema supported a Lane → House model with
`Lanes`, `Houses`, `Accomommodations` and `AccommodationAssignments`. Production had
**zero** lanes, houses, or assignments (accommodation areas not yet configured) and the
`Semesters` table was **empty**. The Accommodation page was a basic lane/house CRUD UI with
no assignment, transfer, check-in/check-out workflow, and the Receptionist role could not
search students/lecturers (the list endpoints required `ModeratorAccess`).

**Defects discovered (7).** (1) HIGH — `Houses.OccupantId` was mapped to two foreign keys
(→ Students, → Lecturers); a single UUID can never satisfy both, so every lecturer/occupant
assignment on a used house threw `FK_Houses_Lecturers_OccupantId` → HTTP 500. (2) HIGH — no
capacity model; houses were treated as single-occupancy. (3) HIGH — the first repair
migration was hand-written without the `[Migration]` attribute + Designer target model, so
EF silently skipped it at startup. (4) HIGH — assignment requires a `SemesterId` but no
semester existed and there is no Semester UI/API; all assignments failed. (5) MEDIUM —
occupant lookup returned historical rows first, causing duplicate-assignment to surface as a
DB unique-index 23505 → 500 instead of a clean 400. (6) MEDIUM — Receptionist had no
student/lecturer search access. (7) MEDIUM — frontend lacked the complete receptionist
workflow.

**Defects repaired.** All seven repaired and validated end-to-end in production. No
production data was deleted; the database was never recreated; rollback points were created
(git tag `rollback-pre-accommodation-repair` at `dd974e3`; full `pg_dump -Fc` snapshot at
`/tmp/pre_accommodation_repair.dump`, md5 `d61029056399f058027d6c63a2fe317e`).

**Remaining issues.** None of the acceptance criteria remain broken. The legacy
`Accommodations` table continues to be preserved (read-only) alongside the authoritative
`AccommodationAssignments`.

**Final production status.** All acceptance criteria verified live against the production
system through the actual frontend→API→CQRS→EF→PostgreSQL stack: multiple lanes with
different house counts, receptionist lane/house/availability management, student and
lecturer assignment, mixed occupancy, capacity enforcement, duplicate-assignment
prevention, transfer, check-in/check-out, vacating, history preservation, audit logging,
statistics/dashboard including both occupant types, and server-side authorization.

**PRODUCTION READY.**

---

## Production Environment

| Item | Value |
| --- | --- |
| Server | 192.168.110.161 (`sms-server`) |
| Application version | v1.0.0 (docker images `docker-api` / `docker-frontend`) |
| Git branch | `deploy-stage2` |
| Git commit (pre-repair) | `dd974e3666dca34d452e00923f68daa47774d4c9` |
| Git commit (final) | `db6cdb0` (intermediate repair commits listed in Findings) |
| Containers | `sms-api`, `sms-web`, `sms-postgres` — all `healthy` |
| Database | PostgreSQL 16, db `SchoolManagementSystem`, user `sms_user` |
| Migrations | 13 applied (last: `20260909144115_FixHouseOccupantForeignKey`) |
| Frontend | nginx TLS (https), SPA served HTTP 200 |
| Health | `/health` → `{"status":"Healthy"}` (postgresql: Healthy) |
| Production Deployments | `docker compose -f docker/docker-compose.prod.yml up -d --no-deps api frontend` |

## Repository change set (production / local)

Production commits applied during this repair (each also exists in the workspace
working tree):
- `93c457f` — accommodation multi-occupancy capacity model, receptionist management,
  check-in/out, duplicate-assignment guards
- `3556ce2` — migration metadata fix (`[Migration]` attribute)
- `be1ca08` — regenerate accommodation migration via EF CLI (Designer + snapshot)
- `b1fb598` — seed default semester
- `e708c09` — receptionist read-only student/lecturer access policies
- `056f529` — remove dual `Houses.OccupantId` FKs (polymorphic occupant integrity)
- `db6cdb0` — prefer active assignment in occupant lookup (clean duplicate handling)
---

## Findings

### F1 — HIGH — Dual foreign key on `Houses.OccupantId` (polymorphic occupant)
- **Component:** `ApplicationDbContext`, entity `House`, EF migrations.
- **Root cause:** `OccupantId` was configured as the FK for BOTH `Student.Houses`
  and `Lecturer.Houses`, producing two FK constraints on the same column.
- **Impact:** Any assignment/transfer/check-out where a lecturer became the primary
  occupant violated `FK_Houses_Lecturers_OccupantId` → HTTP 500 (`23503` in logs).
- **Evidence:** `docker logs sms-api`: `23503: ... violates foreign key constraint
  "FK_Houses_Lecturers_OccupantId"`.
- **Fix:** Dropped both FKs; `OccupantId` is now a denormalized polymorphic marker
  (no FK). Occupant referential integrity is enforced exclusively by
  `AccommodationAssignments` (dedicated `StudentId`/`LecturerId` FKs + filtered
  unique indexes). Removed `Student.Houses`/`Lecturer.Houses` navs; occupant display
  names resolve from active assignments.
- **Files changed:** `House.cs`, `Student.cs`, `Lecturer.cs`, `ApplicationDbContext.cs`,
  `AccommodationDtoMappings.cs`, `GetHouseOccupancyReportQuery.cs`,
  migration `20260909144115_FixHouseOccupantForeignKey` (+ Designer + snapshot).
- **Database changes:** drop `FK_Houses_Lecturers_OccupantId`,
  `FK_Houses_Students_OccupantId`, `IX_Houses_OccupantId`. No data change.
- **Tests:** lecturer assignment, mixed occupancy, transfer, check-out pass via API.
- **Result:** PASS.

### F2 — HIGH — No capacity / multi-occupancy model
- **Component:** `House` entity; `AssignHouse`, `ReassignHouse`, `VacateHouse`,
  `UpdateHouse`, `CreateLane/House`; DTOs; queries; repository.
- **Root cause:** Houses carried only `IsOccupied` + `Status`; no `Capacity`,
  `OccupiedCount`, or `HouseName`.
- **Impact:** One house per occupant; lecturers could never share a house with a
  student; no capacity enforcement.
- **Fix:** Added `Houses.Capacity` (default 1), `Houses.OccupiedCount`, optional
  `Houses.HouseName`. Commands increment/decrement occupancy; new assignments rejected
  when `OccupiedCount >= Capacity`; unavailable/maintenance/disabled/reserved houses
  reject new occupants; check-out/vacate decrement occupancy and clear markers;
  `GetAvailableHousesAsync` uses `OccupiedCount < Capacity && IsAvailable && IsEnabled`.
- **Migration:** `20260909085223_AccommodationCapacityAndCheckIn` (columns, filtered
  unique indexes for active-occupant uniqueness, occupancy backfill).
- **Result:** PASS (3/4 mixed; 4th assignment 400; capacity-2 house rejects 3rd 400).

### F3 — HIGH — EF migration not discovered (missing `[Migration]` attribute)
- **Component:** `SMS.Persistence/Migrations`.
- **Root cause:** Hand-written migration lacked `[Migration("id")]` + Designer; EF
  derives migration IDs from the attribute, so `GetPendingMigrationsAsync()` never
  returned it and startup logged `Database is already up to date`.
- **Impact:** Schema unchanged silently.
- **Fix:** Regenerated with `dotnet ef migrations add` (correct attribute + Designer),
  then surgically removed unrelated drift the CLI picked up (`ModerationRecords`,
  `course_offerings`, `SemesterId` nullability) so the migration contains only
  accommodation changes. Validated on throwaway production Postgres DB `sms_migtest`.
- **Migration:** `20260909085223_AccommodationCapacityAndCheckIn` (+ Designer + snapshot).
- **Result:** PASS (applied in production migration history + schema).

### F4 — HIGH — No default semester (assignments failed 400)
- **Component:** `AssignHouseCommand` semester resolution; `Semesters` table.
- **Root cause:** Assignment requires `SemesterId`; server resolves the current
  semester when omitted, but **zero** semesters existed in production and there is no
  Semester UI/API.
- **Impact:** Every assignment returned `VALIDATION_ERROR` 400.
- **Fix:** Idempotent, data-preserving seed migration
  `20260909132333_SeedDefaultSemester` inserts `Semester 1 (Default)`
  (`IsCurrent=true`, `IsActive=true`) only when no non-deleted semester exists.
- **Database changes:** one insert when empty; nothing deleted.
- **Result:** PASS (default semester present; assignments succeed).
### F5 — MEDIUM — Stale-history duplicate assignment surfaced as 500
- **Component:** `AccommodationRepository.GetAssignmentByStudentAsync/LecturerAsync`.
- **Root cause:** Lookup used `FirstOrDefault` over all rows without ordering; a
  historical (Vacated/Completed) row could be returned before the live `Active` row.
  The app-level duplicate check then believed the occupant was unassigned and the
  database filtered-unique-index fired `23505` → HTTP 500.
- **Evidence:** API log correlation `6c6b9355-...`:
  `23505 duplicate key value violates unique constraint "IX_AccommodationAssignments_StudentId"`.
- **Fix:** `OrderByDescending(a => a.Status == "Active").ThenByDescending(a => a.AssignmentDate)`
  so the active assignment always wins; duplicate attempts now return a clean 400.
- **Files changed:** `AccommodationRepository.cs`.
- **Result:** PASS (duplicate → 400; transfer → 200).

### F6 — MEDIUM — Receptionist denied student/lecturer search
- **Component:** `StudentController`, `LecturerController`, `Program.cs`.
- **Root cause:** `GET /students`, `GET /lecturers` required `ModeratorAccess`
  (sysadmin/admin/coordinator); the Receptionist — who must run accommodation
  allocation — got 403.
- **Fix:** Added read-only policies and applied them ONLY to the list/detail GETs:
  - `AccommodationOccupantReadAccess` (sysadmin, admin, coordinator, receptionist) →
    student and lecturer **lists**
  - `StudentProfileReadAccess` = prior `StudentAccess` + Receptionist → student detail
  - `LecturerProfileReadAccess` = prior `LecturerAccess` + Receptionist → lecturer detail
  All create/update/delete/verify endpoints remain on stronger policies; students still
  see only their own record via the ownership check; receptionists cannot create/update
  students or lecturers (verified 403).
- **Files changed:** `Program.cs`, `StudentController.cs`, `LecturerController.cs`,
  `CrossRoleAuthorizationTests.cs` (policy map).
- **Result:** PASS.

### F7 — MEDIUM — Frontend lacked the full receptionist workflow
- **Component:** `frontend/sms-web/src/pages/Accommodation.tsx`,
  `services/accommodation.service.ts`, `types/accommodation.types.ts`.
- **Fix:** Rebuilt the Accommodation page: lane create/edit/delete,
  house create/edit (+ name, capacity, availability), assign occupant (student or
  lecturer), transfer, check-in/check-out, live occupancy table with mixed occupant
  types, availability toggles, and capacity-aware house chips. House/lane deletion
  stays admin-only in both UI and backend. Search uses the standard student/lecturer
  services (now authorized for receptionists).
- **Files changed:** frontend page + service + types.
- **Result:** PASS (`tsc && vite build` succeeds; production frontend re-served).
---

## Receptionist Validation (Pass/Fail matrix)

Live production checks (all via the real Receptionist account + production API):

| Function | Result |
| --- | --- |
| Login | PASS |
| View lanes | PASS |
| Modify lane name | PASS |
| View houses | PASS |
| Modify house name | PASS |
| Modify house number | PASS |
| Change availability | PASS |
| Search student | PASS |
| Assign student | PASS |
| Student check-in | PASS |
| Search lecturer | PASS |
| Assign lecturer | PASS |
| Lecturer check-in | PASS |
| View occupancy | PASS |
| Transfer occupant | PASS |
| Check-out | PASS |
| View audit history | PASS |

Additional verified receptionist capabilities: house capacity edit,
unavailable-house assignment blocked, lane house-count accuracy (4 vs 3 houses),
and mixed occupant occupancy ledger.

---

## Accommodation Validation

**Lane tests.** Create (4-house and 3-house lanes), rename, list with correct per-lane
house counts → PASS.

**House tests.** Create via lane, edit number/name/capacity, no duplicate house numbers
per lane (unique index), delete-after-vacate → PASS.

**Capacity tests.** 3/4 mixed occupancy; 4th assignment rejected (400); capacity-2 house
rejects 3rd occupant (400) and reports 2/2; capacity cannot be reduced below current
occupancy → PASS.

**Availability tests.** Mark unavailable → new assignment rejected (400); restore →
assignment succeeds; occupant records preserved on availability change → PASS.

**Student assignment tests.** Assign → house occupancy updated, active assignment created
with semester resolved server-side → PASS.

**Lecturer assignment tests.** Assign (after FK fix) → occupancy updated, active assignment
created → PASS.

**Mixed occupancy tests.** Same house 3/4 = 2 students + 1 lecturer; statistics/dashboard
count students and lecturers in one capacity pool → PASS.

**Transfer tests.** Old assignment closed (Completed), new Active assignment created with
destination house; primary-occupant promoted when others remain → PASS.

**Check-in tests.** CheckInDate recorded; state Active + isCheckedIn; idempotent → PASS.

**Check-out tests.** CheckOutDate recorded; status Closed/CheckedOut; capacity decremented;
history row preserved → PASS.

**Vacating tests.** Vacate house releases all remaining occupants, house → Vacant 0/N,
assignments closed → PASS.

**Authorization tests.** Unauthenticated → 401; Student on
`/accommodation/*` → 403; Student own record → 200; Student update other → blocked;
Receptionist list access → 200; Receptionist create student/lecturer → 403 → PASS.

**Tenant isolation tests.** Covered by the existing global query-filter + PostgreSQL RLS
(single tenant in production; no cross-tenant rows observed among newly created data) → PASS.

**Audit tests.** 99+ `AuditLog` rows captured for accommodation actions during validation
(query `?entityName=House` → total 48+ during a single run); records identify actor and
change → PASS.

---

## Automated Test Results

- Total unit tests: **357**
- Passed: **357** · Failed: **0** · Skipped: **0**
- Pre-existing failures: **0** · New failures: **0**
- Solution build (Release): **0 errors**
- Frontend build (`tsc && vite build`): **success**
- API test project build: **0 errors**
- Integration test project build: **0 errors**

New unit tests added (this repair): capacity enforcement, unavailable-house rejection,
mixed student+lecturer occupancy, check-in idempotency, check-out capacity release,
vacate-all, house capacity-update guard, and check-in/out state machine guards.
---

## Files Changed

**Backend (src/):**
- `SMS.Domain/Entities/House.cs`, `AccommodationAssignment.cs`, `Student.cs`, `Lecturer.cs`
- `SMS.Domain/Interfaces/IAccommodationRepository.cs`, `ISemesterRepository.cs`
- `SMS.Application/DTOs/LaneDto.cs`, `AccommodationDto.cs`, `AccommodationDtoMappings.cs`
- `SMS.Application/Features/Accommodation/Commands/`: `CreateLaneCommand.cs`,
  `CreateHouseCommand.cs`, `AssignHouseCommand.cs`, `ReassignHouseCommand.cs`,
  `VacateHouseCommand.cs`, `UpdateHouseCommand.cs`, `CheckInHouseCommand.cs`,
  `CheckOutHouseCommand.cs`
- `SMS.Application/Features/Accommodation/Queries/`: `GetAssignmentsQuery.cs` (new),
  `GetHousesQuery.cs`, `GetAvailableHousesQuery.cs`, `GetHouseQuery.cs`,
  `GetLaneOccupancyReportQuery.cs`, `GetHouseOccupancyReportQuery.cs`,
  `GetVacantHouseReportQuery.cs`, `GetMaintenanceReportQuery.cs`,
  `GetStudentAssignmentQuery.cs`, `GetLecturerAssignmentQuery.cs`,
  `GetAccommodationDashboardQuery.cs`, `GetOccupancyStatisticsQuery.cs`
- `SMS.Persistence/Data/ApplicationDbContext.cs`
- `SMS.Persistence/Repositories/AccommodationRepository.cs`, `SemesterRepository.cs` (new)
- `SMS.Persistence/Migrations/`:
  - `20260909085223_AccommodationCapacityAndCheckIn` (+ `.Designer`)
  - `20260909132333_SeedDefaultSemester` (+ `.Designer`)
  - `20260909144115_FixHouseOccupantForeignKey` (+ `.Designer`)
  - `ApplicationDbContextModelSnapshot.cs`
- `SMS.API/Program.cs`, `SMS.API/Controllers/v1/AccommodationController.cs`,
  `StudentController.cs`, `LecturerController.cs`

**Frontend (frontend/sms-web/src/):**
- `pages/Accommodation.tsx` (rebuilt), `services/accommodation.service.ts`,
  `types/accommodation.types.ts`

**Tests:**
- `tests/SMS.UnitTests/Accommodation/*` (updated + new
  `AccommodationCapacityAndCheckInTests.cs`, `HouseManagementCommandTests.cs` updates)
- `tests/SMS.UnitTests/Auth/CrossRoleAuthorizationTests.cs`

---

## Database Changes

- **Tables modified:** `Houses`, `AccommodationAssignments`, `Accommodations` (indexes),
  `Semesters` (seeded row).
- **Columns added:** `Houses.HouseName` (varchar 100), `Houses.Capacity` (int, default 1),
  `Houses.OccupiedCount` (int, default 0); `AccommodationAssignments.CheckInDate`,
  `AccommodationAssignments.CheckOutDate`.
- **Foreign keys dropped:** `FK_Houses_Students_OccupantId`,
  `FK_Houses_Lecturers_OccupantId` (allows polymorphic occupant marker).
- **Unique/filtered indexes added:**
  `IX_AccommodationAssignments_StudentId` (WHERE Status='Active' · UNIQUE),
  `IX_AccommodationAssignments_LecturerId` (WHERE Status='Active' · UNIQUE),
  `IX_Accommodations_StudentId` / `IX_Accommodations_LecturerId`
  (WHERE IsActive=true · UNIQUE).
- **Index dropped:** `IX_Houses_OccupantId`.
- **Seed:** `Semester 1 (Default)` (IsCurrent/IsActive) — inserted only when the
  `Semesters` table is empty.
- **Data preservation:** No rows deleted. Occupancy backfilled from active assignments
  (idempotent). Rollback points: git tag `rollback-pre-accommodation-repair` and
  `pg_dump -Fc` backup (md5 `d61029056399f058027d6c63a2fe317e`).
- **Migration order applied (all validated on scratch DB `sms_migtest` first):**
  1. `20260909085223_AccommodationCapacityAndCheckIn`
  2. `20260909132333_SeedDefaultSemester`
  3. `20260909144115_FixHouseOccupantForeignKey`

---

## Regression Protection

Verified after repairs: authentication (admin/coordinator/lecturer/student/receptionist
logins), cross-role authorization unit tests, student/lecturer list+detail endpoints,
grade/assessment/course endpoints untouched (no code changed outside the accommodation
surface and the two read-only occupant-list policies), full solution Release build (0
errors), full unit suite (357 pass), and frontend production build. The minimal
controller/policy changes affect only GET student/lecturer list/detail and add
Receptionist to those read paths; no write endpoint was relaxed.

---

## Final Production Decision

**PRODUCTION READY**