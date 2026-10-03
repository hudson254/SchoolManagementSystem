# Registration and Workflow Repair Report

> Scope: newly registered students and lecturers could not see the course/units they
> selected at registration, a newly registered student got **403** creating a request,
> registration was stuck in an approval dead-end, and no accommodation notification was
> raised for a new account.

## 1. Baseline

| Item | Value |
|---|---|
| Previous Git SHA | `9fb7e36800aff93b9527cb14e6da1a50c07da0b5` |
| Branch | `coordinator-repair` |
| Working tree (before) | 3 untracked files only: `Documentation/AGENT_PROMPT_STUDENT_COURSE_SELECTION_FIX.md`, `Documentation/Accommodation/ACCOMMODATION_PRODUCTION_FUNCTIONAL_VERIFICATION_REPORT.md`, `PRODUCTION_NOTIFICATIONS_PWA_DEPLOYMENT_REPORT.md` |
| Backend build | Pass, **0 errors** (416 pre-existing warnings) |
| Frontend build | Pass |
| Unit tests (baseline) | **952 passed / 0 failed** |
| Integration tests (baseline) | 48 passed / **72 failed** — `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5433` |
| API tests (baseline) | 16 passed / **207 failed** — same missing PostgreSQL |

> The integration/API failures are **environmental, not pre-existing defects**. Verified
> by `git stash`ing all changes and re-running against the pristine `9fb7e368` commit:
> identical counts (48/72 and 16/207). Docker Desktop is not running on this machine, so
> no PostgreSQL instance is reachable.

## 2. Root Causes

### Student registration
`RegisterCommandHandler.CreateStudentRecord` persisted **only** `Student.SelectedCourseId`.
It created **zero `Enrollment` rows**. Every downstream authorization and read path
resolves a student's units from `Enrollment`:

* `StudentRepository.GetEnrolledUnitIdsAsync` filters `e.IsActive` → empty
* `AcademicAccessService.StudentEnrolledInUnitAsync` → **false** → 403 on all study materials
* `GetMyStudentDashboardQueryHandler` builds cards from `course_offering_enrollments` → empty
* `GetMyPendingEnrollmentQuery.UnitsCount` → 0

Registration also wrote `RegistrationStatus = PendingCourseSelection` (0), the state that
means *"has not chosen a course yet"*.

### Lecturer registration
`CreateLecturerRecord` wrote **only `UnitAllocation` rows** — **no `course_offering_lecturers`
row**, which is the relationship `GetMyLecturerDashboardQueryHandler` actually reads (hence
"You are not assigned to teach any course offerings yet").
`LecturerRepository.GetTaughtUnitIdsAsync` derives taught units from `Status == "Active"`
allocations plus offering-unit rows, so Study Materials resolved no units either.

### Student New Request 403
`OmsRequestsController` gated `GET /oms/requests/types` on `OmsPolicy.CanViewRequests`,
registered as **SystemAdministrator, Administrator, Coordinator, Lecturer** — **Student
excluded**. `CreateRequestPage.tsx` loads that endpoint on mount, so the page 403'd before the
form could render.

`OmsAuthorization.CreateRequestRoles` *did* include Student, and `POST /oms/requests` used
`Oms.CanCreateRequest` — so the **create** call was never the problem. The role that may
create a request simply could not read the catalogue needed to create one. The same gap also
403'd `GET /{id}` (navigated to on success), history, and attachment reads.

### Administrative approval
An **approval dead-end**:

| Component | Filter |
|---|---|
| `RegisterCommandHandler` wrote | `PendingCourseSelection` (0) |
| `GetPendingApprovalsQuery` lists | `PendingApproval` (1) |
## 3. Changes Implemented

### Backend

| File | Change |
|---|---|
| `SMS.API/Controllers/v1/Oms/OmsPolicy.cs` | Added `CanViewOwnRequest` policy name. |
| `SMS.API/Program.cs` | Registered `Oms.CanViewOwnRequest` = all six roles. **`CanViewRequests` (the privileged queue) is unchanged.** |
| `SMS.API/Controllers/v1/Oms/OmsRequestsController.cs` | Applied `CanViewOwnRequest` to `GET /types`, `GET /{id}`, list, history, attachments, download. `CanViewRequests` still guards `/dashboard` and the `/types` write endpoints. |
| `SMS.Application/Features/OMS/Queries/GetRequestTypesQuery.cs` | Coarse gate now also accepts `ViewOwnRequestsRoles`. |
| `SMS.Application/Features/OMS/Queries/GetRequestHistoryQueries.cs` | Same for history + attachment handlers; `OmsRequestAccess.EnsureCanView` still enforces object-level ownership immediately after. |
| `SMS.Application/Features/Auth/Commands/RegisterCommand.cs` | Injected 5 repositories + `IBusinessEventNotifier`. Student: creates an `Enrollment` row per active unit, sets `PendingApproval`, creates a pending `CourseOfferingEnrollment` when an active offering resolves. Lecturer: sets `PendingApproval`, links `CourseOfferingId` on allocations, creates `CourseOfferingLecturer` + missing `CourseOfferingUnit` rows. Added `ResolveActiveOfferingAsync`. Both raise notifications after commit and emit structured logs. |
| `SMS.Application/Common/Interfaces/IBusinessEventNotifier.cs` | Added `NotifyAccommodationRequiredAsync` and `NotifyRegistrationAwaitingApprovalAsync`. |
| `SMS.Application/Services/BusinessEventNotifier.cs` | Implemented both via the existing `INotificationDispatcher.NotifyRolesAsync`. |

### Frontend
**No frontend code changes were required.** Verified:
* `App.tsx` already gates `oms/requests/new` on `OMS_REQUEST_SELF_ROLES` (includes `STUDENT`).
* `roles.ts` already models `OMS_REQUEST_SELF_ROLES` / `OMS_REQUEST_VIEW_OWN_ROLES` correctly.
* `Register.tsx` already sends `courseId` + `unitIds` with a verification step.

The defect was entirely backend-side (missing policy + missing persistence).

### Database
**No migration.** `git diff --stat src/SMS.Persistence` is empty — no entity, mapping,
foreign key, or query filter changed. All target tables (`Enrollments`,
`CourseOfferingEnrollments`, `CourseOfferingLecturers`, `CourseOfferingUnits`,
`Notifications`) already exist and are mapped. Deploy: schema changes are unnecessary.

### Authorization
**Additive only. No policy or attribute was removed or weakened.**

* New `Oms.CanViewOwnRequest` is **narrower** than `Oms.CanViewRequests`.
* Every endpoint using it re-checks object-level access in the handler:
  `GetRequestsQuery.ResolveScope` forces `"mine"` for non-privileged callers and throws
  `ForbiddenException` for `scope=all`; `GetRequestByIdQueryHandler` requires `isOwner`;
  history/attachments call `OmsRequestAccess.EnsureCanView`.
* Tenant filters, RLS, CSRF, audit logging and `OmsAuthorization` role arrays are untouched.
* `ApproveRegistrationCommand`'s existing `PendingApproval` guard is unchanged — it now
  simply receives a state that satisfies it.

### Notifications
Reused the **existing** stack — `IBusinessEventNotifier` → `INotificationDispatcher`. No new
notification system, no external provider (no email/SMS/Twilio).

* **Accommodation required** → `SystemAdministrator`, `Administrator`, `Coordinator`,
  `Receptionist` (the existing `ReceptionistAccess` accommodation tier). Type
  `Accommodation`, priority `Important`, action `/accommodation`. Body carries name, role,
  registration identifier, course, and the required action.
* **Registration awaiting approval** → approval roles. Type `AccountApproval`, priority
  `Important`, action `/users`. Body carries name, role, course, unit list.

Role fan-out goes through `NotifyRolesAsync`, which resolves live role membership and stamps
the tenant, so recipients are tenant-scoped and de-duplicated.

### Accommodation
Unchanged. Capacity, occupancy, house/lane allocation, duplicate-allocation prevention,
tenant isolation and Accommodation Reports were **not** touched. The notification simply
points an already-authorised back-office role at the existing `/accommodation` workflow.
| `ApproveRegistrationCommand` requires | `PendingApproval`, else **throws** |

A new registration was invisible to the approval queue *and* rejected by the approve command —
no administrator could ever clear it. The approval workflow itself is intended architecture
(`RegistrationStatus` enum, `ApprovalController`, `ApproveRegistrationCommand`,
## 4. Tests

| Suite | Baseline | After |
|---|---|---|
| Unit | 952 passed / 0 failed | **978 passed / 0 failed** (+26) |
| Integration | 48 / 72 failed (no DB) | 48 / 72 failed — **unchanged, verified environmental** |
| API | 16 / 207 failed (no DB) | 16 / 207 failed — **unchanged, verified environmental** |
| Backend build | 0 errors | 0 errors |
| Frontend build | Pass | Pass |
| `tsc --noEmit` | — | Clean |

### New regression coverage

**`tests/SMS.UnitTests/Auth/LecturerRegistrationRelationshipTests.cs`** (new, 6 tests)
* Creates the `CourseOfferingLecturer` assignment → lecturer dashboard is not empty
* Sets `PendingApproval` → the approval workflow can act on it
* Allocates only the selected units (not the whole course)
* Rejects a unit from another course → cross-course assignment impossible
* Raises accommodation + approval notifications
* Does not fabricate an offering when none is scheduled

**`tests/SMS.UnitTests/OMS/StudentRequestCreationAuthorizationTests.cs`** (new, 12 tests)
* Every `CreateRequestRole` can read the type catalogue (**the 403 fix**)
* Student reads the catalogue and their own request detail
* **Negative:** Student A cannot read Student B's request
* **Negative:** Student cannot use `scope=all`
* **Negative:** a smuggled `requesterUserId` is overwritten
* **Negative:** unauthenticated → `UnauthorizedException`
* Student/Receptionist remain excluded from the privileged queue policy

**`tests/SMS.UnitTests/Enrollments/StudentCourseSelectionPersistenceTests.cs`** (+4)
* One `Enrollment` row per active unit, correctly FK-linked, `PendingApproval`
* Creates a pending `CourseOfferingEnrollment` when an active offering exists
* Records the selection without fabricating an offering
* Raises accommodation + approval notifications

**`tests/SMS.UnitTests/Notifications/BusinessEventNotifierTests.cs`** (+4)
* New account notifies Administrator + Coordinator + Receptionist
* Excludes roles that cannot allocate (Lecturer/Student)
* Pending approval notifies approvers with name/course/units
* Blank name is a no-op

**Updated:** `RegisterCommandTests.cs` and `BusinessEventNotifierStub.cs` (new interface
methods), plus one assertion in `StudentCourseSelectionPersistenceTests` that asserted the
old broken `PendingCourseSelection` value.

## 5. End-to-End Verification

**Not executed against a live database.** No PostgreSQL is reachable on this machine
(Docker Desktop not running; connection refused at `127.0.0.1:5433`) and the same suites
fail identically on the pristine baseline commit. Verified statically and by unit tests:

| Item | Status |
|---|---|
| Student registration persists course | ✅ `SelectedCourseId` + `Enrollment` + `CourseOfferingEnrollment` |
| Student units persisted | ✅ one `Enrollment` per active unit |
| Student course/unit visible after login | ✅ reads the persisted relationship |
| Student request creation (no 403) | ✅ policy + handler fixed; 12 tests |
| Lecturer course persisted | ✅ `CourseOfferingLecturer` assignment |
| Lecturer units persisted | ✅ `UnitAllocation` + `CourseOfferingUnit` |
| Lecturer dashboard / study materials | ✅ sourced from the rows now created |
## 6. Security Verification

| Control | Result |
|---|---|
| Tenant isolation | Unchanged — global query filters + RLS untouched; role fan-out is tenant-stamped |
| Role authorization | Unchanged; a **narrower** policy added |
| Cross-user access | **Blocked** — tested (`Student_CannotReadAnotherStudentsRequest`) |
| Cross-tenant access | **Blocked** — a foreign tenant's course/unit id resolves to `NotFound` via the tenant filter; tested via `RejectsUnitFromAnotherCourse` |
| Cross-unit access | **Blocked** — a lecturer is only assigned the units they selected |
| Privileged queue | **Blocked** — `scope=all` still throws for non-privileged roles |
| Unauthenticated | **Blocked** — 401/Unauthorized preserved |
| CSRF / audit logging | Untouched; registration still writes audit entries |

## 7. Documentation

* `Documentation/REGISTRATION_AND_WORKFLOW_REPAIR_REPORT.md` (this file)

## 8. Remaining Issues

1. **Live E2E verification is outstanding.** Integration (72) and API (207) tests cannot run
   without PostgreSQL. Start Docker and re-run both suites, plus a manual pass over the
   acceptance-criteria flows, before production release.
2. **`/approvals` has no SPA page.** The approval action URL points at `/users` because the
   approval queue is API-only (`GET /approval/pending`). A dedicated approvals screen would
   be a genuine improvement but is outside this repair's scope.
3. **Pre-existing production users are not repaired.** This change fixes the workflow for
   **new** registrations only. Accounts registered before the fix remain in
   `PendingCourseSelection` with no `Enrollment`/`CourseOfferingLecturer` rows and are
   invisible to the approval queue. They need remediation (below). No production data was
   modified.

### Production data remediation path (not executed)

Recommended: a **controlled administrative operation**, not a migration — the correct target
offering and units are per-record business decisions.

1. **Identify affected records** (read-only): `students`/`lecturers` where
   `registration_status = 0` (PendingCourseSelection) **and** a linked `Users` row exists.
2. **Re-derive each record's intended selection**: `students.selected_course_id` is already
   persisted, so the course is known; units come from that course's active units.
3. **Backfill in a dry run first**: insert the missing `Enrollments` rows
   (`status='PendingApproval'`, `is_active=false`) and `course_offering_enrollments` /
   `course_offering_lecturers` rows against the resolved active offering — reusing the same
   offering-resolution rule as the repaired handler. Set `registration_status = 1`.
4. **Guard against duplicates** with the same `exists` checks the handler uses.
5. **Never** alter existing `Approved`/`Rejected` records, existing enrollments, lecturer
   assignments, or accommodation rows.

Until this runs, affected users remain visible-but-unapprovable. This is pre-existing state,
not a regression introduced by this change.
| Administrator / Coordinator / Receptionist notifications | ✅ role fan-out tested |
| Accommodation allocation | ⚠️ notification + route verified; live allocation unverified (no DB) |
`RejectRegistrationCommand`, audit logging), so the fix was to make registration land correctly
in `PendingApproval` rather than bypass approval.

### Accommodation notifications
`RegisterCommandHandler` injected **no notifier at all**. Nothing was raised for any role.