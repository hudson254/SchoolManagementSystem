# Production Repair Report — Registration Course & Unit Verification

**Date:** 2026-09-28
**Branch:** `coordinator-repair`
**Baseline:** `0b0aa60` (reconciled production baseline)
**Scope:** Public registration wizard (`/register`) — Student and Lecturer course/unit verification and persistence.

---

## 1. Root cause

The verification ("Review & Submit") step **already existed** in `Register.tsx`. It was
not routed away, not skipped by a conditional, and it did not lose state. It was
**structurally incapable of showing units**, because the unit data path did not exist
anywhere in the chain:

```
Registration UI → React state → Course selection → Unit API
               → Verification component → Submit handler → CQRS command
               → Handler → Repository → EF Core → PostgreSQL
```

The break was at **two independent points**.

### 1a. No readable unit source for a pre-login registrant

| Endpoint | Policy | Reachable by a registrant? |
|---|---|---|
| `GET /api/v1/courses/{id}/units` | `ModeratorAccess` | ❌ 403 |
| `GET /api/v1/enrollment/available-courses/{id}/units` | `StudentAccess` | ❌ 401/403 (no account yet) |
| `GET /api/v1/auth/active-courses` | `AllowAnonymous` | ✅ but **returns courses only** |

The wizard sits at `/register` and runs **before** any account exists. No endpoint it
was permitted to call could return a course's units, and `Register.tsx` contained **no
unit-fetching code at all** — the only call it made was `/auth/active-courses`.

### 1b. The command had no unit field, and discarded the lecturer's course

- `RegisterCommand` had `CourseId` but **no `UnitIds` property**. A unit selection
  could not even be expressed in the request DTO.
- `CreateLecturerRecord` **silently discarded `request.CourseId`**. The lecturer's
  course was accepted by the UI and the validator, then thrown away. No
  `UnitAllocation` row was created, so a newly registered lecturer had **no teaching
  assignment at all** — exactly the reported symptom. (`IUnitRepository` and
  `IUnitAllocationRepository` were injected into the handler but unused.)
- `CreateStudentRecord` persisted `SelectedCourseId` but created **no `Enrollment`
  rows**, so the student had no unit-level record to verify.

**Net effect:** the review page rendered, but with course only. Units could not be
shown because they were never fetched, never carried in the payload, and never stored.

---

## 2. Existing flow (before)

**Student** — `Personal → Contact → Account → Course Selection → Review & Submit`
Course step rendered a course `<Select>` only. Review step rendered
`Course Selection: {name} {code} · {duration} months · {credits} credits` and **no units**.

**Lecturer** — `Personal → Contact → Professional → Account → Course Assignment → Review & Submit`
Course step rendered a course `<Select>` only — **no unit picker existed at all**.
Review step rendered the specialization and course name — **no units**.

---

## 3. Corrected flow (after)

**Student**
```
Personal → Contact → Account → Select Course → (units auto-load)
          → Review: account + course + ALL units → Confirm → Account created
```

**Lecturer**
```
Personal → Contact → Professional → Account → Select Course
          → Select All / individual units
          → Review: account + course + SELECTED units only → Confirm → Account created
```

Both keep the existing step order, router, `useForm`/zod state and `apiClient`; no new
framework, no new state library, no new application.

---

## 4. Student course & unit verification behaviour

The existing SMS business rule is *"a student is enrolled in **all** active units of the
selected course"* (see `SubmitStudentEnrollmentCommand`). That rule is **preserved and
not narrowed**:

- Selecting a course loads its units from `GET /auth/active-courses/{courseId}/units`.
- The wizard pre-selects every unit, and the review step states
  *"You will be enrolled in all N active units of this course."*
- The student never picks units manually.
- The submitted `unitIds` are the full verified set. The handler resolves the
  authoritative set server-side; a client that sent a **different, non-empty** set is
  logged as a warning and the authoritative set wins. The payload is never allowed to
  narrow an enrollment.

## 5. Lecturer course & unit selection behaviour

- An explicit **"Select All Units (N)"** checkbox plus per-unit checkboxes.
- The review step lists **only the ticked units** and shows
  *"You will be assigned to teach X of N available units."*
- The final submission sends exactly `verifiedUnits`, and the handler writes exactly
  those ids.

**Multiple courses:** the existing domain supports it —
`SubmitLecturerTeachingAssignmentCommand` already accepts `List<Guid> CourseIds`. That
capability is **not reduced**; it is left untouched, and the registration path adds
single-course selection to the wizard, which is what the UI already offered.

---

## 6. API changes

**One new endpoint** (additive, anonymous, tenant-scoped):

```
GET /api/v1/auth/active-courses/{courseId:guid}/units   [AllowAnonymous]  → 200 / 404
```

Backed by `GetCourseUnitsForRegistrationQuery`. It deliberately sits beside
`/auth/active-courses` because it serves the same anonymous registration page, and is
kept separate from the `ModeratorAccess`/`StudentAccess` unit endpoints so **no existing
authorization boundary is weakened**.

`POST /api/v1/auth/register` gains an optional `unitIds: string[]` (required for
lecturers). No other request or response shape changed.

**Response envelope:** the endpoint returns a **bare JSON array**, not the paged
`{ items, totalCount, page, pageSize }` envelope. `registrationService.getCourseUnits`
normalizes defensively — a bare array is used directly, a paged envelope is unwrapped
via `.items`, anything else yields `[]` and the caller surfaces a real error state
rather than silently rendering an empty list.

---

## 7. Database changes

**None. No migration was required or created.**

Verified with `dotnet ef migrations has-pending-model-changes`:

> *No changes have been made to the model since the last migration.*

The repair reuses the existing schema exactly as required ("use the existing domain
model"):

| Relationship | Existing table | Used for |
|---|---|---|
| Student → Course | `Students.SelectedCourseId` (migration `20260926183852`) | student course choice |
| Student → Unit | `Enrollments` | created by the existing enrollment command |
| Lecturer → Unit | `UnitAllocations` | lecturer teaching assignment (newly written) |
| Course → Unit | `units.course_id` | the authoritative unit set |

---

## 8. Backend validation

All server-side, in `RegisterCommandHandler`:

| Rule | Enforcement |
|---|---|
| Course exists, active, not deleted | `ResolveCourseAsync` → `NotFoundException` |
| Course belongs to caller's tenant | tenant global query filter on the repository |
| Course with no active units is rejected (lecturer) | `CreateLecturerRecord` → `ValidationException` |
| Every submitted unit belongs to the selected course | `foreignUnitIds` check → `ValidationException` + warning log |
| Lecturer must choose ≥ 1 unit | validator + handler |
| No duplicate / empty unit ids | validator |
| Semester FK never dangling | `ISemesterRepository.GetCurrentOrDefaultAsync`; rejects with a clear message instead of a 500 |

All browser-supplied ids are treated as untrusted. Tenant filtering and existing
authorization are untouched.

---

## 9. Tests performed

Commands are the repository's own (from `SchoolManagementSystem.sln` and `package.json`).

### Backend

| Suite | Command | Result |
|---|---|---|
| Build | `dotnet build SchoolManagementSystem.sln -c Release` | **0 errors** |
| Unit | `dotnet test tests/SMS.UnitTests -c Release` | **664 passed / 0 failed** |
| API | `dotnet test tests/SMS.ApiTests -c Release` | **165 passed / 0 failed** |
| Integration | `dotnet test tests/SMS.IntegrationTests -c Release` | **55 passed / 0 failed** |
| OMS subset | `--filter FullyQualifiedName~OMS` | **209 passed / 0 failed** (OMS intact) |
| Migration drift | `dotnet ef migrations has-pending-model-changes` | **no changes** |

Added `GetCourseUnitsForRegistrationQueryHandlerTests` (5 cases) and new cases in
`RegisterCommandTests` (validator rules, lecturer unit persistence, foreign-unit
rejection, unknown-course rejection, student unit-ids optionality).

### Frontend

| Command | Result |
|---|---|
| `npx tsc --noEmit` | **0 errors** |
| `npx vitest run` (Register) | **14 passed** (4 pre-existing + 10 new) |
| `npx vitest run` (rest of suite) | **262 passed / 25 files** |
| `npm run build` | **success** (production bundle emitted) |

The 10 new tests cover: student unit loading; course + unit display on the review step;
submission of the full verified set; going back without losing the course; lecturer
select-all; individual selection; "only selected units" preview; exact submission;
blocking submission with an empty selection; API failure; empty-unit-list handling.

**`npm run lint` could not be run:** ESLint 8.57.1 reports *"ESLint couldn't find a
configuration file"*. No `.eslintrc*` / `eslint.config.*` has ever been committed to this
repository — pre-existing and unrelated to this repair. No lint config was invented and
no lint/compiler suppression was added.

---

## 10. Production validation

Verified against a live API + PostgreSQL (`GET /health` → `Healthy`, DB reachable).

**End-to-end proof that the verified selection is the persisted record** — an
integration test drives the real HTTP pipeline, reads the unit list the review page
renders, submits it, then reads PostgreSQL back:

- `LecturerRegistration_PersistsExactlyTheVerifiedUnits` — the ids returned by
  `/auth/active-courses/{id}/units` and submitted by the client are compared against
  the `UnitAllocations` rows actually written. **Passes.**
- `LecturerRegistration_RejectsAUnitFromAnotherCourse` — 400, **and no lecturer row is
  left behind** (tamper test).
- `LecturerRegistration_WithoutUnits_IsRejected` — 400.
- `StudentRegistration_PersistsTheCourseShownOnTheReviewStep` — the verified course is
  the course stored on `Students.SelectedCourseId`.
- `ModeratorOnlyEndpoints_RemainClosedToStudents` — `/courses` and
  `/courses/{id}/units` still return **403** to a Student token.

Independent `psql` confirmation against the database:

```
Email                                                     | course | allocations
----------------------------------------------------------+--------+-------------
lect.verify.cabc1a4012d84c8fbca0d5d5656a72ba@example.com  | SSC101 |           1
lect.verify.6005342b891b44bc927662ac4a501c15@example.com  | SSC101 |           1
lect.verify.1e41c141100d43d3acfc566e4dec39cd@example.com  | SSC101 |           1

tamper_leftovers : 0   -- rejected registrations persisted nothing
verify_students  : 3   -- students with a persisted SelectedCourseId
```

Deployment impact:

- **Schema compatible** — no migration.
- Docker build / container start / `/health` / login / existing dashboards: **not
  deployed or exercised in this environment.** Docker Desktop's daemon is not running
  here (`npipe://...dockerDesktopLinuxEngine` unavailable), so these are listed as an
  outstanding deployment step rather than claimed as done.
- OMS functionality: unaffected — 209 OMS tests pass and no OMS file was touched.

---

## 11. Files changed

| File | Reason |
|---|---|
| `src/SMS.Application/Features/Auth/Commands/RegisterCommand.cs` | `UnitIds` field + validation; authoritative `ResolveCourseAsync`; lecturer course/units now persisted; semester FK resolved safely |
| `src/SMS.Application/Features/Courses/Queries/GetActiveCoursesForRegistrationQuery.cs` | New `GetCourseUnitsForRegistrationQuery` — the anonymous, tenant-scoped read path |
| `src/SMS.API/Controllers/v1/AuthController.cs` | Exposes `GET /auth/active-courses/{id}/units` |
| `frontend/sms-web/src/services/registration.service.ts` | **New** — registration API calls, envelope normalization |
| `frontend/sms-web/src/pages/Register.tsx` | Unit loading/caching, lecturer unit picker, unit lists on both review steps, gating |
| `frontend/sms-web/src/contexts/AuthContext.tsx` | `unitIds` on `RegisterData` |
| `frontend/sms-web/src/types/user.types.ts` | `RegisterRequest.courseId`/`unitIds`, `RegistrationUnit` |
| `frontend/sms-web/src/services/enrollment.service.ts` | Import restored after extracting the new service |
| `tests/SMS.UnitTests/Auth/RegisterCommandTests.cs` | Constructor update + new validator/handler tests |
| `tests/SMS.UnitTests/Enrollments/StudentCourseSelectionPersistenceTests.cs` | Constructor update |
| `tests/SMS.UnitTests/Courses/GetCourseUnitsForRegistrationQueryHandlerTests.cs` | **New** — query handler tests |
| `tests/SMS.ApiTests/Controllers/StudentCourseSelectionApiTests.cs` | Endpoint, authorization and persistence round-trip tests |

---

## 12. Remaining limitations

1. **Docker / production container checks not performed** — the Docker daemon is not
   available in this environment. Run `docker build`, container start, `/health` and the
   login/dashboard smoke test as the deployment step.
2. **`npm run lint` is unrunnable** as shipped (no ESLint config in the repo). If lint
   must gate CI, the config has to be added as a separate change.
3. **Administrator-created accounts** (`AddStudentPage` / `AddLecturerPage`) were left
   untouched. They post to the staff create endpoints, not `/auth/register`, and have no
   registration wizard. If staff must also get a course/unit verification step, that is a
   separate, additive change.
4. **Student `Enrollment` rows are still created by `SubmitStudentEnrollmentCommand`**
   (the post-login wizard), not at registration. This preserves the existing two-phase
   lifecycle and the dashboard reconciliation a previous repair carefully tuned. The
   review page therefore shows the units that *will* be enrolled, resolved from the
   identical repository filter that command uses. The verified course is persisted
   immediately on `Students.SelectedCourseId`.
5. **A lecturer with no academic period configured** is rejected with a clear message
   rather than persisted with a dangling `UnitAllocation.SemesterId` FK.
