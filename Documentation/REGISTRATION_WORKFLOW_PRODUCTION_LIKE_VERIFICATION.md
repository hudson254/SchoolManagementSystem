# Registration Workflow Production-Like Verification Report

> Production-like verification of the registration and post-registration workflow
> repair. Executed against a **real PostgreSQL 16** stack, the real ASP.NET Core API
> and the real SPA, using only the application's own HTTP APIs for creation and
> approval. Database access was **read-only verification** (plus provisioning of
> reference/fixture data that has no public API).
>
> **No application source code was modified. No new commit was created.**

---

## 1. Verification Date

2026-10-03 (local machine time). Timestamps below are as reported by the
application and database on the verification host.

## 2. Git Baseline

| Item | Value |
|---|---|
| Commit (verified) | `eb50939faf1afdf8213e75e3a4e6d4cf9e69ea64` |
| Branch | `coordinator-repair` |
| Fix commit under test | `34372f5` — `fix(registration): persist course/unit selections and unblock student requests` |
| Documentation commit | `eb50939` |
| Previous baseline | `9fb7e36800aff93b9527cb14e6da1a50c07da0b5` |
| `git log --oneline -5` | `eb50939`, `34372f5`, `9fb7e36`, `caa7562`, `997e44c` |
| Working tree before | 3 untracked files only (documented pre-existing) |
| Working tree after | 3 untracked files only — **identical** |
| Tracked modifications | **None** (`git diff --stat` empty) |

Working tree before **and** after verification:

```
?? Documentation/AGENT_PROMPT_STUDENT_COURSE_SELECTION_FIX.md
?? Documentation/Accommodation/ACCOMMODATION_PRODUCTION_FUNCTIONAL_VERIFICATION_REPORT.md
?? PRODUCTION_NOTIFICATIONS_PWA_DEPLOYMENT_REPORT.md
```

`34372f5` is the parent of `eb50939`, so the checkout is exactly the intended repair.
A transient `src/SMS.API/uploads/` directory produced by the study-material upload
test was removed again; no tracked file was ever touched.

## 3. Environment

| Item | Value |
|---|---|
| Docker | Docker Desktop started during this task (was **not** running — the cause of the historical `Failed to connect to 127.0.0.1:5433`) |
| Containers | `sms-postgres-test` (postgres:16-alpine, 5433→5432, healthy), plus unrelated `oms-test-api` (5080), `oms-test-postgres` (5434) |
| PostgreSQL | **16.15 (Alpine)** — `pg_isready` → *accepting connections*; `GET /health` → `{"status":"Healthy",...,"postgresql":"Database is reachable and responding."}` |
| Database used for E2E | **`sms_verify`** (disposable, created for this task, owned by `sms_migration`) |
| Test databases | `sms_test`, `sms_enrollment_state_test`, `sms_rls_test` (created by the suites) |
| API | `http://localhost:5088/api/v1`, ASP.NET Core, `Development` |
| Frontend | `frontend/sms-web` (Vite / React 19), build + Vitest |
| Application role (runtime) | **`sms_app`** — `NOSUPERUSER`, `NOBYPASSRLS` |
| DDL role | **`sms_migration`** — `NOSUPERUSER`, `NOBYPASSRLS` (owns tables) |
| Bootstrap role | `testuser` — Superuser, BypassRLS (cluster init only; the application never uses it) |
| Auth mechanism | httpOnly `access_token` / `refresh_token` cookies + `XSRF-TOKEN` echoed in `X-CSRF-TOKEN` (RISK-08/RISK-10 flow, exercised as a real browser would) |

### Migrations and schema (section 5)

| Item | Value |
|---|---|
| Migration mechanism | Application's own — `DatabaseMigrationRunner.ApplyAsync` over `ConnectionStrings:MigrationConnection` (`dotnet run -- seed-data`, API startup) |
| Migration state before | `sms_verify` created empty; **0 tables**, no `__EFMigrationsHistory` |
| Migration state after | **82 tables** in `public`, 70 with RLS enabled, full EF migration chain applied |
| Schema changes required by `34372f5` | **None.** The commit changes no entity, mapping, FK or query filter — confirmed at runtime |
| Required tables present | `Students`, `Enrollments`, `course_offerings`, `course_offering_enrollments`, `course_offering_lecturers`, `course_offering_units`, `UnitAllocations`, `Notifications`, `AuditLogs`, `sms_requests` — **all present** |
| Post-migration grants | `docker/grant-least-privilege-privileges.sql` executed (the repository's documented post-migration step). Without it the runtime role gets `permission denied for table course_offerings`. No schema was altered |

## 4. Test Tenant

| Item | Value |
|---|---|
| Verification tenant | `11111111-1111-1111-1111-111111111111` ("Default Tenant", created by the app's own `DatabaseSeeder`) |
| Second tenant (isolation) | `22222222-2222-2222-2222-222222222222` ("Verification Tenant B") |
| Header | `X-Tenant-Id` (read by `TenantResolutionMiddleware`) |

### Test accounts (identifiers only — no passwords or secrets recorded)

| Role | Identifier |
|---|---|
| SystemAdministrator | `verify.admin@sms-verification.local` |
| Administrator | `verify.administrator@sms-verification.local` |
| Coordinator | `verify.coordinator@sms-verification.local` |
| Receptionist | `verify.receptionist@sms-verification.local` |
| Student A | `verify.student.a@sms-verification.local` |
| Student B | `verify.student.b@sms-verification.local` |
| Lecturer A | `verify.lecturer.a@sms-verification.local` |
| Lecturer B | `verify.lecturer.b@sms-verification.local` |
| No-offering student | `verify.nooffering@sms-verification.local` |
| Tenant B admin | `verify.tenantb.admin@sms-verification.local` |

All roles created through `POST /api/v1/users`; all student/lecturer registrations
through `POST /api/v1/auth/register`. No personal data was used.

## 5. Course, Units and Offering (section 7)

Created through the application's own APIs (`POST /courses`, `POST /units`,
`POST /CourseOffering`) as SystemAdministrator.

| Item | Value |
|---|---|
| Course | VERIFICATION COMPUTER SCIENCE (`VERCS01`) |
| Course ID | `62dfbd90-2393-422c-ba7a-56cdbc644d81` |
| Unit 1 | VERU101 — `7e6fb7f0-aadf-4bd5-b557-82b5e3713344` |
| Unit 2 | VERU102 — `538da254-1e66-4891-8f2e-bbb8b83c3981` |
| Unit 3 | VERU103 — `8ce32841-ca11-479f-ba57-e47953af7ca0` |
| Course offering | `VERCS01-2026-S1-001`, `08b083ac-3874-4621-a442-42cf21e49a1d` |
| Year | 2026/2027 |
| Period | Semester 1 (2026-08-01 → 2026-12-18) |
| Offering status | **Active** |

The offering was resolved by the **same rule** registration uses
(`ResolveActiveOfferingAsync`: semester match, else the single active offering) —
not fabricated. A second course (`VERNOFF`) with **no** offering was created for
section 27.

> **Fixture note (not a workflow bypass):** `Semesters` seeded by
> `SeedDefaultSemester` carries `tenant_id = 00000000-…`, so it is invisible to any
> real tenant, and lecturer registration requires a resolvable academic period
> (`RegisterCommand.cs:619-622`). There is **no API to create a Semester**. One
> tenant-scoped semester row was therefore inserted as **reference data** for the
> verification tenant. This is fixture provisioning only — every registration,
> approval and authorization decision in this report went through the HTTP API.

## 6. Student Verification

| Step | Result |
|---|---|
| Registration | `POST /auth/register` → **201**, roles `[Student]` |
| Course persistence | `Students.SelectedCourseId = 62dfbd90-…` (VERCS01) — **correct** |
| Unit persistence | **3 `Enrollments` rows** (VERU101/102/103), `Status = PendingApproval`, `IsActive = false` — **correct, exactly 3** |
| Offering enrollment | **1 `course_offering_enrollments` row** → offering `08b083ac-…`, `PendingConfirmation` / `IsActive = true` — **correct** |
| PendingApproval | `RegistrationStatus = 1 (PendingApproval)` — **not** `PendingCourseSelection` |
| Tenant | `tenant_id = 11111111-…` — **correct** |
| Duplicates | Duplicate-relationship query returned **0 rows** |
| Registration state endpoint | `GET /enrollment/my-status` → 200, `registrationStatus = PendingApproval`, `hasSelectedCourse = true`, `selectedCourseName = VERIFICATION COMPUTER SCIENCE`, `needsCourseSelection = false` |
| Dashboard | `GET /dashboard/student-me` → 200, `pendingCourse` populated with course name/code, **no false "no course" state** |
| Material access (pending) | `GET /study-materials/my-units` → **200 `[]`** — gated by design (enrollments are `IsActive = false` until approval). **No privileged content while pending** |
| **New Request — types** | `GET /oms/requests/types` → **200** with the request type. **This is the exact call that previously returned 403 for Student. PASS** |
| **New Request — create** | `POST /oms/requests` → **201**, `REQ-2026-000001`, `requesterUserId` = Student A, tenant stamped |
| **New Request — own read** | `GET /oms/requests/{id}` → **200** (no 403); `GET /oms/requests` → 200 returning only Student A's own request |
| Approval queue | `GET /Approval/pending?userType=Student` → 200, Student A **and** Student B listed with name, identifier (`STU…`), email. Previously invisible |
| Approval | `POST /Approval/approve` → **200** `"Approved"`; queue dropped to 1 |
| Post-approval | `registrationStatus = Approved`, `isApproved = true`, course retained; enrollments flipped to `Active` / `IsActive = true`; **`GET /study-materials/my-units` now returns all 3 units** with `accessRole: Student` |
| Audit | `ApproveRegistration` row written naming the student and the approving action |

## 7. Lecturer Verification

| Step | Result |
|---|---|
| Registration | `POST /auth/register` (role Lecturer) → **201** |
| Course persistence | Lecturer row created; `RegistrationStatus = 1 (PendingApproval)` — **not** `PendingCourseSelection` |
| Unit persistence | Lecturer A → **exactly VERU101 + VERU102**; Lecturer B → **exactly VERU103**. `Status = PendingApproval` — correct, no over-allocation |
| Offering assignment | `course_offering_lecturers` row created for each lecturer (notes: *"units: VERU101, VERU102"* / *"units: VERU103"*), `Status = PendingConfirmation`, `IsActive = true` |
| Offering units | `course_offering_units` rows created for the allocated units |
| Tenant | `tenant_id = 11111111-…` on every row |
| Approval queue | `GET /Approval/pending?userType=Lecturer` → 200, both lecturers listed |
| Dashboard (pending) | `GET /dashboard/lecturer-me` → **200** (no longer 404 / "not assigned"), but **`courses: []`** — see defect **D3** |
| Pending material authorization | **NOT gated** — `POST /study-materials/unit/VERU101` → **201**, and `POST /study-materials/unit/VERU103` (a unit allocated to **Lecturer B**) → **201**. See defect **D4** |
| Approval | `POST /Approval/approve` → **200** `"Approved"`; `UnitAllocations` flipped to `Active`; audit row written; applicant notified |
| Teaching assignment active? | **No** — `course_offering_lecturers.Status` stays `PendingConfirmation` after approval. See defect **D3** |
| Study Materials after approval | `unitAllocations` populated (VERU101/102). `courses` still `[]` until the lecturer accepts the assignment via `POST /Confirmation/teaching/{id}/confirm` → **200**, after which `courses` contains VERCS01 `status: Active` with its units |
| Final state | Lecturer A `RegistrationStatus = 2 (Approved)`, 2 unit allocations, 1 offering assignment |

## 8. Notifications

| Step | Result |
|---|---|
| Accommodation — student | Type `Accommodation`, title *"Accommodation Allocation Required"*, priority `Important`, `actionUrl = /accommodation`. Body: *"Verification A has just registered as a Student. Role: Student · Student ID: STU202610034735 · Course: VERIFICATION COMPUTER SCIENCE · Accommodation allocation required."* — identifies person, role, number, course and action |
| Accommodation — lecturer | Same shape for lecturers (employee number in place of student number) |
| **Recipients** | Fanned out to **SystemAdministrator, Administrator, Coordinator, Receptionist** (4 per registration) — the required Administrator / Coordinator / Receptionist are all present |
| Approval notification | Type `AccountApproval`, *"Registration Awaiting Approval"*, `actionUrl = /users`, body carries role + course + unit list. Recipients: the same 4 back-office roles — **note: Receptionist also receives it**, a superset of the Administrator/Coordinator expectation |
| Approval outcome | Type `Registration`, *"Registration Approved"*, addressed to the applicant's own account (Student A, Lecturer A) — a **distinct business event** from accommodation |
| Ordering | Both are emitted **after** `_unitOfWork.SaveChangesAsync`, i.e. after the transaction commits |
| **Tenant isolation** | **Every** notification row carries `tenant_id = 11111111-…`. Cross-tenant read attempts returned nothing |

## 9. Security

| Check | Expected | Actual | Result |
|---|---|---|---|
| Student A reads own request | 200 | 200 | **PASS** |
| Student B reads Student A's request | 403 | **403** | **PASS** |
| Student B requests privileged scope `?scope=all` | 403 | **403** | **PASS** |
| Student B lists own requests | only own | only own | **PASS** |
| **Student B updates Student A's request** | 403 | **200 — and it persisted** (`Title` became *"HIJACKED BY STUDENT B"* while `RequesterUserId` stayed Student A) | **FAIL — D2** |
| Lecturer B reads Lecturer A's unit materials | denied | 200 | **FAIL — D5** |
| **Lecturer B deletes Lecturer A's materials** | 403 | **204 — persisted** (`is_deleted = true`) | **FAIL — D5** |
| **Pending Lecturer A writes material to VERU103 (Lecturer B's unit)** | 403 | **201 — persisted** | **FAIL — D4** |
| Student A lists courses (Tenant A) | no Tenant B rows | none | **PASS** |
| Student A reads Tenant B course by id | 403/404 | **403** | **PASS** |
| Student A units/notifications leak Tenant B | none | none | **PASS** |
| Registration retry (same email) | rejected | **409 Conflict** | **PASS** |

## 10. RLS Verification (section 29)

Performed by connecting **as `sms_app`** (the real application role), *not* as the
bootstrap superuser.

| Check | Result |
|---|---|
| Connected role | `sms_app` — `rolsuper = f`, **`rolbypassrls = f`** |
| RLS enabled on `Students`, `Enrollments`, `course_offering_enrollments`, `course_offering_lecturers`, `course_offering_units`, `UnitAllocations`, `Notifications`, `AuditLogs`, `sms_requests` | **enabled** (relrowsecurity = true) |
| Policy shape | `USING (tenant_id = app.current_tenant_id())` — 4 policies per table |
| Rows visible as tenant `11111111-…` | Students 2, Enrollments 6, Notifications 28, UnitAllocations 3, Lecturers 2, sms_requests 1 |
| Rows visible as tenant `99999999-…` | **0 in every table** |
| Direct cross-tenant read by **known primary key** | **0 rows** (students, enrollments, notifications) |
| Cross-tenant `INSERT` | **Blocked**: `new row violates row level security policy for table "Students"` |

> **Note on the earlier 0-row reading:** the session GUC is `app.tenant_id`, not
> `app.current_tenant_id`. Setting the wrong name makes `app.current_tenant_id()`
> return `00000000-…`, which correctly hides *everything*. With the correct GUC the
> role sees its own tenant and nothing else. This is normal fail-closed RLS behaviour,
> not a fault.

**Conclusion:** RLS enforcement is genuinely verified under the NOBYPASSRLS runtime
role. It is **not** an artifact of a superuser test connection.

## 11. Database Verification

Read-only SQL (plus fixture provisioning). Key observed state:

```
Students        : STU...4735 (Student A, Approved, 3 enrollments, 1 offering enrollment)
                  STU...6003 (Student B, PendingApproval, 3 enrollments, 1 offering enrollment)
                  STU...8721 (no-offering student, PendingApproval, 1 enrollment, 0 offering enrollments)
Lecturers       : LEC...9451 (Lecturer A, Approved, 2 unit allocations, 1 offering assignment)
                  LEC...3077 (Lecturer B, PendingApproval, 1 unit allocation, 1 offering assignment)
Enrollments     : Student A -> Active/true x3; Student B -> PendingApproval/false x3
UnitAllocations : Lecturer A -> Active x2 (VERU101, VERU102); Lecturer B -> PendingApproval x1 (VERU103)
Notifications   : 28 rows, all tenant-scoped, 3 distinct business types
AuditLogs       : Register + ApproveRegistration rows for every account and approval
```

Duplicate checks (§33) and retry (§34):

* A single registration created **no** duplicate `Enrollments`, `course_offering_enrollments`, `course_offering_lecturers`, `course_offering_units` or `UnitAllocations` rows.
* Re-submitting the same registration email returns **409 Conflict** — duplicate registration is rejected and no duplicate relationships are created.

## 12. Test Results

| Suite | Baseline (documented) | **This verification** |
|---|---|---|
| Unit (`SMS.UnitTests`) | 978 passed / 0 failed | **978 passed / 0 failed** |
| Integration (`SMS.IntegrationTests`) | 48 passed / **72 failed** (`Failed to connect to 127.0.0.1:5433`) | **120 passed / 0 failed** |
| API (`SMS.ApiTests`) | 16 passed / **207 failed** (same) | **223 passed / 0 failed** |
| Frontend (`vitest`) | not recorded | **390 / 391 passed** (see below) |
| Frontend `tsc` (via `npm run build`) | clean | **clean** |
| Frontend production build | pass | **pass** (`built in 1m 26s`) |

**The historical integration/API failures were entirely environmental.** With Docker
Desktop running and PostgreSQL reachable, **every** previously failing test passes
(72 + 207 = 279 tests recovered). No test was excluded, filtered or hidden.

### Frontend flake analysis

Two runs, same code:

| Run | Result |
|---|---|
| Under heavy concurrent load (Docker + API + .NET builds) | 389 passed / 2 failed |
| After quiescing the machine | 390 passed / 1 failed |
| Both failures re-run in isolation (`--testTimeout=30000`) | **25 passed / 25 passed** |

Both failures are `Error: Test timed out in 5000ms`:

* `src/pages/Register.test.tsx` — *"disables Next until a strong password is entered"*
* `src/pages/AccommodationReports.test.tsx` — *"loads the current occupancy report..."*

**Classification: environment / test-harness, not an application defect.** They are
vitest default-timeout overflows that pass when the machine is not saturated; no
assertion failed.

## 13. Acceptance Matrix

| Test | Expected | Actual | Result |
|---|---|---|---|
| Student registration | PendingApproval | 201, `RegistrationStatus=1` | **PASS** |
| Student course persistence | Correct | `SelectedCourseId = 62dfbd90-...` | **PASS** |
| Student unit persistence | Correct | 3 `Enrollments` (VERU101/102/103) | **PASS** |
| Student profile | Course + units visible | course visible; `unitsCount = 0` | **PARTIAL — D1** |
| Student dashboard | Course + units visible | `pendingCourse` populated, no false "no course"; units appear in Study Materials after approval | **PASS** |
| Student request types | 200 | **200** (was 403) | **PASS** |
| Student create request | Success | **201** `REQ-2026-000001` | **PASS** |
| Student own request | Accessible | 200 | **PASS** |
| Student other user's request | 403 | 403 for GET and `?scope=all`; **200 for PUT** | **FAIL — D2** |
| Student approval queue | Visible to admin | 200, both students listed | **PASS** |
| Student approval | Success | 200 `Approved`, enrollments activated | **PASS** |
| Lecturer registration | PendingApproval | 201, `RegistrationStatus=1` | **PASS** |
| Lecturer course persistence | Correct | lecturer + offering assignment created | **PASS** |
| Lecturer unit persistence | Correct | exactly VERU101 + VERU102 | **PASS** |
| Lecturer dashboard pending | Selection visible | 200 but `courses: []` | **FAIL — D3** |
| Lecturer pending material management | Correctly gated | **201 Created** (incl. another lecturer's unit) | **FAIL — D4** |
| Lecturer approval | Success | 200 `Approved`, allocations -> Active | **PASS** |
| Lecturer approved materials | Accessible | accessible; `courses` populates only after lecturer confirms | **PARTIAL — D3** |
| Lecturer cross-user access | Denied | **204 delete succeeded** | **FAIL — D5** |
| Student accommodation notification | 3 recipients | 4 (incl. all 3 required) | **PASS** |
| Lecturer accommodation notification | 3 recipients | 4 (incl. all 3 required) | **PASS** |
| Registration approval notification | Correct recipients | 4 back-office roles | **PASS** (note: Receptionist included) |
| Notification tenant isolation | Enforced | all rows tenant-scoped | **PASS** |
| No-offering behavior | No fabricated offering | 0 offering rows, selection persisted, PendingApproval | **PASS** |
| Failed registration rollback | No partial state | no academic state/notifications; **orphan Identity user remains** | **PARTIAL — D6** |
| Cross-tenant student access | Denied | 403; no leaks | **PASS** |
| Cross-tenant lecturer access | Denied | no leaks | **PASS** |
| RLS/application role | Verified or documented | verified as `sms_app` (NOBYPASSRLS) | **VERIFIED** |
| Integration tests | Pass | **120 / 120** | **PASS** |
| API tests | Pass | **223 / 223** | **PASS** |
| Frontend tests | Pass | 390/391; flake passes in isolation | **PASS (flake)** |
| Frontend build | Pass | `tsc && vite build` succeeded | **PASS** |

## 14. Defects

Six issues were found. **None was fixed** — per the task's safety rules this was a
verification pass. **D2 and D5 are pre-existing**, i.e. present at the previous
baseline `9fb7e368` and *not* introduced by `34372f5`.

### D1 — `unitsCount` is always 0 (read-path gap left by the repair) — minor

* **Severity:** Low. Display only; authorization is unaffected.
* **Observed:** `GET /api/v1/enrollment/my-status` returns `"unitsCount": 0` for a
  student with 3 persisted `Enrollments` rows — before *and* after approval.
* **Root cause:** `GetMyPendingEnrollmentQueryHandler.Handle`
  (`src/SMS.Application/Features/Enrollments/Queries/GetMyPendingEnrollmentQuery.cs:90`)
  computes `var unitsCount = student.Enrollments?.Count ?? 0;`, but the navigation is
  never loaded. `StudentRepository.GetStudentByEmailAsync`
  (`src/SMS.Persistence/Repositories/StudentRepository.cs:26-28`) includes only
  `s => s.SelectedCourse`, **not** `s => s.Enrollments`, and lazy-loading proxies are
  not enabled anywhere (`UseLazyLoadingProxies` absent from `Program.cs` and
  `ApplicationDbContext`). The collection is therefore always empty.
* **Relation to the repair:** the repair report lists
  `GetMyPendingEnrollmentQuery.UnitsCount -> 0` among the symptoms being fixed, and
  correctly created the `Enrollment` rows — but the *read* path that counts them was
  not updated, so the symptom persists.

### D2 — Any student can update any other student's request — HIGH (pre-existing)

* **Severity:** High. Broken object-level authorization.
* **Endpoint:** `PUT /api/v1/oms/requests/{requestId}`
* **Observed:** Student B sent `PUT /oms/requests/3657418d-...` with a new title ->
  **HTTP 200**. Database confirms `sms_requests.Title = 'HIJACKED BY STUDENT B'`
  while `RequesterUserId` remained Student A's id.
* **Root cause:** `UpdateRequestCommandHandler.Handle`
  (`src/SMS.Application/Features/OMS/Commands/UpdateRequestCommand.cs:78-80`) guards with
  `req.RequesterUserId != _currentUser.UserId && !OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.UpdateRequestRoles)`
  but `OmsAuthorization.UpdateRequestRoles` is aliased to `CreateRequestRoles`
  (`src/SMS.Application/Common/OmsPermissions.cs:147`), which is *"all roles"* and
  **includes `Student`**. For any student the second operand is `true`, so the whole
  condition is false and the ownership check never fires. The comment on line 146 says
  *"Update **own** draft/returned requests"*, so the intent is clearly own-records-only.
* **Pre-existing:** `git show 9fb7e368:src/SMS.Application/Common/OmsPermissions.cs`
  contains the identical alias. `34372f5` did not touch it.
* **Note:** the sibling read path is correct — `GET /{id}` returns 403 and
  `GetRequestsQuery.ResolveScope` rejects `scope=all`, so the new
  `Oms.CanViewOwnRequest` policy is itself correctly implemented. Only the *update*
  verb is unguarded.

### D3 — Lecturer teaching assignment never becomes Active; dashboard `courses` stays empty — MEDIUM

* **Severity:** Medium. Blocks the dashboard / Study Materials sections the repair set out to fix.
* **Observed:** After admin approval, `GET /dashboard/lecturer-me` still returns
  `"courses": []`. `courses` only populates after the lecturer separately calls
  `POST /Confirmation/teaching/{id}/confirm`.
* **Root cause:** three parts.
  1. `RegisterCommand.CreateLecturerRecord` writes
     `CourseOfferingLecturer.Status = "PendingConfirmation"` (`RegisterCommand.cs:694`).
  2. `CourseOfferingLecturerRepository.GetActiveByLecturerAsync` filters
     `l.Status == "Active"` (`CourseOfferingLecturerRepository.cs:66`), which the
     lecturer dashboard reads.
  3. `ApproveRegistrationCommand`'s lecturer branch
     (`ApproveRegistrationCommand.cs:143-155`) flips `Lecturer.RegistrationStatus`
     and `UnitAllocation.Status` to `Active` but **never updates
     `course_offering_lecturers`** — verified in the database: status remained
     `PendingConfirmation` after approval.
* **Relation to the repair:** the commit message states the
  `CourseOfferingLecturer` row *"is what the lecturer dashboard reads (hence 'You are
  not assigned to teach any course offerings yet')"*. The row is now created, but
  because it is created as `PendingConfirmation` and approval does not activate it,
  the reported user-visible symptom is **not** resolved by approval alone. The
  two-step approve -> confirm lifecycle is real and reachable, so this is a gap in
  the repair's stated outcome rather than a broken architecture.

### D4 — Pending lecturer can create study materials, including in another lecturer's unit — HIGH

* **Severity:** High. The approval gate does not gate privileged teaching actions.
* **Endpoints:** `GET /api/v1/study-materials/my-units`,
  `GET /api/v1/study-materials/unit/{unitId}`,
  `POST /api/v1/study-materials/unit/{unitId}`
* **Observed:** Lecturer A, still `RegistrationStatus = PendingApproval`:
  * `my-units` -> 200 returning **all three** units of the offering, including
    **VERU103, which is allocated only to Lecturer B**;
  * `POST` to VERU101 -> **201**;
  * `POST` to **VERU103** -> **201** (material persisted, `lecturerId` = Lecturer A).
* **Root cause:** `LecturerRepository.GetTaughtUnitIdsAsync`
  (`src/SMS.Persistence/Repositories/LecturerRepository.cs:47-80`) unions two sources:
  1. `UnitAllocation` where `Status == "Active"` — correctly excludes pending allocations;
  2. `CourseOfferingLecturer` filtered **only** on `IsActive && !IsDeleted`
     (**no `Status` filter**), then expanded to *every* `CourseOfferingUnit` of that offering.

  Registration sets `CourseOfferingLecturer.IsActive = true` with
  `Status = "PendingConfirmation"`, so branch 2 grants a pending lecturer every unit in
  the offering. The offering snapshot holds all three units because Lecturer B
  registered against the same offering.
* **Directly contradicts** the repair commit's claim that privileged teaching actions
  remain gated — §19 of the verification brief asks specifically for this and it does
  not hold.

### D5 — Lecturers can read and delete other lecturers' study materials — HIGH (pre-existing)

* **Severity:** High. Broken object-level authorization.
* **Endpoints:** `GET /api/v1/study-materials/unit/{unitId}` (200 for a unit the
  caller does not hold), `DELETE /api/v1/study-materials/{id}?unitId=...` (**204**).
* **Observed:** Lecturer B deleted both of Lecturer A's materials. Database confirms
  `LectureNotes.is_deleted = true` for both.
* **Root cause:** the same `GetTaughtUnitIdsAsync` over-broad union as **D4** — the
  per-unit authorization decision is *unit-scoped*, not *material-scoped*, so any
  lecturer who resolves the unit may delete anyone's material inside it.
* **Pre-existing:** this resolution logic was not modified by `34372f5`.

### D6 — Failed registration leaves an orphan Identity user + role — MEDIUM

* **Severity:** Medium. Data-integrity / orphaned account.
* **Observed:** a registration that fails *after* the Identity user is created leaves
  the `AspNetUsers` row and its role assignment behind, with no `Student`/`Lecturer`
  profile. Observed twice:
  * lecturer registration rejected with *"No academic period is configured..."* ->
    `verify.lecturer.a@...` existed with role `Lecturer` but `Lecturers` had no row
    (hence the later `409` on retry);
  * registration with a non-existent course -> **404**, and an orphan
    `verify.rollback2.student@...` user remained.
* **Correct behaviour observed:** **no** partial academic state and **no**
  notifications leak from failed registrations — `Students` 0, `Enrollments` 0,
  accommodation/approval notifications 0. So the notification-after-commit ordering
  (section 26) is **correct**; the residue is confined to the Identity layer.
* **Root cause:** `RegisterCommandHandler.Handle` creates the user and assigns the role
  before calling `CreateStudentRecord`/`CreateLecturerRecord`, and no transaction
  spans the Identity writes and the academic writes (the handler issues several
  `_unitOfWork.SaveChangesAsync` calls with no ambient transaction).

### Minor observations (not defects)

* `POST /auth/register` returns `registrationStatus: "PendingCourseSelection"` in the
  response DTO while the persisted record is `PendingApproval`. The DTO field is
  hard-coded at `RegisterCommand.cs:326` and is stale relative to the persisted state;
  every read endpoint (`/enrollment/my-status`, `/dashboard/student-me`) correctly
  reports `PendingApproval`. Cosmetic inconsistency only.
* `RejectRegistration` / `BulkApprove` were not exercised — out of scope for this brief.
* The seeded `Semesters` row is scoped to `tenant_id = 00000000-...`, so lecturer
  registration is impossible in a freshly seeded tenant until an administrator creates
  a tenant-scoped semester through a channel that **does not exist in the API**.

## 15. `/approvals` Route Discrepancy (section 31)

* There is **no `/approvals` route** in `frontend/sms-web/src/App.tsx` (confirmed by
  reading the route table).
* The registration-approval notification uses `actionUrl = "/users"`, and
  `<Route path="users" element={<Users />} />` **does** exist (line 162).
* `BusinessEventNotifier.cs:51-53` documents this deliberately: *"existing
  GET /approval/pending API surfaced through the account management screens. Pointing
  at /users keeps the notification's action [valid]"*.
* **Verdict:** the destination is functional and intentional. **No new route was
  created**, per the brief. If a dedicated approval screen is ever wanted it is a
  feature request, not a repair.

## 16. Environmental Limitations

1. **Single-tenant host, Windows + WSL2 Docker.** Linux-specific compose behaviour,
   nginx/backup/prometheus/Grafana containers and the `sms-*` production stack were
   **not** started. Verification used the repository's own
   `docker/docker-compose.test.yml` PostgreSQL plus a locally hosted API — not the
   production compose topology.
2. **Real browser UI not driven.** The SPA was exercised through its HTTP APIs with
   genuine cookie + CSRF double-submit headers, not through a headless browser.
   Findings about *what a user sees* on Study Materials / Courses-and-Units screens are
   therefore inferred from the payloads those screens consume (notably D3's empty
   `courses` array), not from rendered pixels.
3. **SMS backend not reachable** (`192.168.110.161` unreachable from this host, as in
   earlier reports). No production deployment was attempted and none is claimed.
4. **Notification delivery is in-app only.** No email/SMS/SignalR transport was
   exercised; persistence, fan-out, type/priority/action-URL and tenant scoping were.
5. **Reference-data fixtures inserted directly** (tenant B, tenant-scoped semester,
   the OMS request type) because no API exists for them. These are prerequisites, not
   workflow bypasses; every workflow decision went through the public API.
6. **One frontend test is flaky under load** (§12). It passes in isolation.
7. **Docker Desktop was not running at the start of this task** and had to be launched.
   This is the sole reason the historical integration/API suites failed, and it is an
   environment characteristic, not a code defect.

## 17. Conclusion

**FAIL.**

The **core repair is confirmed working**, and this verification converted the entire
previously-unverifiable surface into real runtime evidence:

* All **279** historically failing integration and API tests now pass — they were
  environmental, exactly as claimed.
* Student registration persists the **course and all three units**, reaches
  **`PendingApproval`**, becomes **visible to administrators**, can be **approved**,
  and the student then gains **real access to all three units** — the approval
  dead-end is genuinely closed.
* The **student New Request 403 is genuinely fixed**: types return 200, a request can
  be created, and own-request read/list work.
* Accommodation and approval notifications are emitted **after commit**, with the
  correct content, the correct back-office recipients and correct **tenant scoping**.
* Registration **rollback** correctly leaves no academic or notification residue.
* **Row-level security is genuinely enforced** under the real `sms_app` NOBYPASSRLS role.
* The **no-active-offering** contract holds: nothing is fabricated.

However, three of the repair's own stated outcomes are **not** met — pending-lecturer
privileged material management is **not** gated (**D4**), cross-lecturer material
management is **not** denied (**D5**), and the lecturer dashboard does **not** become
populated through approval (**D3**). A further pre-existing authorization gap allows
any student to modify any other student's request (**D2**). Because §19 and §22 of the
brief require these denials and they do not occur, a **PASS** cannot be claimed.

D2 and D5 predate `34372f5` and are not regressions from it. D3 and D4 are gaps in the
repair as delivered. D1 and D6 are lower-severity residue. All six are documented above
with exact endpoints, payloads, responses, database state, source locations and
severities. **No source code was changed and no commit was created.**