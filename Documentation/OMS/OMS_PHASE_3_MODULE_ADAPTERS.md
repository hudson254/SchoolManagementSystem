# OMS Phase 3 — Module Adapters, Production Reconciliation, Frontend Client

## 1. Outcome

Phase 3 added **thin module request adapters** for the modules that genuinely
have an approval/exception workflow, seeded their request types, added
integration tests, and reconciled production.

**The OMS Request Core remains the single workflow engine.** No module gained a
second status machine, a duplicate request entity, its own audit trail, its own
notification system, or its own attachment storage.

The owning SMS module remains authoritative for its own business data. An
approved request does **not** apply the business change; a coordinator still
performs it through the module's existing commands.

## 2. Module workflow audit

| Module | Genuine workflow found in the repository | Request use case | Existing owner | Adapter |
| --- | --- | --- | --- | --- |
| Enrollment | Yes — `Approvals/ApproveRegistrationCommand`, `Enrollments/UpdateEnrollmentStatusCommand` (both mutate state directly) | Exception / course change / unit correction / cancellation a requester cannot self-serve | `Enrollment` | **Yes** |
| Accommodation | Yes — `TransferRoomCommand`, `ReassignHouseCommand` (privileged moves) | Transfer / allocation / exception / correction | `Accommodation` | **Yes** |
| Assignment | Partial — `SubmitAssignmentCommand` already owns submission & publication | Deadline extension, reopen, correction, exception | `Assignment` | **Yes** |
| Assessment | Yes — `ReviewMarksHandler`, `SubmitForReviewHandler` own grading rules | Not implemented in this phase | `Assessment` | **No** |
| Certificate | Certificate generation already owned by the certificate module | Not implemented in this phase | `Certificates` | **No** |
| Timetable / Calendar | No administrative request workflow found | Not implemented | `Timetable` / `Calendar` | **No** |

Modules deliberately **not** given an adapter, with reasons:

- **Assessment** — grading authorization and mark rules are already enforced
  inside `ReviewMarksHandler` / `SubmitForReviewHandler`. An adapter would add
  a workflow in front of a workflow. Needs a real exception path to be defined
  by the module owner first.
- **Certificate** — generation is already owned by the certificate module. A
  "correction request" needs a defined correction policy (what may be corrected
  after issuance) before it can be modelled.
- **Timetable / Calendar** — no administrative request workflow exists in the
  module. Creating one purely because the module exists was explicitly out of
  scope.

## 3. Adapter design rules

An adapter **may**:

- validate module-specific input
- resolve module business context
- authorize the requester against the module's own rules
- choose the RequestType from its own allow-list
- populate typed Request relationships and `RelatedEntityId`/`RelatedEntityType`
- delegate to the generic Request create command
- return the generic Request DTO

An adapter **must not**:

- implement its own status machine
- write its own history, comments, attachments, notifications or audit
- mutate module data
- bypass OMS authorization or tenant isolation

### The delegation seam

Every adapter calls exactly one method:

```csharp
IOmsRequestModuleAdapter.CreateAsync(CreateRequestCommand, CancellationToken)
```

`OmsRequestModuleAdapter` is a pure pass-through that dispatches the generic
`CreateRequestCommand` through MediatR, so an adapter inherits the same
validation, logging, RequestType validation, number generation, tenant
stamping, authorization and audit as the public generic endpoint. There is
deliberately no alternate persistence path: an adapter that bypassed this seam
would be building a second workflow engine.

Shared adapter primitives live in
`SMS.Application/Features/OMS/Services/ModuleRequestAdapterSupport.cs`
(authenticated-requester check, reference resolution, module-specific
authorization, request-type allow-listing, title defaulting).

## 4. Adapter endpoints

These endpoints **only create a request**. They expose no approve/reject/
assign/cancel verbs; the whole lifecycle is served by the generic endpoints.

```text
POST /api/v1/oms/requests/enrollment     { enrollmentId, requestType?, title?, description?, reason?, priority?, dueDate? }
POST /api/v1/oms/requests/accommodation  { accommodationId, requestType?, ... }
POST /api/v1/oms/requests/assignment     { assignmentId, requestType?, ... }
```

All three return the ordinary generic Request DTO. The returned id then works
with every generic request endpoint.

## 5. Module adapter authorization

| Adapter | Requester may raise for |
| --- | --- |
| Enrollment | Their own enrollment (matched on `Student.UserId`, falling back to `Student.Email`), or any student if they hold a privileged queue role |
| Accommodation | Their own allocation; staff allocations require a privileged queue role |
| Assignment | Their own teaching material (`Lecturer.UserId` must match), or any assignment if privileged |

An unowned assignment (no lecturer) is privileged-only: otherwise anybody could
request changes to teaching material with no responsible lecturer.

Rejections happen **before** the Request Core is reached, so an unauthorized
caller cannot create anything at all.

## 6. Request types

Seeded by `20260925090000_SeedSmsRequestTypes`. The seed is:

- **tenant-safe** — one row per active tenant, never a single global row;
- **idempotent** — `ON CONFLICT ("tenant_id","Code") DO NOTHING` against the
  unique index created by `20260923080030_AddSmsRequests`, so re-running is a
  no-op and operator customisations are never overwritten by a deploy;
- **non-destructive on rollback** — `Down` is intentionally a no-op, so
  rolling back cannot delete request types operators have customised or orphan
  historical requests that reference the codes.

| Code | Display name | Requester roles |
| --- | --- | --- |
| `GENERAL_REQUEST` | General Request | all request-capable roles |
| `ENROLLMENT_EXCEPTION` | Enrollment Exception | all request-capable roles |
| `ENROLLMENT_COURSE_CHANGE` | Enrollment Course Change | all request-capable roles |
| `ENROLLMENT_UNIT_CORRECTION` | Enrollment Unit Correction | all request-capable roles |
| `ENROLLMENT_CANCELLATION` | Enrollment Cancellation | all request-capable roles |
| `ACCOMMODATION_TRANSFER` | Accommodation Transfer | Admin, Coordinator, Student |
| `ACCOMMODATION_ALLOCATION` | Accommodation Allocation | Admin, Coordinator, Student |
| `ACCOMMODATION_EXCEPTION` | Accommodation Exception | Admin, Coordinator |
| `ACCOMMODATION_CORRECTION` | Accommodation Correction | Admin, Coordinator, Student |
| `ASSIGNMENT_EXTENSION` | Assignment Extension | Admin, Coordinator, Lecturer, Student |
| `ASSIGNMENT_REOPEN` | Assignment Reopen | Admin, Coordinator, Lecturer |
| `ASSIGNMENT_CORRECTION` | Assignment Correction | Admin, Coordinator, Lecturer |
| `ASSIGNMENT_EXCEPTION` | Assignment Exception | Admin, Coordinator, Lecturer |

Each adapter exposes its own allow-list of codes, so a client cannot raise an
"enrollment" request that is actually typed as an unrelated workflow. The
generic create command independently verifies the type exists and is active.

> **Defect fixed in Phase 3:** `SeedSmsRequestTypes` was missing its
> `[DbContext]` / `[Migration]` attributes, so Entity Framework never
> discovered it and the seed had never run anywhere — including production.
> The attributes are now present and the seed carries the full code set.

## 7. Typed relationships

The adapter resolves module context and populates only what is genuinely
correct:

| Adapter | Populated |
| --- | --- |
| Enrollment | `EnrollmentId`, `StudentId`, `CourseId`, `UnitId`, `RelatedEntityType="Enrollment"` |
| Accommodation | `AccommodationId`, `StudentId`, `LecturerId`, `RelatedEntityType="Accommodation"` |
| Assignment | `AssignmentId`, `UnitId`, `LecturerId`, `RelatedEntityType="Assignment"` |

`Assignment.CourseOfferingId` is deliberately **not** mapped to `CourseId`: an
assignment belongs to a unit, and copying the offering id into the Course field
would misrepresent the academic context. This is covered by a test.

Every generated description states the domain-ownership boundary explicitly, so
an approver never mistakes an approved request for an applied business change.

## 8. Tests

`tests/SMS.UnitTests/OMS/ModuleAdapters/` — 23 tests across two files.

Covered:

- **Creation** — valid request, unknown/invalid referenced entity, unprivileged
  role, unauthenticated caller, missing required reference.
- **Authorization** — requester access, coordinator-on-behalf, cross-student
  rejection, cross-lecturer rejection, unowned-assignment rejection.
- **Relationships** — typed fields populated correctly; `CourseId` deliberately
  not populated for assignments.
- **Request type allow-list** — an enrollment request cannot be typed as a
  certificate correction, and vice versa.
- **Tenant isolation** — a filtered (cross-tenant) reference is reported as a
  field-level validation error and does not leak the entity's existence.
- **Domain separation** — the adapter delegates through the shared
  `IOmsRequestModuleAdapter` seam and never mutates the module entity.

## 9. Frontend

`frontend/sms-web/src/services/requests.service.ts` is a complete, typed API
client for the generic Request API and the three adapter endpoints. It mirrors
the backend DTOs and never invents a transition, computes a request number, or
filters a list client-side to compensate for access control. Attachment
downloads go through the authorized endpoint; raw storage paths are never known
to the browser.

> **Not yet built:** the Request Workspace UI (list, detail, comments,
> attachments, lifecycle action dialogs, module entry points) and its routes.
> The service client is the foundation those pages will consume. See
> "Remaining work" below.

## 10. Production reconciliation (read-only evidence)

Collected over SSH on `sms_admin@192.168.110.161`, `/opt/sms/app`. **No changes
were made to production.**

```text
Code parity:
  Production HEAD = 8f54cfe4dc4cdbc7c99e7ec82f952ac153745d92 (identical to baseline)
  Working tree clean; no local modifications
  -> production runs the same code as the Phase 1 baseline

Container parity:
  sms-api      Up 4 days (healthy)
  sms-web      Up 4 days (healthy)
  sms-postgres Up 11 days (healthy)
  -> all application containers healthy

Migration parity:
  Latest applied migration = 20260918045602_AddOms
  -> 20260923080030_AddSmsRequests        NOT applied
  -> 20260925090000_SeedSmsRequestTypes   NOT applied

Database schema:
  No table matching '%request%' exists in schema public
  COUNT sms_requests            -> table does not exist
  COUNT sms_request_types       -> table does not exist
  COUNT oms_request_attachments -> table does not exist

Request number sequence:
  Only oms_order_sequences exists; there is no Request sequence table

RequestTypes:
  None seeded
```

### Interpretation

Production is **exactly at the Phase 1 baseline** and has **no OMS Request
schema at all**. This is expected: the entire Request implementation was
uncommitted working-tree work, so it was never deployed.

This is a *clean forward deployment*, not a drift reconciliation. There is no
partial Request schema, no orphaned data, and nothing destructive to undo.

### Deployment plan (not executed)

1. Deploy the code containing the Request migrations.
2. Apply `20260923080030_AddSmsRequests` (creates tables + indexes).
3. Apply `20260925090000_SeedSmsRequestTypes` (tenant-safe, idempotent seed).
4. Smoke-test: login, create, submit, comment, attach, download, assign,
   approve, complete, verify notification and audit, then verify an
   unauthorized call and a cross-tenant call are both rejected.
5. Remove temporary test data and cookies.

### Safety notes

- No destructive migration was executed.
- The Request migrations are additive (new tables and indexes only) and do not
  touch any existing Order table.
- The Order OMS schema (`oms_order*`) is unaffected and its `oms_order_sequences`
  table remains the Order numbering mechanism.

## 11. Relationship to the existing Order OMS

The Order OMS is untouched by the Request work. `Order`, `OrderItem`,
`OrderStatusHistory`, `OrderManifest`, `OrderAttachment`, `OrderImport`,
`RoadAccount`, `RoadAccountingResult`, the Order API, the Order frontend and the
Order tests are all unmodified.

The Order domain is finance-shaped (road accounts, accounting results, currency
totals). The Request domain is a generic approval workflow. The two share the
`oms_` table prefix and the OMS controller folder, but they are separate models
with separate lifecycles, separate numbering, and separate tables. No finance
or procurement concept was introduced into the Request model, and no generic
Request concept was pushed into the Order model.

## 12. Known limitations and remaining work

- **Request Workspace UI is not built.** The typed service client exists and
  compiles (`tsc --noEmit` clean), but the list page, detail page, comment and
  attachment components, lifecycle action dialogs, module entry points and
  routes still have to be implemented, together with their frontend tests.
- **Assessment, Certificate and Timetable adapters are not implemented.** The
  audit in section 2 records why, and the request-type codes should be added
  only when the owning module defines a real exception path.
- **No production smoke test has been run**, because the Request feature is not
  deployed. Production is verified healthy and at the baseline, and nothing has
  been changed there.
- **5 pre-existing frontend test failures** exist on the baseline in
  `CourseOfferings`, `OmsOrderCreate`, `OmsOrders` and `Register`. They are
  unrelated to the Request work (no Request code is imported by those tests) and
  were present before Phase 3 changes. They should be triaged separately.
- `RelatedEntityId` / `RelatedEntityType` remain a loose association. Only the
  owning module interprets them; the Request Core does not resolve or act on
  them.
