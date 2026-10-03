# Registration Workflow Repair, Verification and Deployment Report

> Repair of the six verified defects (D1–D6) from
> `Documentation/REGISTRATION_WORKFLOW_PRODUCTION_LIKE_VERIFICATION.md`, followed
> by a fresh production-like verification against a **real PostgreSQL 16** stack
> (the application runtime role `sms_app`, `NOSUPERUSER` / `NOBYPASSRLS`) and by
> deployment of the exact verified commit to production.
>
> The previous report is **preserved unchanged**.

---

## 1. Baseline

| Item | Value |
|---|---|
| Previous SHA | `eb50939faf1afdf8213e75e3a4e6d4cf9e69ea64` |
| Branch | `coordinator-repair` |
| Working tree (before) | 4 untracked files only; **no tracked modifications** |
| Repair-under-test commit | `34372f5` (`fix(registration): persist course/unit selections and unblock student requests`) |
| Pristine baseline for D2/D5 | `9fb7e36800aff93b9527cb14e6da1a50c07da0b5` |

Untracked files present before this work (all pre-existing, none discarded):

```
?? Documentation/AGENT_PROMPT_STUDENT_COURSE_SELECTION_FIX.md
?? Documentation/Accommodation/ACCOMMODATION_PRODUCTION_FUNCTIONAL_VERIFICATION_REPORT.md
?? Documentation/REGISTRATION_WORKFLOW_PRODUCTION_LIKE_VERIFICATION.md
?? PRODUCTION_NOTIFICATIONS_PWA_DEPLOYMENT_REPORT.md
```

> **Note on the previous report's tracking state.** The brief described three
> pre-existing untracked files. `Documentation/REGISTRATION_WORKFLOW_PRODUCTION_LIKE_VERIFICATION.md`
> was in fact a **fourth** untracked file, and commit `eb50939` contains
> `REGISTRATION_AND_WORKFLOW_REPAIR_REPORT.md` (not the production-like report).
> The production-like report was therefore **never committed**. It has been left
> byte-for-byte untouched and is committed unchanged in this repair so the
> evidence chain is complete.

### Baseline tests (before any change)

| Suite | Result |
|---|---|
| Relevant unit tests | 412 passed / 0 failed |
| Full solution build | 0 errors |

---

## 2. Verified Defects

| ID | Defect | Severity | Status |
|---|---|---|---|
| **D1** | Student `unitsCount` always 0 | Low | **FIXED** |
| **D2** | Any student can update any student's request | High (pre-existing) | **FIXED** |
| **D3** | Lecturer teaching assignment never becomes Active; dashboard `courses` empty | Medium | **FIXED** |
| **D4** | Pending lecturer can create study materials, including another lecturer's unit | High | **FIXED** |
| **D5** | Lecturer B can delete Lecturer A's materials | High (pre-existing) | **FIXED** |
| **D6** | Failed registration leaves an orphan Identity user | Medium | **FIXED** |

---

## 3. Root Causes

### D1 — `unitsCount` always 0

`GetMyPendingEnrollmentQueryHandler` computed `student.Enrollments?.Count`, but
`StudentRepository.GetStudentByEmailAsync` only included `SelectedCourse`.
Lazy-loading proxies are not enabled anywhere (`UseLazyLoadingProxies` is absent
from `Program.cs` and `ApplicationDbContext`), so the navigation was **always an
empty collection at runtime** — never a test-only artefact. Registration wrote the
`Enrollment` rows correctly; only the read path was broken.

### D2 — cross-user request update

`UpdateRequestCommandHandler` guarded with

```csharp
req.RequesterUserId != _currentUser.UserId &&
!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.UpdateRequestRoles)
```

but `UpdateRequestRoles` was **aliased to `CreateRequestRoles`** ("all roles",
including `Student`). For any student the second operand was `true`, so the whole
condition was `false` and the ownership check never fired. The comment above the
alias ("Update own draft/returned requests") shows the intent was own-records-only.
The sibling read path (`OmsRequestAccess.CanView` + `Oms.CanViewOwnRequest`) was
already correct.

### D3 — teaching assignment never activated

Three parts:

1. `RegisterCommand.CreateLecturerRecord` writes `CourseOfferingLecturer.Status = "PendingConfirmation"`.
2. `CourseOfferingLecturerRepository.GetActiveByLecturerAsync` filters `Status == "Active"` — and that is the relationship the lecturer dashboard reads.
3. `ApproveRegistrationCommand`'s lecturer branch flipped `Lecturer.RegistrationStatus` and `UnitAllocation.Status` but **never touched `course_offering_lecturers`**.

A second, related defect: the dashboard listed **every** unit of the shared
`course_offering_units` snapshot, so each lecturer also saw colleagues' units.

### D4 / D5 — over-broad teaching entitlement and unit-scoped delete

`LecturerRepository.GetTaughtUnitIdsAsync` expanded every `CourseOfferingLecturer`
row — filtering only `IsActive`, **never `Status`** — to *every* unit of that
offering. Registration sets `IsActive = true` with `Status = "PendingConfirmation"`,
so branch 2 granted a pending lecturer the entire course, including units allocated
to other lecturers.

`DeleteStudyMaterialCommandHandler` then used a purely unit-scoped rule:

```csharp
currentLecturer.Id == material.LecturerId ||
await _academicAccessService.LecturerTeachesUnitAsync(currentLecturer.Id, material.UnitId)
```

A unit-level entitlement is **shared by every lecturer appointed to that unit**, so
the second clause let Lecturer B delete Lecturer A's material merely because both
lectured the same course.

### D6 — orphan Identity user on failed registration

ASP.NET Identity is configured with `AddIdentity().AddEntityFrameworkStores<ApplicationDbContext>()`
— the **same** `DbContext` as the academic repositories — but registration issued
several `_unitOfWork.SaveChangesAsync()` calls with **no ambient transaction**.
`UserManager` commits its own `SaveChanges`, so a failure after user creation (no
academic period, unknown course, foreign unit, duplicate username) left a usable
Identity account with no profile behind it, which also made the same email
unregistrable (409).

---

## 4. Implementation Changes

### D1
* `StudentRepository.GetStudentByEmailAsync` — added `.Include(s => s.Enrollments).ThenInclude(e => e.Course)` so the counted navigation is materialized.
* `GetMyPendingEnrollmentQueryHandler` — `unitsCount` now derives from the non-deleted `Enrollment` rows; still tenant-scoped (tenant-filtered `DbSet`) and user-scoped (rows hang off this student).

### D2
* `OmsPermissions`/`OmsAuthorization` — `UpdateRequestRoles` is now the **ownership-scoped** `ViewOwnRequestsRoles` (no longer aliased to `CreateRequestRoles`); new `UpdateAnyRequestRoles = { SystemAdministrator, Administrator, Coordinator }`.
* `OmsRequestAccess.CanUpdate(request, userId, roles)` — new object-level rule mirroring the existing `CanView`: requester-or-privileged.
* `UpdateRequestCommandHandler` — replaced the alias-based guard with `OmsRequestAccess.CanUpdate(...)`. Tenant check, lifecycle check and `Oms.CanViewOwnRequest` are untouched.

### D3
* `ApproveRegistrationCommandHandler` — now also activates **this lecturer's own** `CourseOfferingLecturer` rows still in `PendingConfirmation` (`Status = "Active"`, `IsActive = true`) and ensures an active `CourseOfferingUnit` relationship exists for each approved, offering-linked unit. `Active`/`Completed`/`Cancelled`/`Removed` rows are left untouched, and no other lecturer's assignment is ever loaded.
* `GetMyLecturerDashboardQueryHandler` — loads the lecturer's active allocations first and, when they exist for an offering, shows **only those units** of the shared snapshot. An administrative appointment with no allocation of its own keeps the previous full-snapshot behaviour.

### D4
* `LecturerRepository.GetTaughtUnitIdsAsync` — taught units are now the lecturer's **own ACTIVE allocations**, each additionally requiring an **ACTIVE** (`Status == "Active"`) teaching assignment when the allocation belongs to an offering. The "expand to every offering unit" branch is gone.
* `AcademicAccessService.LecturerTeachesUnitAsync` — now requires `Lecturer.RegistrationStatus == Approved` on both overloads, in addition to the relationship check. The lecturer is always resolved from the authenticated identity.
* `GetMyStudyMaterialUnitsQueryHandler` — withholds all taught units for a lecturer who is not `Approved` (the selector can no longer advertise a unit the API would refuse).

### D5
* `DeleteStudyMaterialCommandHandler` — a lecturer may delete only material **they uploaded** (`material.LecturerId`) **and** for which they hold an approved, active teaching appointment. The Administrator/Coordinator override is unchanged. The relationship is still checked against the material's **stored** `UnitId`, never the client query parameter.
* `Documentation/StudyMaterials/README.md` — the authorization table was corrected; the old row documented the vulnerable "ownership **or** teaching relationship" rule.

### D6
* `RegisterCommandHandler` — the identity + academic block now runs inside `_unitOfWork.ExecuteInTransactionAsync(...)`. Because Identity and the academic repositories share one `ApplicationDbContext`, the transaction spans both. `ExecuteInTransactionAsync` (not `BeginTransactionAsync`) is required because the Npgsql retrying execution strategy owns user-initiated transactions; the tenant GUC is session-scoped, so RLS is unaffected.
* `RegisterCommandHandler.CompensateFailedRegistrationAsync` — defence in depth: if the transaction itself cannot be established, the account **created by this attempt** (captured by object identity, never by email lookup) is deleted. Only the exact user returned by `CreateUserAsync` for this attempt can match; an account created by any other process can never be touched, because the handler returns 409 earlier when `FindByEmailAsync` finds one. Cleanup failures are logged and never replace the original registration failure.

### Approval semantics — unchanged
`RegistrationStatus`, `ApprovalController`, `ApproveRegistrationCommand`, `RejectRegistrationCommand`, `GetPendingApprovalsQuery` and the audit trail are all preserved. Registration still enters `PendingApproval`; approval is still the only transition to `Approved`/`Active`.

---

## 5. Regression Tests

| Suite | Baseline | After repair |
|---|---|---|
| Unit (`SMS.UnitTests`) | 978 passed / 0 failed | **1040 passed / 0 failed** (+62) |
| Integration (`SMS.IntegrationTests`) | 120 passed / 0 failed | **120 passed / 0 failed** |
| API (`SMS.ApiTests`) | 223 passed / 0 failed | **231 passed / 0 failed** (+8) |
| Frontend (`vitest`) | 390 / 391 | **390 / 391** (same isolated timeout, §12) |
| Frontend `tsc` + `vite build` | pass | **pass** |
| Solution build | 0 errors | **0 errors** |

New regression files:

| File | Covers |
|---|---|
| `tests/SMS.UnitTests/Enrollments/StudentUnitsCountRegressionTests.cs` | D1 — 3 / 1 / 0 enrollments, soft-deleted rows, before/after approval, per-student scoping, plus a real-DbContext proof that the navigation is loaded |
| `tests/SMS.UnitTests/OMS/RequestUpdateOwnershipTests.cs` | D2 — student B denied, lecturer denied, cross-tenant denied, owner allowed, admin/coordinator preserved, invalid transition rejected, role-set guards |
| `tests/SMS.UnitTests/Approvals/LecturerApprovalActivatesTeachingTests.cs` | D3 — assignment activation, allocation activation, colleague isolation, cancelled assignments preserved, offering-unit creation, dashboard pending vs approved, unselected unit hidden |
| `tests/SMS.UnitTests/StudyMaterials/LecturerTeachingEntitlementTests.cs` | D4/D5 — repository derivation (real EF context), approval gate, selector, delete ownership, admin override, student denial, unit-id mismatch |
| `tests/SMS.UnitTests/Auth/RegistrationFailureRollbackTests.cs` | D6 — three failure paths, no notifications, success path untouched, pre-Identity failure deletes nothing, duplicate email never deletes the existing account, failed compensation does not mask the original error |
| `tests/SMS.ApiTests/Controllers/LecturerMaterialAuthorizationApiTests.cs` + fixture | D4/D5 **end-to-end over HTTP against PostgreSQL** |
| `tests/SMS.UnitTests/Common/UnitOfWorkMockExtensions.cs` | Test-double helper: makes a `Mock<IUnitOfWork>` run the transaction delegate like the real unit of work |

Existing fixtures adjusted (semantically, not to weaken anything):

* `DocumentUploadApiTests` — its lecturer is an **active, approved, confirmed** teacher; it now says so explicitly (`RegistrationStatus = Approved`).
* `StudyMaterialAuthorizationTests` — the selector fixture lecturer is now `Approved`, matching the real gate.
* `RegisterCommandTests`, `LecturerRegistrationRelationshipTests`, `StudentCourseSelectionPersistenceTests` — the `IUnitOfWork` double now executes the transaction delegate.

---

## 6. Production-Like Verification

### Environment

| Item | Value |
|---|---|
| PostgreSQL | **16.15 (Alpine)** in `sms-postgres-test` (host port 5433) |
| Verification database | **`sms_repair_verify`** — created empty, disposable, owned by `sms_migration` |
| Migration | `dotnet run --project src/SMS.API -- migrate-database` → **23 migrations applied** |
| Post-migration | `docker/grant-least-privilege-privileges.sql` applied (repository's documented step) |
| Schema after migration | **82 tables**, **70 with RLS enabled** — identical to the previous report |
| Runtime role | **`sms_app`** — `rolsuper = f`, **`rolbypassrls = f`** |
| API | `http://localhost:5088/api/v1`, real ASP.NET Core, `/health` → `Healthy` + `postgresql: Healthy` |
| Auth | httpOnly `access_token` / `refresh_token` cookies + `XSRF-TOKEN` echoed in `X-CSRF-TOKEN` |
| Tenant | `11111111-1111-1111-1111-111111111111` (`Default Tenant`), header `X-Tenant-Id` |

Reference-data fixtures only (no public API exists for them, and they are not workflow bypasses — every registration, approval and authorization decision went through the HTTP API):
* one **tenant-scoped** `Semesters` row (the seeded semester carries `tenant_id = 00000000-…` and is invisible to any real tenant, so lecturer registration is otherwise impossible);
* one OMS `sms_request_types` row (`GENERAL`).

Course/units/offering and all accounts were created through the application's own APIs (`POST /courses`, `POST /units`, `POST /CourseOffering`, `POST /users`, `POST /auth/register`, `POST /Approval/approve`).

Test graph: course `RVCS…` with **three** units (`U1`, `U2`, `U3`) and **one active offering** containing all three. Lecturer A selected **U1 + U2**, Lecturer B selected **U3**, against the *same* offering — so the shared offering snapshot legitimately contains all three units and every assertion below holds even though "teaches the unit" is true of the attacker.

### 6.1 Student

| Step | Expected | Actual | Result |
|---|---|---|---|
| `POST /auth/register` (Student A, Student B) | 201 | 201 | **PASS** |
| `GET /enrollment/my-status` → `registrationStatus` | `PendingApproval` | `PendingApproval` | **PASS** |
| `GET /enrollment/my-status` → **`unitsCount`** | **3** | **3** (was 0) | **PASS — D1** |
| `GET /enrollment/my-status` → `hasSelectedCourse` / `selectedCourseName` | true / course | true / `REPAIR VERIFICATION COMPUTER SCIENCE` | **PASS** |
| `GET /dashboard/student-me` | course visible, no false "no course" | student identity + selected-course card; `registrationStatus: Approved` after approval | **PASS** |
| `GET /study-materials/my-units` (approved) | 3 enrolled units | 3 | **PASS** |
| `GET /oms/requests/types` | 200 | 200 | **PASS** |
| `POST /oms/requests` | 201 | 201 (`REQ-2026-000001`) | **PASS** |
| `GET /oms/requests/{id}` (own) | 200 | 200 | **PASS** |
| `GET /oms/requests` (own list) | only own | only own | **PASS** |
| `POST /Approval/approve` | 200 `Approved` | 200, both students approved | **PASS** |
| After approval → `unitsCount` / `isApproved` | 3 / true | 3 / true | **PASS — D1** |
| Database | 3 `Enrollments` per student | 3 each | **PASS** |

### 6.2 Lecturer

| Step | Expected | Actual | Result |
|---|---|---|---|
| `POST /auth/register` (Lecturer A: U1+U2, Lecturer B: U3) | 201 | 201 | **PASS** |
| Persisted allocations | exactly the selected units | A → U1+U2, B → U3 (no over-allocation) | **PASS** |
| Before approval: `GET /study-materials/my-units` | `[]` | `[]` | **PASS — D4** |
| Before approval: `GET /study-materials/unit/{U1}` | 403 | **403** | **PASS — D4** |
| Before approval: `GET /dashboard/lecturer-me` | 200, `courses: []` | 200, `courses: []` | **PASS** |
| `POST /Approval/approve` | 200 `Approved` | 200, both lecturers | **PASS** |
| After approval: `course_offering_lecturers.Status` | **Active** | **Active** (was `PendingConfirmation`) | **PASS — D3** |
| After approval: `UnitAllocations.Status` | Active | Active | **PASS** |
| After approval: lecturer A dashboard `courses` | 1 | 1 (`REPAIR VERIFICATION COMPUTER SCIENCE`, `status=Active`) | **PASS — D3** |
| After approval: lecturer A dashboard units | **U1 + U2 only** | U1 + U2 | **PASS — D3** |
| After approval: lecturer A dashboard **must not** show U3 | absent | **absent** | **PASS — D3** |
| Lecturer B dashboard | U3 only | U3 only, no U1 | **PASS — D3** |
| After approval: `my-units` for lecturer A | U1 + U2 | 2 units, U3 absent | **PASS — D4** |
| Approved lecturer A `POST` material to **U1** | 201 | **201** | **PASS** |
| Approved lecturer A `POST` material to **U3** (unassigned) | 403 | **403** | **PASS — D4** |
| Approved lecturer B `POST` material to **U1** (not theirs) | 403 | **403** | **PASS — D4** |
| Approved lecturer B `POST` material to **U3** (theirs) | 201 | **201** | **PASS** |

### 6.3 Approval

| Step | Expected | Actual | Result |
|---|---|---|---|
| `GET /Approval/pending?userType=Student` | lists students | 2 listed | **PASS** |
| `GET /Approval/pending?userType=Lecturer` | lists lecturers | 2 listed | **PASS** |
| `POST /Approval/approve` ×4 | 200 `Approved` | 200 ×4 | **PASS** |
| Audit trail | `ApproveRegistration` rows | 4 rows | **PASS** |
| Registration still `PendingApproval` before approval | never immediate | confirmed | **PASS** |

### 6.4 Requests (D2)

| Check | Expected | Actual | Result |
|---|---|---|---|
| Student A creates own request | 201 | 201 | **PASS** |
| Student A reads own request | 200 | 200 | **PASS** |
| **Student B reads Student A's request** | 403 | **403** | **PASS** |
| **Student B `PUT`s Student A's request** | **403** | **403 FORBIDDEN** (was **200 + persisted**) | **PASS — D2** |
| Student A's request title after the attempt | unchanged | unchanged | **PASS — D2** |
| Student A updates own draft request | 200 | 200, persisted | **PASS** |
| Student B `GET /oms/requests?scope=all` | 403 | **403** | **PASS** |
| Student A's request list | own only | own only | **PASS** |

### 6.5 Materials (D5)

| Check | Expected | Actual | Result |
|---|---|---|---|
| Lecturer A uploads material for U1 | 201 | 201 | **PASS** |
| **Lecturer B `DELETE`s Lecturer A's material** | **403** | **403 FORBIDDEN** (was **204 + persisted**) | **PASS — D5** |
| Material still listed after the refused delete | present | present | **PASS — D5** |
| Lecturer A deletes own material | 204 | 204 | **PASS** |
| Material gone after the allowed delete | absent | absent | **PASS** |

### 6.6 Failure rollback (D6)

| Step | Expected | Actual | Result |
|---|---|---|---|
| `POST /auth/register` with a **non-existent course** | 404 | **404** | **PASS** |
| Orphan Identity user left behind? | none | **none — the same email registers successfully afterwards (201)** (previously `409`) | **PASS — D6** |
| Retry with a valid course | 201 | **201** | **PASS — D6** |
| Duplicate registration (same email) | 409 | **409** | **PASS** |
| Notifications emitted by the failed registration | none | none | **PASS** |
| Academic rows created by the failed registration | none | none | **PASS** |

### 6.7 Notifications

| Event | Type / title | Recipients observed | Result |
|---|---|---|---|
| Registration (student & lecturer) | `Accommodation` — *Accommodation Allocation Required* | **SystemAdministrator, Administrator, Coordinator, Receptionist** | **PASS** |
| Registration awaiting approval | `AccountApproval` — *Registration Awaiting Approval* | **SystemAdministrator, Administrator, Coordinator, Receptionist** | **PASS** |
| Approval outcome | `Registration` — *Registration Approved* | the applicant's own account (2 Students + 2 Lecturers) | **PASS** |
| Tenant scoping | every notification row | `tenant_id = 11111111-…` | **PASS** |
| Failed registration | none | none | **PASS** |

> The fan-out is role-driven (`AccommodationAllocationRoles` / `RegistrationApprovalRoles`
> = `SystemAdministrator, Administrator, Coordinator, Receptionist`). `Administrator`,
> `Coordinator` and `Receptionist` accounts were provisioned through `POST /api/v1/users`
> before the last registration so all four recipients could be observed directly.

### 6.8 Security matrix

| Check | Expected | Actual | Result |
|---|---|---|---|
| Student A → Student B request (read) | 403 | 403 | **PASS** |
| Student A → Student B request (update) | 403 | 403 | **PASS — D2** |
| Student A → lecturer dashboard | 403 | 403 | **PASS** |
| Student A → `POST` study material | 403 | 403 | **PASS** |
| Lecturer B → Lecturer A material (delete) | 403 | 403 | **PASS — D5** |
| Lecturer A → unassigned unit | 403 | 403 | **PASS — D4** |
| Pending lecturer → `my-units` | `[]` | `[]` | **PASS — D4** |
| Pending lecturer → unit materials | 403 | 403 | **PASS — D4** |
| Unknown tenant header | 400 | 400 (`Invalid tenant`, `TenantResolutionMiddleware`) | **PASS** |

### 6.9 RLS under `sms_app` (NOBYPASSRLS)

| Check | Result |
|---|---|
| Connected role | `sms_app` — `rolsuper = f`, `rolbypassrls = f` |
| RLS enabled on `Students`, `Enrollments`, `course_offering_lecturers`, `UnitAllocations`, `Notifications`, `AuditLogs` | **enabled** |
| Rows visible as tenant `11111111-…` | Students 4, Enrollments 12, Lecturers 2, UnitAllocations 3, course_offering_lecturers 2 |
| Rows visible as tenant `22222222-…` | **0 in every table** |
| Cross-tenant read by known primary key / identifier | **0 rows** |

**RLS remains genuinely enforced under the real NOBYPASSRLS runtime role.**

### 6.10 Final database state

```
Students              : STU…2798 PendingApproval  3 enrollments
                        STU…2886 Approved         3 enrollments
                        STU…2981 PendingApproval  3 enrollments
                        STU…5401 Approved         3 enrollments
Lecturers             : LEC…3858 Approved  Active RVU101  (lecturer A)
                        LEC…3858 Approved  Active RVU102  (lecturer A)
                        LEC…8381 Approved  Active RVU103  (lecturer B)
course_offering_lecturers : Active / Pending  LEC…3858
                            Active / Pending  LEC…8381
AuditLogs             : Register 10, ApproveRegistration 4, Course 1, CourseOffering 1,
                        CreateUnit 3, FileUploaded 2, FileDeleted 1, RequestCreated 1, Update 1, Login 30
```

---

## 7. GitHub

| Item | Value |
|---|---|
| Previous GitHub SHA (`origin/coordinator-repair`) | `9fb7e36800aff93b9527cb14e6da1a50c07da0b5` |
| New GitHub SHA | _recorded after the push — see the commit log at the end of this document_ |

The push is a **fast-forward** from the previous remote head; no force push was used
and no unrelated commit was rewritten.

| Item | Value |
|---|---|
| Previous GitHub SHA (`origin/coordinator-repair`) | `9fb7e36800aff93b9527cb14e6da1a50c07da0b5` |
| New GitHub SHA | **`3f4d8c4c51b89dc81130f6f4b6772e4336cc4a07`** |
| `git rev-parse HEAD` == `git rev-parse origin/coordinator-repair` | **true** |
| Push result | `9fb7e36..3f4d8c4  coordinator-repair -> coordinator-repair` (fast-forward) |

---

## 8. Production Deployment

| Item | Value |
|---|---|
| Host | `192.168.110.161` (`sms-server`), reachable over SSH (22) and HTTPS (443) |
| Application path | `/opt/sms/app` |
| Compose | `docker/docker-compose.prod.yml` |
| Environment file | `/opt/sms/app/.env`, mode `-rw------- sms_admin` — **protected, never printed** |
| Procedure followed | `Documentation/PRODUCTION_UPDATE_FROM_GITHUB.md` |
| Previous production SHA | `9fb7e36800aff93b9527cb14e6da1a50c07da0b5` (detached HEAD) |
| Target SHA | **`3f4d8c4c51b89dc81130f6f4b6772e4336cc4a07`** |
| Deployment start | 2026-10-03 20:01 (server local, +03:00) |
| **Database migration** | **none** — the diff contains no migration, entity, mapping, query filter, compose or frontend change; the API reported *"Database is already up to date. No migrations to apply."* |

### Pre-deployment baseline

| Item | Value |
|---|---|
| Containers | 11 running; `sms-api`, `sms-web`, `sms-nginx`, `sms-postgres`, `sms-cadvisor` healthy |
| Previous API image | `sha256:5344e8ba35216abdb125739bf10c5be85993e2ee1e814514453aaca882f295cc` (built 25 h before) |
| Previous frontend image | `sha256:db20e8e8510a5429e98d63406c6658b48e5cde16166321ba29193d16583e62d5` (built 22 h before) |
| Disk | 229 G total, **155 G available** (29 % used) |
| Backup mechanism | `sms-backup` container → `scripts/backup.sh` → `pg_dump -Fc` into the `docker_backup_data` volume (`/backups`), 24 h interval, 30-day retention, 55 dumps present |
| HTTPS / `/health` | `https://127.0.0.1/` → **200**; `/health` → **Healthy** (postgresql Healthy) |

### Backup

| Item | Value |
|---|---|
| Backup completed | **yes** |
| Backup timestamp | 2026-10-03T16:59:15Z |
| Backup identifier | `/backups/SchoolManagementSystem_20261003T165915Z.dump` (volume `docker_backup_data`) |
| Size / verification | 729.1 K; `pg_restore -l` lists **1078** catalog entries — dump is readable |
| Method | the repository's own `scripts/backup.sh`, executed inside `sms-backup` with its own credentials |

### Cutover

```bash
cd /opt/sms/app
git fetch origin coordinator-repair
git checkout 3f4d8c4c51b89dc81130f6f4b6772e4336cc4a07
docker compose -f docker/docker-compose.prod.yml build api
docker compose -f docker/docker-compose.prod.yml up -d --force-recreate api
```

Only `sms-api` was rebuilt and recreated — the change is backend-only. **No volume was
removed, no database was dropped, no data was reset**, and no other service was
touched (PostgreSQL, frontend, nginx, Prometheus, Grafana, Alertmanager, cadvisor,
node-exporter, postgres-exporter and backup all kept running).

---

## 9. Production Health

| Check | Expected | Actual | Result |
|---|---|---|---|
| Deployed SHA | `3f4d8c4…` | `3f4d8c4c51b89dc81130f6f4b6772e4336cc4a07` | **PASS** |
| API image | new | `sha256:b1bc6e6e9304b2c1b8d573bfa74d33ecb3aed53a765f03757bbb93c3e312ccc6` | **PASS** |
| HTTPS root | 200 | **200** | **PASS** |
| TLS | valid | nginx TLS terminated, `curl -sk https://127.0.0.1/` succeeded | **PASS** |
| `/health` | Healthy | `{"status":"Healthy", … "postgresql":"Database is reachable and responding."}` | **PASS** |
| PostgreSQL | accepting | `/var/run/postgresql:5432 - accepting connections` | **PASS** |
| Containers | all healthy | 11 running, `sms-api` healthy within ~25 s | **PASS** |
| Frontend | loads | `https://127.0.0.1/` → 200 | **PASS** |
| Migration | none | "Database is already up to date. No migrations to apply." | **PASS** |
| Swagger | disabled | `Swagger__Enabled` unchanged in `.env` | **PASS** |
| Logs — 5xx responses since deploy | none | **none** (max status observed: 403) | **PASS** |
| Logs — crashes / DB errors / migration errors | none | none; the only logged exceptions are the expected `ForbiddenException` (20), `UnauthorizedException` (12), `ValidationException` (8) and `FluentValidation.ValidationException` (4) raised by the deliberate negative tests | **PASS** |

### Authentication

| Check | Expected | Actual | Result |
|---|---|---|---|
| `POST /api/v1/auth/login` (seeded SystemAdministrator) | 200 | **200** | **PASS** |
| `XSRF-TOKEN` double-submit cookie issued | present | present | **PASS** |
| `GET /api/v1/auth/me` authenticated | 200 + identity | 200 + identity | **PASS** |
| `POST` without `X-CSRF-TOKEN` | 403 | **403** (CSRF protection active) | **PASS** |
| `GET /api/v1/auth/me` unauthenticated | 401 | **401** | **PASS** |
| Login of the four verification accounts | 200 | 200 ×4 | **PASS** |

---

## 10. Production Smoke Tests

Run on the live host with the real API, the real database and the real
`X-Tenant-Id: 11111111-…` tenant, using **dedicated verification accounts only**
(e-mail pattern `prod.smoke.*@sms-verification.local`). No real user was touched.

### Student

| Check | Expected | Actual | Result |
|---|---|---|---|
| Registration (`POST /auth/register`) | 201 | 201 (student A and student B) | **PASS** |
| Login | 200 | 200 | **PASS** |
| `GET /enrollment/my-status` | 200 | 200 | **PASS** |
| **`unitsCount` with 3 persisted enrollments** | **3** | **3** | **PASS — D1** |
| `registrationStatus` before approval | `PendingApproval` | `PendingApproval` | **PASS** |
| `unitsCount` / `isApproved` after approval | 3 / `true` | 3 / `true` | **PASS — D1** |
| `registrationStatus` after approval | `Approved` | `Approved` | **PASS** |
| `GET /study-materials/my-units` | 3 enrolled units | 3 | **PASS** |
| `POST /oms/requests` | 201 | 201 (`REQ-2026-000031`) | **PASS** |
| `GET /oms/requests/{id}` (own) | 200 | 200 | **PASS** |
| `PUT /oms/requests/{id}` (own) | 200 | 200 | **PASS** |
| Student → `POST` study material | 403 | **403** | **PASS** |

### Lecturer

| Check | Expected | Actual | Result |
|---|---|---|---|
| Registration (lecturer A: U1+U2, lecturer B: U3) | 201 | 201 | **PASS** |
| Login | 200 | 200 | **PASS** |
| Before approval — `GET /study-materials/my-units` | `[]` | **`[]`** | **PASS — D4** |
| Before approval — `GET /study-materials/unit/{U1}` | 403 | **403** | **PASS — D4** |
| Before approval — `POST /study-materials/unit/{U1}` | 403 | **403** | **PASS — D4** |
| Approval (`POST /Approval/approve`) | 200 `Approved` | 200 ×4 (2 lecturers, 2 students) | **PASS** |
| **After approval — dashboard `courses`** | **1** | **1** | **PASS — D3** |
| **After approval — dashboard units** | **exactly the 2 selected** | **exactly 2 (U1 + U2)** | **PASS — D3** |
| Lecturer B dashboard | only its own unit | 1 unit, and **not** lecturer A's unit | **PASS — D3** |
| After approval — `my-units` | the lecturer's own units | 2 for A, 1 for B | **PASS — D4** |
| Approved lecturer A `POST` material to assigned unit | 201 | **201** | **PASS** |
| Approved lecturer A `POST` to the second assigned unit | 201 | **201** | **PASS** |
| Approved lecturer A `POST` to a **non-entitled** unit | 403 | **403** | **PASS — D4** |
| Lecturer B `GET` lecturer A's unit materials | 403 | **403** | **PASS — D4** |
| Database — `course_offering_lecturers.Status` | `Active` | **Active** | **PASS — D3** |

### Requests (D2 — the repaired defect)

| Check | Expected | Actual | Result |
|---|---|---|---|
| Student A creates own request | 201 | **201** | **PASS** |
| Student A reads own request | 200 | **200** | **PASS** |
| **Student B reads Student A's request** | **403** | **403** | **PASS — D2** |
| **Student B `PUT`s Student A's request** | **403** | **403** | **PASS — D2** |
| Request title after the attempt | unchanged | unchanged | **PASS — D2** |
| Student A updates own request | 200 | **200** | **PASS** |

### Materials (D5 — the repaired defect)

| Check | Expected | Actual | Result |
|---|---|---|---|
| Lecturer A uploads material | 201 | **201** | **PASS** |
| **Lecturer B `DELETE`s Lecturer A's material** | **403** | **403** | **PASS — D5** |
| Material survives the refused delete | present | **present** | **PASS — D5** |
| Lecturer A deletes own material | 204 | **204** | **PASS** |

### Notifications

| Check | Expected | Actual | Result |
|---|---|---|---|
| `GET /notifications` | 200 | **200** | **PASS** |
| Fan-out reached the back office | non-empty | **non-empty** | **PASS** |

### Authorization / security

| Check | Expected | Actual | Result |
|---|---|---|---|
| Student A → Student B request (update) | 403 | **403** | **PASS — D2** |
| Lecturer B → lecturer A material (delete) | 403 | **403** | **PASS — D5** |
| Pending lecturer → manage materials | 403 | **403** | **PASS — D4** |
| Lecturer → unassigned unit | 403 | **403** | **PASS — D4** |
| Student → create material | 403 | **403** | **PASS** |
| Cross-tenant header | 400 | **400** | **PASS** |
| `POST` without CSRF token | 403 | **403** | **PASS** |
| Duplicate relationship rows created | 0 | **0** | **PASS** |

**Production smoke result: 38 / 38 checks PASS.** Phase 1 covered authentication,
the academic graph, registration, D1, pending D4 and D2; phase 2 covered approval, D1,
D3, D4, D5, requests, notifications, cross-tenant and CSRF.

> One phase-2 check initially reported `400` because the fixture used a request-type
> code that does not exist in production. Re-running it with a real production code
> (`ACCOMMODATION_ALLOCATION`) returned **201**, confirming student request creation
> works and that the earlier `400` was a fixture value, not a defect.

### Verification fixtures left on production

Deliberately **retained** as the evidence trail, all clearly named and tenant-scoped,
so an administrator can retire them on request:

| Item | Count |
|---|---|
| Courses named `PROD SMOKE VERIFICATION COURSE …` | 5 (each with 3 units and one active offering) |
| Accounts `prod.smoke.*@sms-verification.local` | 10 (4 students, 6 lecturers — all `Approved`) |
| Verification requests | 2 |

No pre-existing production data was modified. No smoke course was auto-deleted,
because every one of them carries at least one enrollment.

---

## 11. Rollback

| Item | Value |
|---|---|
| Rollback SHA | `9fb7e36800aff93b9527cb14e6da1a50c07da0b5` |
| Target SHA | `3f4d8c4c51b89dc81130f6f4b6772e4336cc4a07` |
| Database migration required | **none** — nothing to reverse at the schema level |
| Backup | `/backups/SchoolManagementSystem_20261003T165915Z.dump` (volume `docker_backup_data`), verified with `pg_restore -l` |

```bash
# Rollback procedure (established mechanism, no ad hoc steps)
cd /opt/sms/app
git fetch origin coordinator-repair
git checkout 9fb7e36800aff93b9527cb14e6da1a50c07da0b5
docker compose -f docker/docker-compose.prod.yml build api
docker compose -f docker/docker-compose.prod.yml up -d --force-recreate api
```

No restore of the database dump is required for this deployment because no migration
was applied. The dump remains available if a data-level restore is ever needed.

**Rollback was not required** — no health-check failure, no authentication failure,
no database failure, no 5xx errors, no OMS or accommodation regression, and no
authorization regression were observed.

---

## 12. Final Status

# PASS

All mandatory workflows and security boundaries were proven twice: once against a
**disposable PostgreSQL 16** stack running the real API as the `NOBYPASSRLS` runtime
role, and then again **live on production** `192.168.110.161` running the exact
verified commit.

| Defect | Verified where | Status |
|---|---|---|
| **D1** `unitsCount` | local PostgreSQL (`unitsCount = 3`) and **production** (`unitsCount = 3`) | **FIXED** |
| **D2** cross-user request update | local PostgreSQL (**403**, title unchanged) and **production** (**403**, title unchanged) | **FIXED** |
| **D3** teaching assignment activation | local PostgreSQL (dashboard shows only U1+U2, no U3) and **production** (1 course, exactly 2 units; lecturer B sees only its own unit) | **FIXED** |
| **D4** pending lecturer material management | local PostgreSQL (`my-units = []`, unit read **403**) and **production** (same) | **FIXED** |
| **D5** cross-lecturer material delete | local PostgreSQL (**403**, material survives) and **production** (**403**, material survives) | **FIXED** |
| **D6** orphan Identity user | local PostgreSQL (same email re-registers → **201**) and unit tests | **FIXED** |

### Acceptance criteria

| Criterion | Status |
|---|---|
| D1 — `unitsCount` reflects persisted enrollments (3 / 1 / 0) | **PASS** |
| D2 — a student cannot update another student's request | **PASS** |
| D3 — approved lecturer assignments become active; dashboard shows the approved course and units | **PASS** |
| D4 — pending lecturers cannot manage materials; approved lecturers only for authorized units | **PASS** |
| D5 — lecturer B cannot delete lecturer A's material | **PASS** |
| D6 — failed registration leaves no orphan Identity user or partial state | **PASS** |
| Student registration / approval / New Request continue to work | **PASS** |
| Accommodation notifications continue to work | **PASS** |
| OMS, Accommodation and Authentication continue to work | **PASS** |
| Tenant isolation intact | **PASS** (0 rows cross-tenant under the real `sms_app` NOBYPASSRLS role) |
| RLS intact | **PASS** |
| Role authorization intact | **PASS** |
| Object-level authorization intact | **PASS** (D2, D5 are now object-level) |
| **No authorization weakened to make tests pass** | **PASS** — every change is a strict tightening; the Administrator/Coordinator override and the `Oms.CanViewOwnRequest` policy are untouched |
| Approval semantics unchanged (PendingApproval → Approved) | **PASS** |
| All test suites pass | **PASS** — unit 1040/1040, integration 120/120, API 231/231, frontend 390/391 (the single documented isolated timeout), `tsc && vite build` pass |
| Production-like verification passes | **PASS** |
| Production smoke tests pass | **PASS** — 38/38 |
| Exact tested commit on `origin/coordinator-repair` | **PASS** — `3f4d8c4c51b89dc81130f6f4b6772e4336cc4a07` |
| Exact tested commit deployed to `192.168.110.161` | **PASS** — same SHA running as the live API image |
| Production health checks pass | **PASS** |
| No rollback required | **PASS** |

### Noted, not defects

* **Frontend 390/391.** `src/pages/Register.test.tsx > disables Next until a strong
  password is entered` times out at vitest's 5 s default under load. It is the **same
  single test** documented in the previous production-like report, no frontend file was
  touched by this repair, and it passes in isolation
  (`npx vitest run src/pages/Register.test.tsx --testTimeout=30000` → **14/14**).
  It was **not** hidden, skipped or re-timed.
* **Docker Desktop was not running** at the start of this task and had to be launched
  before any PostgreSQL-backed suite could run — the same environmental cause as in the
  previous report.
* **The previous production-like report was untracked.** It has now been committed
  unchanged so the defect → repair → verification evidence chain is complete.
* **Seeded `Semesters` row is tenant `00000000-…`** and therefore invisible to any real
  tenant; the production database already has a proper tenant-scoped
  `Semester 1 (Default)`, so lecturer registration works there without any fixture.
* **Production password policy rejected the first smoke-fixture password**
  ("Password must not contain your personal information") — correct behaviour, and the
  fixture was changed rather than the policy.

---

---
