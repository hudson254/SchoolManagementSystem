# OMS Phase 2 — Request Core Stabilization

## 1. Scope and outcome

Phase 2 stabilized the **generic OMS Request Core** so it could serve as the
single authoritative workflow engine for the whole School Management System.

The Request Core owns the workflow. It does **not** own any SMS business
entity. Student, Lecturer, Course, Unit, Enrollment, Accommodation, Assignment,
Assessment, Certificate, Document, Timetable and Calendar Event all remain owned
by their existing modules.

## 2. What was stabilized

| Area | Outcome |
| --- | --- |
| Lifecycle | Centralised in `SMS.Domain/Common/RequestLifecycle.cs` + `Request` aggregate. No controller or module helper may set a status directly. |
| Status history | Append-only. Every lifecycle action writes a new `RequestStatusHistory` row; no record is ever updated or deleted. |
| Comments | Centralised. Authenticated author, tenant isolation, object-level authorization, chronological retrieval. |
| Attachments | Wired to the existing `IFileStorageService`. No second storage system. |
| Notifications | Wired to the existing in-app pipeline via `IOmsRequestNotifier`. No SMTP/Twilio/external gateway. |
| Audit | Uses the existing `IAuditService`. No parallel Request audit table. |
| Request types | Configuration is authoritative, not advisory: an unknown or deactivated type cannot open a request. |
| Object-level authorization | Centralised in `SMS.Application/Common/OmsRequestAccess.cs`. |
| Numbering | `REQ-yyyy-nnnnnn`, tenant-scoped, concurrency-safe via PostgreSQL upsert. |

## 3. Architecture

```text
SMS Business Modules
        |
        v
OMS Request Core            <-- the ONLY workflow engine
        |
        +-- lifecycle        (RequestLifecycle + Request aggregate)
        +-- authorization    (OmsRequestAccess + OmsAuthorization)
        +-- comments         (RequestComment)
        +-- status history   (RequestStatusHistory, append-only)
        +-- attachments      (RequestAttachment + FileStorageService)
        +-- notifications    (IOmsRequestNotifier -> NotificationService/Hub)
        +-- audit            (IAuditService)
        +-- request types    (RequestType)
        +-- assignment       (assign / reassign)
        +-- escalation       (escalate)
        |
        v
Existing SMS infrastructure (PostgreSQL, tenant/RLS, file storage, notifications, audit)
```

## 4. Request aggregate

`SMS.Domain/Entities/Request.cs` owns:

- request number, request type, title, description
- requester, assignee
- priority, status, lifecycle
- due date
- status history (append-only collection)
- comments
- related entity reference (`RelatedEntityType` / `RelatedEntityId`)
- typed SMS references where justified (`StudentId`, `LecturerId`, `CourseId`,
  `UnitId`, `EnrollmentId`, `AccommodationId`, `AssignmentId`, `CertificateId`)

The aggregate exposes behaviour methods (`Submit`, `StartReview`, `Assign`,
`Approve`, `Reject`, `ReturnForCorrection`, `Cancel`, `Complete`, `Escalate`).
Status is never assigned from the outside.

## 5. Lifecycle

Statuses: `Draft`, `Submitted`, `PendingReview` (under review), `Assigned`,
`Returned`, `Approved`, `Rejected`, `Cancelled`, `Completed`, `Escalated`.

Terminal states — `Approved`, `Rejected`, `Cancelled`, `Completed` — cannot be
modified. A terminal request is frozen, including its attachment set, so the
evidence recorded against it cannot change after the fact.

## 6. API

Base route: `/api/v1/oms/requests`

```text
GET    /api/v1/oms/requests                      list (scope-filtered)
POST   /api/v1/oms/requests                      create
GET    /api/v1/oms/requests/{id}                 detail
PUT    /api/v1/oms/requests/{id}                 update (Draft/Returned only)
POST   /api/v1/oms/requests/{id}/submit          submit
POST   /api/v1/oms/requests/{id}/review          start review
POST   /api/v1/oms/requests/{id}/assign          assign
POST   /api/v1/oms/requests/{id}/reassign        reassign
POST   /api/v1/oms/requests/{id}/approve         approve
POST   /api/v1/oms/requests/{id}/reject          reject
POST   /api/v1/oms/requests/{id}/return          return for correction
POST   /api/v1/oms/requests/{id}/cancel          cancel
POST   /api/v1/oms/requests/{id}/complete        complete
POST   /api/v1/oms/requests/{id}/escalate        escalate
GET    /api/v1/oms/requests/{id}/history         append-only status history
GET    /api/v1/oms/requests/{id}/comments        comments
POST   /api/v1/oms/requests/{id}/comments        add comment
GET    /api/v1/oms/requests/{id}/attachments     attachment metadata
POST   /api/v1/oms/requests/{id}/attachments     upload attachment
GET    /api/v1/oms/requests/{id}/attachments/{aid}/download
DELETE /api/v1/oms/requests/{id}/attachments/{aid}
GET    /api/v1/oms/requests/dashboard            queue summary
GET    /api/v1/oms/requests/types                list request types
POST   /api/v1/oms/requests/types                create request type (admin)
PUT    /api/v1/oms/requests/types/{typeId}       update request type (admin)
GET    /api/v1/oms/requests/types/{typeId}       request type detail
```

### List scope is server-authoritative

`GET /api/v1/oms/requests?scope=mine|assigned|all`

| Scope | Server behaviour |
| --- | --- |
| `mine` | Force-restricted to the caller's own requests. Client filters are ignored. |
| `assigned` | Force-restricted to requests assigned to the caller. |
| `all` | Full tenant queue. **Rejected with 403** for non-privileged roles. |

A non-privileged caller cannot widen their own access by asking for `all`, and
cannot forge `mine`/`assigned` to read somebody else's queue.

## 7. Authorization

Role membership alone is never sufficient. `OmsRequestAccess` evaluates the
requester/assignee relationship on the request itself:

| Actor | May |
| --- | --- |
| Requester | view own, edit own (Draft/Returned), submit own, respond when returned, cancel own, comment, access own attachments |
| Assignee | view assigned, review, comment, perform permitted workflow actions |
| Approver | view requests requiring their authority, approve, reject, return |
| Administrator | assign/reassign, manage request types, administrative cancellation |
| System Administrator | elevated capabilities |

Endpoint policy (`OmsPolicy`) is the first gate; object-level authorization is
the second and is never skipped.

## 8. Tenant isolation

- Every Request, RequestType, RequestComment, RequestStatusHistory and
  RequestAttachment carries `tenant_id`.
- The `ApplicationDbContext` applies a global query filter on tenant and
  soft-delete for each of them.
- Module adapters read module data through the same filtered repositories, so a
  cross-tenant reference resolves to `null` and is reported as a field-level
  validation error. It never returns a 404, which would confirm that the id
  exists in another tenant.

## 9. Notifications

All notifications use the existing in-app pipeline. No SMTP, Twilio, WhatsApp or
external notification platform was introduced.

| Trigger | Recipient |
| --- | --- |
| Submission | Approval queue roles |
| Assignment | New assignee |
| Reassignment | New assignee |
| Return for correction | Requester |
| Approval | Requester and relevant participants |
| Rejection | Requester |
| Completion | Requester |
| Review started | Requester |
| Comment | Requester, when the author requests notification |

Notification failures are logged and swallowed so a notification problem can
never roll back a workflow transition.

## 10. Attachments

Stored through the existing `IFileStorageService`.

Controls enforced server-side:

- tenant isolation
- object-level authorization (a bare attachment id is never sufficient)
- path traversal protection (`Path.GetFileName` strips any directory component;
  `FileStorageService.ResolveSafePath` re-validates containment)
- allowed-extension validation
- file-size limit
- audit of upload and delete
- terminal-request freeze (no uploads once terminal)

Raw filesystem paths are never returned to a client.

## 11. Request numbering

Format `REQ-yyyy-nnnnnn`, e.g. `REQ-2026-000001`.

- Per-tenant sequence.
- Concurrency-safe via a PostgreSQL upsert on the tenant+year key.
- Uniqueness is enforced by a unique index.
- A failed create rolls back with the sequence increment (same transaction).

## 12. Tests

`tests/SMS.UnitTests/OMS/Domain/RequestAggregateTests.cs` covers numbering
format and validation, the full transition matrix, terminal immutability,
invalid-transition rejection, edit rules, append-only history, and comment
validation.

## 13. Known limitations

- The Request Frontend Workspace did not exist at the end of Phase 2; it is
  started in Phase 3.
- The `Request` aggregate does not own or mutate any SMS business entity, by
  design.
