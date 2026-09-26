# OMS Full Integration — Phase 1 Discovery and Reconciliation Report

**Repository:** `hudson254/SchoolManagementSystem`
**Branch:** `coordinator-repair`
**Repository commit:** `8f54cfe4dc4cdbc7c99e7ec82f952ac153745d92`
**Discovery date:** 2026-09-23
**Scope:** full OMS discovery and reconciliation across repo, persistence, API, frontend, infra, tests, production host posture, and existing SMS modules
**Objective anchor:** make OMS a reusable institutional request / approval / workflow / document / assignment platform for SMS — **not a finance system**

---

## 1. Source control state (verified)

- **Current branch:** `coordinator-repair`
- **HEAD commit:** `8f54cfe chore(repo): drop generated git-bundle artifact; document source reconciliation`
- **Upstream:** `origin/main` (same commit as HEAD on this branch)
- **Production remote:** `prod` → `ssh://sms_admin@192.168.110.161/opt/sms/app`
- **Working tree:** dirty; OMS-related work is the important part
  - Modified (existing OMS/OMS-request wiring touched in this branch):
    - `src/SMS.API/Controllers/v1/Oms/OmsOrdersController.cs`
    - `src/SMS.API/Controllers/v1/Oms/OmsPolicy.cs`
    - `src/SMS.API/Program.cs`
    - `src/SMS.Application/Common/OmsPermissions.cs`
    - `src/SMS.Application/Features/OMS/Dtos/OmsDtos.cs`
    - `src/SMS.Application/Features/OMS/Queries/OmsDashboardSummaryQuery.cs`
    - `src/SMS.Domain/Interfaces/IUnitOfWork.cs`
    - `src/SMS.Persistence/Data/ApplicationDbContext.cs`
    - `src/SMS.Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`
    - `src/SMS.Persistence/Repositories/UnitOfWork.cs`
  - Untracked (new SMS Request implementation present on disk but **not yet committed**):
    - Domain: `Request`, `RequestComment`, `RequestStatusHistory`, `RequestType`, `RequestNumberSequence`, `RequestStatus`, `RequestPriority`, `RequestActionType`, `RequestLifecycle`, `SmsRequestNumber`
    - Interfaces: `IRequestRepository`, `IRequestTypeRepository`, `IRequestCommentRepository`, `IRequestNumberGenerator`
    - Infrastructure: `RequestNumberGenerator`
    - Persistence: `RequestRepository`, `RequestTypeRepository`, `RequestCommentRepository`, migration `20260923080030_AddSmsRequests`
    - Application: full request command/query/DTO set under `Features/OMS/`
    - API: `OmsRequestsController`
    - Docs: `Documentation/OMS/OMS_PHASE_2E_AUDIT.md`

**Interpretation:** the repo on this branch already contains a substantial **uncommitted SMS Request implementation** on top of the existing OMS baseline. This task is therefore **not starting from zero**.

---

## 2. Production reconciliation posture (verified)

- **Production host:** `192.168.110.161`
- **Edge:** nginx terminates TLS; external-facing entry points are HTTPS.
- **Reachability from this machine:**
  - ICMP ping to `192.168.110.161` succeeds (LAN reachable).
  - Plain HTTP probes to `http://192.168.110.161/health` and `/api/v1/health` are **not** the production surface reachable from this machine because the external path is HTTPS-terminated at nginx.
  - No usable PostgreSQL port exposure to this machine from the LAN.
- **Production deploy model (from repo artifacts):**
  - `docker/docker-compose.prod.yml` is the production compose file.
  - Production API: `sms-api`, HTTP-only behind nginx, env-driven.
  - Production DB: PostgreSQL 16, internal-only, RLS enabled via `init-db-rls.sql`.
  - Production storage: `/app/uploads`, `/app/data`, `/app/logs`, `/app/dataprotection-keys`.
- **Production vs repo cannot be assumed identical.**

**Key reconciliation conclusion:**
- What is **certain from repo artifacts**: production is provisioned to run the existing SMS + OMS storage contract (compose/env/dockerfile).
- What is **not yet proven from this machine**: exact production image commit, applied migration set, OMS tables currently present, OMS data, and whether the uncommitted SMS Request implementation has ever run in production.

Therefore Phase 1 cannot certify production OMS behavior yet. It can only certify the **intended production shape** and the **actual repository implementation**, and identify the verification gap that Phase 2 must close before production deployment.

---

## 3. Existing OMS baseline (Order Management System)

The repository already contains a real OMS implementation. It is **not** only config:

### Domain
- `Order` aggregate with items, status history, manifest link, attachment link
- `OrderStatus`, `OrderActionType`, `OrderLifecycle` state machine
- `OmsMoney`, `OmsOrderNumber`, `RoadAccount`, `RoadAccountType`, `RoadAccountingResult`
- `OrderManifest`, `OrderAttachment`, `OrderImport` / `OrderImportRow`, `OrderImportStatus`

### Persistence
- `OrderRepository`, `RoadAccountRepository`, `OrderImportRepository`
- `UnitOfWork` already exposes OMS repositories
- Migration and model snapshot include OMS tables and RLS-compatible tenant filtering
- `ApplicationDbContext` includes OMS DbSets/config and tenant interception setup

### Application
- CQRS commands/queries for orders, items, submit, approve, reject, cancel, history, dashboard, imports, road accounts, manifests
- DTOs (`OrderDto`, `OrderItemDto`, `OrderStatusHistoryDto`, `OmsDashboardSummaryDto`, etc.)

### API
- `OmsOrdersController`, `OmsDashboardController`
- `OmsPolicy` with order lifecycle policies
- `Program.cs` wires OMS services and OMS policies

### Frontend
- `sms-web/src/services/oms.service.ts` (order client)
- `oms.service.test.ts`
- `OmsAccess.test.tsx`
- `roles.ts` OMS role helpers
- Sidebar/ProtectedRoute OMS navigation and guards

### Tests
- OMS domain/persistence unit tests
- OMS API tests (`OmsOrdersApiTests`, `OmsOrderCancellationApiTests`)
- Integration tests touching tenant isolation

### Documentation
- `Documentation/OMS/OMS_ARCHITECTURE.md`
- `Documentation/OMS/OMS_OPEN_REQUIREMENTS.md`
- `Documentation/OMS/OMS_PHASE_2E_AUDIT.md` (previous phase audit/gap matrix)

**Important:** the existing OMS is explicitly an **order/purchasing-oriented** system with currency, totals, road accounts, manifests, CSV import, etc. The brief says SMS is **not a finance system**, so this existing OMS must be treated carefully: some of it is reusable infrastructure, some is finance-shaped and must **not** become the SMS-facing request model.

---

## 4. Newly added SMS Request layer (current working tree)

The uncommitted work already creates a second OMS shape inside the same repo:

- **Generic request aggregate** (`Request`) with:
  - Request number `REQ-yyyy-nnnnnn`
  - Request type code + display name
  - Title, description, priority, status
  - Requester, assignee, due date, completion info
  - Related-entity pointers: `RelatedEntityId`, `RelatedEntityType`
  - Optional typed links: `StudentId`, `LecturerId`, `CourseId`, `UnitId`, `EnrollmentId`, `AccommodationId`, `AssignmentId`, `CertificateId`
- **Lifecycle**: `RequestLifecycle` with explicit allowed transitions and terminal-state checks
- **Status history**: append-only `RequestStatusHistory`
- **Comments**: `RequestComment`
- **Request types**: `RequestType` (configurable code/display/active/requester roles/approver roles/workflow steps/attachment/comment defaults)
- **Number generation**: `RequestNumberGenerator` (PostgreSQL `ON CONFLICT DO UPDATE RETURNING`)
- **Repository**: `RequestRepository` with paged/filtered lookup, status counts, requester/assignee queries, history, comments, request types
- **CQRS**: create, update, submit, assign, reassign, approve, reject, return-for-correction, cancel, complete, escalate, comment, request-type commands; list/detail/dashboard/types/history/comments queries
- **Policies/permissions**: `OmsPermissions` request constants + `OmsAuthorization` request role sets; `OmsPolicy` request policies; `Program.cs` wires them
- **Migration**: `20260923080030_AddSmsRequests` creates request tables, sequences, types, comments, history, indexes, unique constraints

This is exactly the kind of generic request platform the task wants OMS to become. The current implementation is therefore **well-aligned directionally**.

---

## 5. SMS modules, auth, RBAC, audit, notifications, file storage, tenant isolation (existing)

### SMS modules present (API controllers)
- Dashboard, Students, Lecturers, Courses, Units, Course Offerings (+ units/students/lecturers), Classes, Timetable, Calendar Events, Assignments (+ documents), Assessments, Grades, Accommodation (lanes/houses/rooms/assignments/reports), Enrollments, Notifications, Reports, Report Admin/Verification, Auth, Users, Admin, Password Reset, Health

### Auth
- JWT bearer + cookie session, CSRF double-submit, correlation id, tenant resolution middleware, rate limiting, security headers, exception handling
- `User` extends `IdentityUser` + `ITenantAwareEntity`
- Roles: `SystemAdministrator`, `Administrator`, `Coordinator`, `Lecturer`, `Student`, `Receptionist`
- `JwtService`, `UserManagerService`

### Authorization
- Policy-based: `SystemAdministratorAccess`, `AdministratorAccess`, `ModeratorAccess`, `LecturerAccess`, `StudentAccess`, `ReceptionistAccess`, plus many feature policies
- OMS already extends this with its own `Oms.Can*` policies

### Notifications
- SignalR `NotificationHub` + `NotificationService` (in-app)
- In-app-only password reset/verification notifications already exist

### Audit
- `AuditService` / `IAuditService` with structured audit logs, persistence + logging

### File storage
- `FileStorageService` with path traversal protection, containers, size/extension constraints
- Upload-related API tests exist (`DocumentUploadApiTests`)

### Tenant isolation
- `ITenantContext`, tenant resolution middleware
- PostgreSQL RLS via `app.current_tenant_id()` + `app.enable_tenant_rls()`
- `ApplicationDbContext` sets `app.tenant_id` and applies `HasQueryFilter` on tenant-aware entities
- Tenant isolation tests exist in integration and OMS persistence suites

**Conclusion:** the shared infrastructure this task says must be reused is present and already used by SMS.

---

## 6. What is implemented and working (in repo)

- Existing OMS Order infrastructure and workflow (draft/submit/approve/reject/cancel/history/items/manifest/import/road-account persistence)
---

## 8. Backend without frontend integration (found)

- SMS Request backend is essentially complete in the working tree, but the **frontend currently has no SMS Request UI**.
- The only frontend OMS client today is for the **Order** module (`oms.service.ts`), not for SMS Requests.
- No frontend request list/detail/form/workflow UI exists yet for `Request`.

---

## 9. Frontend without backend support (found)

- None significant: the existing frontend OMS pages talk to the existing Order API. The new SMS Request backend has no frontend yet, which is the main integration gap.

---

## 10. Production functionality missing from Git

Cannot be confirmed from this machine. To be closed in Phase 2 before deployment:
- exact production image/tag
- applied migrations on production DB
- whether OMS tables (including the new SMS request tables) exist in production
- whether any SMS Request data exists in production
- whether production is running the behaviors in the uncommitted working tree

---

## 11. Git functionality missing from production

Cannot be confirmed without production access beyond what this machine can reach. Likely candidates based on the working tree:
- the migration `20260923080030_AddSmsRequests` likely not yet applied to production
- the SMS Request API endpoints likely not in the running production API unless it was redeployed from this branch

---

## 12. Incomplete workflows (identified)

- SMS Requests: lifecycle backend appears complete, but **notifications** for request events appear to be **not wired** yet (handlers have empty notification stubs in several places).
- SMS Requests: **attachments** for requests are not yet implemented in the new request layer (the old OMS already has `OrderAttachment` + `FileStorageService`, but requests do not yet expose request attachment upload/download/authorize flows).
- SMS Request types: present and manageable, but the domain `RequestType` carries richer configuration (requester roles, approver roles, workflow steps, notify flags) while the commands currently only manage a subset; UI/backend parity should be aligned.
- SMS-specific workflow integrations (course/enrollment, accommodation, assignment/assessment, certificate/document, timetable/event) are **not yet integrated** through OMS; they remain separate SMS modules today.

---

## 13. Missing authorization (identified)

- The new SMS Request layer adds its own OMS request policies and role checks, which is correct.
- However, object-level authorization in requests currently relies heavily on status/ownership/role checks inside handlers; this is good, but must be verified end-to-end for:
  - view own vs view assigned vs view all
  - cancel own vs cancel any
  - assignment/reassignment authorization
  - comment authorization
  - request-type management authorization

---

## 14. Missing tests (identified)

- SMS Request **backend** handlers/commands are implemented but I did not find committed SMS Request unit/API/integration tests matching the new request lifecycle.
- Existing OMS Order tests remain and must stay green.
- Frontend OMS tests exist for orders only, not for SMS Requests.

---

## 15. Missing documentation (identified)

- SMS Request lifecycle, permission matrix, request type configuration semantics, integration points to SMS modules, and “How SMS modules use OMS” guide are not yet documented.
- Production reconciliation evidence (exact production commit/image/migrations) is not yet documented.

---

## 16. Discovery conclusion

The repository already contains:

1. A real **Order-oriented OMS** with workflow, documents, imports, road accounting, manifests.
2. A newly built **SMS Request layer** intended to become the reusable institutional workflow platform.

What remains before this task is complete:

- **Reconcile and commit** the SMS Request implementation properly, without letting finance-shaped OMS concepts leak into the SMS request model.
- **Complete missing wiring**: request notifications, request attachments (reusing existing file storage), request-type configuration parity, and request dashboard/queue integration.
- **Add/extend tests** for the new request lifecycle and security, and protect existing OMS regression tests.
- **Integrate** OMS requests with the relevant SMS modules where approval/exception/admin workflow is appropriate, without replacing normal CRUD.
- **Validate production**: confirm production image/migrations/data state and run smoke tests against production using test accounts.
---

## 17. Phase 2 plan (directional, will be refined after commit)

### 17.1 Stabilize and commit the existing SMS Request implementation
- Review and align naming/behavior with existing SMS conventions
- Ensure `Request` lifecycle and history invariants are protected
- Ensure the migration and snapshot are consistent and committed

### 17.2 No finance leakage
- Keep currency/totals/road accounting/manifest/import concepts out of the SMS Request-facing workflow
- Preserve working Order infrastructure where it is internal OMS infrastructure, but do not expose it as SMS workflow

### 17.3 Complete request infrastructure gaps
- Wire request notifications through existing in-app notification service
- Add request attachment support via existing `FileStorageService` with authorization/tenant checks
- Ensure request-type management supports the configuration stored on `RequestType`

### 17.4 API and authorization hardening
- Verify every request action has server-side authorization
- Add missing object-level checks where required
- Keep policies aligned with `OmsPermissions` / `OmsAuthorization`

### 17.5 Frontend integration
- Add SMS Request workspace/pages in `sms-web`:
  - My Requests
  - Pending Approval / Assigned to Me / All Requests (role-scoped)
  - Request detail with actions, comments, status history
  - Request create/edit forms
- Update navigation and dashboard widgets where appropriate

### 17.6 SMS module integration points
- Identify appropriate workflows for:
  - student administration requests
  - accommodation-related requests
  - assignment/assessment-related requests
  - certificate/document-related requests
  - timetable/event-related requests
- Implement only where an approval/exception/admin process is actually required

### 17.7 Tests
- Request lifecycle tests
- Authorization/tenant isolation tests for requests
- API tests for new request endpoints
- Regression protection for existing OMS Order tests

### 17.8 Documentation
- Update `Documentation/OMS/` and add “How SMS Modules Use OMS”

---

*End of Phase 1 report.*

- **Document** architecture, permissions, integration, API, frontend, deployment, testing.


- SMS request aggregate, lifecycle, number generator, persistence, CQRS, controller, permissions/policies
- SMS request dashboard summary query
- SMS request type management commands
- SMS request comment command
- Frontend OMS service for orders (existing) and OMS access/roles/Sidebar integration for the existing order module
- Audit, notifications (in-app), file storage, tenant/RLS infrastructure

---

## 7. What is implemented but not committed

- Entire SMS Request implementation listed in Section 4 is untracked/unstaged on `coordinator-repair`
- This includes migration `20260923080030_AddSmsRequests`

**Implication:** the repo's committed baseline at `8f54cfe` does **not** yet contain the SMS Request model, even though the files exist on disk.
