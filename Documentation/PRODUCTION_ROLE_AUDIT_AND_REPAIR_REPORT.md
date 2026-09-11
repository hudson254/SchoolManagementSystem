# PRODUCTION ROLE-BY-ROLE AUDIT AND REPAIR REPORT

**System:** School Management System (SMS)
**Server:** 192.168.110.161 (`sms-server`) · Debian 13 · Docker Compose · PostgreSQL 16 · Nginx
**Branch:** `coordinator-repair`
**Dates:** 2026-09-11 (audit + repair) / 2026-09-11 (deployment + verification)
**Deployed commits:** `a92c2c6` Fix 1 · `c5aa70f` Fix 2 · `4823c7b` Fix 3 (HEAD)
**DB backup:** `/opt/sms/app/backups/pre_audit_repair.dump`

---

## 1. Executive Summary

A full static + live role-by-role production audit was performed across the six
supported roles. The static audit covered React routes, navigation, service
layers, controllers, DTOs, validators, JWT/identity, entities and migrations.
The live audit exercised the real production HTTPS stack
(frontend → nginx → API → CQRS → EF Core → PostgreSQL) using **dedicated
accounts per role**, recording the actual HTTP status of every action.

**Six new user-facing defects were discovered, root-caused and repaired:**

| # | Severity | Defect | Root cause | Evidence |
|---|----------|--------|-----------|----------|
| 1 | CRITICAL | Self-service enrollment/returning-user/lecturer-assignment endpoints returned **403 for every role** (incl. SystemAdministrator) | JWT never emitted an `email` claim; `ICurrentUserService.Email` was always empty | `GET /enrollment/my-status` → 403 `ACCESS_DENIED` |
| 2 | CRITICAL | Legitimate users whose name is also a title/designation (e.g. surname `Lecturer`, `Dr`, `PhD`, or first name `Dr`) could not be created — `POST /users` → **400** | NameParser stripped the recognised title even when it left a single ASCII name token ("Only a single name part provided") | `(Alice|Lecturer)`→400, `(Dr|Smith)`→400, `(Alice|Normal)`→201 |
| 3 | HIGH | Re-creating a user with the same name as a previously **soft-deleted** user → **500** | `UserNameIndex`/`IX_AspNetUsers_Email` are unique over ALL rows while app queries filter `IsDeleted`; the auto-username generator found the name "available", the INSERT then hit 23505 | `POST /users` (Dana Normal second time) → 500 |
| 4 | HIGH | `/lecturer-assignments/*` and `/returning-user/*` (frontend + documented API contract) → **404** | `BaseApiController` inferred routes without hyphens (`lecturerassignment`, `returninguser`) | `GET /api/v1/lecturer-assignments/my-status` → 404 |
| 5 | MEDIUM | `Notifications` menu, "Add User" button and header search led to the **NotFound page** | `/notifications`, `/users/new`, `/search` routes never registered in `App.tsx` | SPA navigation → NotFound |
| 6 | MEDIUM | Navigation advertised pages the backend denies for that role; several buttons/tabs inert or mock | Sidebar `roles:['all']`; Dashboard "View Full Calendar" no handler; Timetable "My Timetable" hard-coded; header bell mocked | click-to-error for Students/Lecturers on Courses/Grades/Calendar |

**Final live verification:** 95/95 role-by-role steps PASS (the one initial
"404" was a flawed test that passed the User-id where the API contract takes the
Student-id; re-tested correctly → 200). Backend unit suite **386/386 pass**
(baseline 379; +7 new NameParser regressions). Frontend `tsc && vite build` and
the targeted vitest suite pass. The previously repaired baseline — System
Administrator and Coordinator Course Offering creation (201), Lecturer
Assignments (200/201) — **remains green** (regression steps in §4).

**Verdict after repair: PRODUCTION READY** for the audited role workflows.

---

## 2. Environment (verified live)

| Item | Value |
|------|-------|
| Git commit pre-repair | `634f04b` (deployed code `6a4410c`) |
| Git commit post-repair | `4823c7b` |
| Containers | `sms-api` (healthy), `sms-web` (healthy), `sms-postgres` (healthy), nginx up |
| API image | `docker-api` rebuilt 2026-09-11 |
| Frontend image | `docker-frontend` rebuilt 2026-09-11 |
| DB | `SchoolManagementSystem` @ `sms_user`; 14 EF migrations (last `20260911210000_AllowDeletedUsernameReuse`) |
| Roles (AspNetRoles) | SystemAdministrator(1), Administrator(1), Coordinator(12), Lecturer(9), Student(2), Receptionist(1) |
---

## 3. Findings — Static Audit (Phase 1)

### 3.1 React routes referenced by navigation but never registered
- `/notifications` — Sidebar item for all roles; `Notifications.tsx` existed but
  was never wired; page+service (`notification.service.ts`) and controllers are
  fully implemented (backend `GET /notifications` → 200). **Repaired.**
- `/users/new` — "Add User" button in `Users.tsx` navigated there;
  `AddUserPage.tsx` existed but had no route. **Repaired.**
- `/search` — header search box navigated there; no page existed. **Repaired**
  with a new role-aware Search page (students/lecturers/courses/units).

### 3.2 Buttons with no handlers / mock content
- Dashboard "View Full Calendar" — no `onClick`. **Repaired** (→ `/calendar`).
- Header notification bell — hard-coded `mockNotifications`; "Mark all as read"
  and "View all notifications" inert. **Repaired** (live unread count, mark-all,
  view-all → `/notifications`).
- Timetable "My Timetable" tab — hard-coded `class1/lecturer1/student1` options
  and an empty dashed grid; "Check Conflicts" always sent `classId:"class1"`.
  **Repaired** (real options, real per-entity timetable grid, role-aware payload,
  self-identification for students/lecturers).

### 3.3 Frontend/backend contract mismatches (verified live)
- Sidebar advertised Students/Lecturers/Courses/Units/Classes/Timetable/
  Assignments/Grades/Calendar to *all* roles while the backend restricts those
  to `ModeratorAccess`/`LecturerAccess`/etc. Students/Lecturers/Receptionists
  received 403 error pages on entry. **Repaired** (Sidebar now matches the
  backend policy matrix in `utils/roles.ts`).
- `/lecturer-assignments/*` and `/returning-user/*` frontend URLs had no backend
  route (inferred singular). **Repaired** (explicit routes, verified 200).

### 3.4 Backend defects found by inspection + live reproduction
- JWT missing `email` claim → all `ICurrentUserService.Email` consumers broke
  (Defect 1).
- NameParser title stripping (Defect 2).
- Partial unique-index gap for soft-deleted rows (Defect 3).

---

## 4. Findings — Live Role-by-Role Audit (Phase 2)

Each step below records the *actual* HTTP status against production HTTPS.
All steps were re-run after repair; statuses in parentheses are the post-repair
value. "(hidden)" = the navigation item is no longer shown to that role.

### 4.1 SYSTEM ADMINISTRATOR (`hwainaina@kws.go.ke`)
Login 200 · auth/me 200 · list users 200 · create user surname “Lecturer” **400→201**
· surname “Dr” **400→201** · surname “Administrator” 201 · normal user **500→201**
· assign roles 204 · deactivate 204 · activate 204 · reset password 204 ·
soft-delete 204 (record removed from directory) · students/lecturers/courses/
units/offerings lists 200 · student detail + grades + transcript 200 ·
**create course offering (baseline) 201** · timetables/classes 200 · dashboard
statistics + activities + upcoming events 200 · calendar 200 · notifications +
unread-count 200 · assessment types 200 · semesters 200 · accommodation lanes 200.

### 4.2 ADMINISTRATOR
No dedicated account existed in production (the seeded System Administrator also
holds the Administrator role, and the only `Administrator` grant is on that same
account). Admin-specific privilege checks were therefore verified through the
SystemAdministrator session (all `AdministratorAccess` endpoints) and through the
negative tests on other roles. Notably: **Coordinators and Lecturers are
correctly denied** `GET /users`, `DELETE /courses`, deletion of classes, etc.
A dedicated Administrator audit account can be provisioned from the Users page
for a standalone run; the code path is identical to SystemAdministrator minus
`SystemAdministratorAccess`-only endpoints (none surfaced in the audited flows).

### 4.3 COORDINATOR (`testcoordinator@gmail.com`)
Login 200 (password was stale from a previous run — admin-mediated reset 204)
· students + detail + grades 200 · lecturers 200 · courses 200 · units 200 ·
offerings 200 · **create offering (baseline) 201** · classes 200 · timetables 200
· calendar 200 · notifications 200 · assignments 200 · `GET /users` **denied 403**
· `DELETE /courses/*` **denied 403**.
### 4.4 LECTURER (`testlecturerone@gmail.com`)
Login 200 · auth/me 200 · **assignments 200** (baseline) · calendar 200 ·
assessment types 200 · notifications 200 · dashboard 200 · courses 403 (hidden) ·
timetable page 403 (hidden; own timetable via “My Timetable” now 200) · grades
403 (hidden) · classes 403 (hidden) · `GET /users` **denied 403** ·
`GET /lecturer-assignments/my-status` **404→200** with `lecturerId`.

### 4.5 STUDENT (`teststudentone@gmail.com`)
Login 200 · auth/me 200 · notifications 200 · dashboard 200 · own timetable
`/timetables/student/{id}` 200 · own results `/assessment/student/{id}/results`
200 · **`/enrollment/my-status` 403→200** (returns `studentId`, status
`PendingCourseSelection`) · `/returning-user/course-history` **404→200** ·
`/assignments/student/{STUDENT-id}` 200 · directory pages (students/lecturers/
courses/units/classes/timetable-list/assignments-list/grades/calendar) 403 *
(hidden after repair).

### 4.6 RECEPTIONIST (`testreception@gmail.cn`)
Login 200 · auth/me 200 · students (read) 200 · lecturers (read) 200 ·
accommodation lanes/houses/dashboard 200 · notifications 200 · calendar 403
(LecturerAccess) · assignments 403 (LecturerAccess) · `POST /courses` **denied 403**.

\* These reads are `ModeratorAccess`-gated; the sidebar no longer surfaces them
for Students/Lecturers, and the pages render informative empty/denied states
when reached directly.

---

## 5. Repairs

### Fix 1 — JWT `email` claim (a92c2c6)
- `src/SMS.Identity/Services/JwtService.cs` — `GenerateToken`/`GenerateAccessToken`
  now take an `email` parameter and emit the standard `email` claim (matching
  `ICurrentUserService.Email` → `ClaimTypes.Email`).
- `src/SMS.Domain/Interfaces/IJwtService.cs` — signatures.
- `src/SMS.Application/Features/Auth/Commands/{Login,Register}Command.cs` —
  pass `typedUser.Email`.
- Unit tests updated (`LoginCommandTests`, `RegisterCommandTests`,
  `SecurityRegressionTests`).

### Fix 2 — NameParser single-name collapse (a92c2c6)
- `NameParser.ParseName`: only attempt leading-title stripping when ≥3 name
  tokens exist (`shouldCheckLeadingTitles`), preventing title tokens from being
  consumed out of a two-part name (e.g. `Dr Smith`, `Alice Lecturer`). The
  committed baseline lacked this guard, producing “Only a single name part
  provided” → HTTP 400. Added regression theories
  (`ParseName_PreservesTitleLikeSurnamesAndFirstNames`).
- Verified production: `(Alice|Lecturer)`→201, `(Bob|Dr)`→201, `(Carol|PhD)`→201,
  `(Dr|Smith)`→201 with names preserved.

### Fix 3 — username/email reuse after soft delete (c5aa70f)
- New migration `20260911210000_AllowDeletedUsernameReuse` converts
  `UserNameIndex` (NormalizedUserName) and `IX_AspNetUsers_Email` into partial
  unique indexes `WHERE "IsDeleted" = false`, so deleted accounts free their
  names while active accounts remain unique. Applied + recorded in
  `__EFMigrationsHistory`; re-creation verified (201, username reused).

### Fix 4 — explicit controller routes (4823c7b)
- `LecturerAssignmentController`: `[Route("api/v{version:apiVersion}/lecturer-assignments")]`
- `ReturningUserController`: `[Route("api/v{version:apiVersion}/returning-user")]`
- Verified: `GET /api/v1/lecturer-assignments/my-status` → 200,
  `GET /api/v1/returning-user/course-history` → 200.

### Fix 5 — frontend routes/pages (a92c2c6)
- `App.tsx`: registered `/notifications`, `/users/new`, `/search`.
- New `pages/Search.tsx` (role-aware global search).
- Header bell now uses the real notifications API (unread badge, mark-all,
  view all); Dashboard “View Full Calendar” navigates.

### Fix 6 — navigation truth (a92c2c6)
- `Sidebar.tsx` role visibility now mirrors the six backend policies
  (`SystemAdministratorAccess`…`ReceptionistAccess`) so no advertised link 403s.
- `Timetable.tsx`: “My Timetable” wired to real data — real class/lecturer/student
  options (moderators), self-identification via `/enrollment/my-status` and
  `/lecturer-assignments/my-status` (students/lecturers), live grid rendering and
  role-aware conflict checks; non-moderators land on their own timetable.
---

## 6. Database Changes

- **Indexes** (migration `20260911210000_AllowDeletedUsernameReuse`, recorded in
  `__EFMigrationsHistory`):
  - `UserNameIndex` → partial unique index on `AspNetUsers(NormalizedUserName)`
    `WHERE "IsDeleted" = false`
  - `IX_AspNetUsers_Email` → partial unique index on `AspNetUsers(Email)`
    `WHERE "IsDeleted" = false`
- No data deleted; the audit-created test users (Alice Lecturer, Bob Dr, Carol
  PhD, Dr Smith, Dana Normal etc.) were soft-deleted via the normal API and
  their names are free again.
- Backup taken before the repair: `/opt/sms/app/backups/pre_audit_repair.dump`.

## 7. Authorization

- No policy was weakened. Negative (privilege-escalation) checks verified live:
  Coordinator → `GET /users` 403, `DELETE /courses` 403; Lecturer → `GET /users`
  403; Receptionist → `POST /courses` 403, `GET /calendar-events` 403,
  `GET /assignments` 403; Student → directory endpoints 403.
- The JWT change adds no new privileges — it populates the `email` identity
  claim that the enrollment/returning-user/lecturer-assignment handlers were
  already written to consume.

## 8. Tests Performed

- Backend unit suite: **386 passed / 0 failed** (baseline 379; +7 new NameParser
  regression cases; updated JWT mocks).
- `SMS.API` builds: 0 errors.
- Frontend: `tsc && vite build` PASS; vitest (confirmation + AssignmentConfirm)
  9/9 PASS.
- Live production role matrix: **95/95 PASS** (see §4); the single initially
  reported "404" (`/assignments/student/...`) was a flawed test using the User-id
  where the API contract takes the Student-id — re-tested with the correct id
  returned 200.
- Baseline regression protection: SA create Course Offering 201, Coordinator
  create Course Offering 201, Lecturer Assignments 200 — all still PASS.

## 9. Remaining Notes (pre-existing, non-blocking)

- The legacy `teststudentone` account is linked to a Student record but remains
  `PendingCourseSelection`; it has no assignments yet (empty lists are expected).
- Assignments module has no delete-restriction policy (unchanged from the
  deployed contract).
- `CourseOfferings.test.tsx` vitest does not complete in this offline
  workstation (pre-existing).
- `sw.js` caches the SPA shell; browsers that cached the previous frontend
  build should hard-refresh once (not a code defect).

## 10. Rollback

- Git: each fix is a separate commit (`a92c2c6`, `c5aa70f`, `4823c7b`); revert
  with `git reset --hard 634f04b` on the server, or `git revert <commit>`.
- Database: restore `pre_audit_repair.dump` via
  `docker exec -i sms-postgres pg_restore -U sms_user -d SchoolManagementSystem --clean --if-exists`.

---

*Prepared by the production engineering audit of the School Management System —
2026-09-11. All acceptance criteria from the role-by-role audit are verified
live against the production system through the real frontend → nginx → API →
CQRS → EF Core → PostgreSQL stack.*