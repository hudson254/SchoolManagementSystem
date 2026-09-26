# OMS Phase 2E — Requirements Audit and Gap Matrix

**Baseline commit:** `8f54cfe4dc4cdbc7c99e7ec82f952ac153745d92`
**Branch at audit time:** `coordinator-repair`
**Audit date:** 2026-09-21
**Scope:** OMS lifecycle completion (approve/reject/cancel), currency/financial totals, road accounting, manifest, attachments, import, notifications, audit/history, concurrency, API surface, frontend lifecycle controls, and tests.

---

## 1. Source documents examined

- `Documentation/OMS/OMS_ARCHITECTURE.md`
- `Documentation/OMS/OMS_OPEN_REQUIREMENTS.md`
- Domain: `Order`, `OrderStatus`, `OrderActionType`, `OrderLifecycle`, `OrderStatusHistory`, `OrderItem`, `OmsMoney`, `OmsOrderNumber`, `RoadAccount`, `RoadAccountType`, `RoadAccountingResult`, `OrderManifest`, `OrderAttachment`
- Application: OMS commands/queries, DTOs, `OmsPermissions`, handlers


### 2.3 Road Accounting (#18)

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| Road Accounting formulas / allocation / posting | OMS_OPEN_REQUIREMENTS.md #18 | Entities: RoadAccount, RoadAccountRepository, RoadAccountingResult, IRoadAccountingService stub exists but no formula implemented. | Business rules/formula not defined. Code explicitly returns NOT-CONFIGURED. | Yes — cannot implement real accounting without rules. | Do not implement speculative formulas. Document missing inputs: rate source, allocation key, posting accounts, approval triggers, currency conversion. Keep NOT-CONFIGURED guard and add tests confirming safe behavior. |

### 2.4 Manifest

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| Manifest generation and fields | OMS_OPEN_REQUIREMENTS.md (manifest); OMS_ARCHITECTURE.md | Entity OrderManifest exists with DB mapping; no generation workflow/endpoint. | Manifest number format, required fields, vehicle/transport info, signatures, output format, storage not fully specified in current docs. | Partial — schema exists but generation rules are open. | Document exact missing specification. Do not implement generation until fields/format are confirmed. |

### 2.5 Attachments and document storage

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| OMS attachments using existing FileStorageService | OMS_OPEN_REQUIREMENTS.md (attachments); OMS_ARCHITECTURE.md | OrderAttachment entity + mapping exists; references UploadFile (central storage). IFileStorageService exists. | No OMS attachment upload/download/listing endpoint; no authorization/tenant checks wired for attachment operations in OMS. | Partial — storage abstraction integrated at entity level; missing API/service behavior. | Implement OMS attachment endpoints only where authorization/tenant isolation can be guaranteed; reuse existing storage; add tests for tenant isolation and ownership. |

- Persistence: `ApplicationDbContext` OMS mappings, `OrderRepository`, `UnitOfWork`, `IOrderRepository`, `IRoadAccountingService`


### 2.6 Import

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| OMS CSV import with validation/staging/audit | OMS_OPEN_REQUIREMENTS.md (import); IOrderImportRepository; OrderImport/OrderImportRow entities | Import staging entities and repository exist. | CSV schema not defined in current docs; no import endpoint/validation rules. | Yes — cannot implement schema-specific validation without schema. | Document precise missing CSV schema requirements. Do not implement guessed parser. Provide gap report and interface readiness. |

### 2.7 Notifications

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| In-app notifications for lifecycle events | Not explicitly enumerated as required in current docs; SMS has INotificationService (SignalR in-app) | NotificationService implemented; no OMS lifecycle notifications wired. | No explicit requirement confirmed for approval requested/approved/rejected/cancelled notifications. | No — but if implemented, must use existing in-app notification service only. | If lifecycle notifications are desired, wire through INotificationService. Do not introduce external messaging. Document assumption if implemented. |



### 2.9 API surface

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| Approve and reject endpoints | OMS_ARCHITECTURE.md §6 workflow; OMS API conventions | No approve/reject controller actions; no policies for those actions. | Missing endpoints, policies, DTOs/response shapes. | Yes | Add POST /{id}/approve and POST /{id}/reject under OMS policy with thin controllers and MediatR handlers. |

### 2.10 Frontend

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| Lifecycle action controls in OMS UI | OMS frontend scope | OmsOrderDetail renders status and some actions; approve/reject UI not implemented. | No approve/reject buttons/guards; no API calls for them. | Yes — after backend endpoints ready. | Add approve/reject controls driven by server state and role; show validation/auth errors; do not rely on UI hiding as security. |

### 2.11 Testing

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| Comprehensive lifecycle tests | Phase 2E test requirements | Domain lifecycle/approval rules tests exist. | No application handler tests and no API tests for approve/reject; cancellation tests to be extended. | Yes | Add unit tests for handlers; add API tests for approve/reject/cancel extended scenarios; add concurrency and history regressions. |

---

## 3. Blocking summary

**Blocked this phase (must implement now):**
- Approve workflow: command, handler, policy action, controller endpoint, frontend control.
- Reject workflow: command, handler, policy action, controller endpoint, frontend control.
- Cancellation test coverage expansion around the already-implemented security rules.

**Blocked indefinitely / explicitly unresolved (do not guess):**
- Road Accounting formulas (#18).
- Manifest generation specification.
- CSV import schema.
- Any monetary rounding/currency policy not defined in docs.

**Not blocked, optionally implemented through existing infrastructure:**
- Lifecycle in-app notifications, only if desired; must use existing in-app notification service.

---

## 4. Assumptions

- The domain lifecycle constants and `OrderLifecycle` matrix are authoritative for allowed transitions.
- The domain rule requiring a rejection remark is treated as the source of truth for rejection reason requirement.
- Any currency/rounding behavior not documented in OMS requirements is left unspecified rather than invented.
- Existing SMS authorization/audit/notification/file-storage architectures are reused; no parallel systems introduced.

### 2.8 Audit / history

| Requirement | Architecture source | Current implementation | Gap | Blocking | Proposed implementation |
| ----------- | ------------------- | ---------------------- | --- | -------- | ----------------------- |
| Every transition records history (append-only) | OMS_ARCHITECTURE.md §6; OrderStatusHistory; history tests | Domain appends history on transitions; history fields are private-set; tests exist. | New lifecycle commands must also persist history in application transaction. | No — invariant exists; must be preserved by new handlers. | Handlers append history within same transaction as status change; add regression test ensuring history is inserted, not updated. |

- API: `OmsOrdersController`, `OmsDashboardController`, `OmsPolicy`, `Program.cs` OMS policies
- Notifications: `INotificationService`, `NotificationService`
- Frontend: `OmsOrders`, `OmsOrderDetail`, `oms.service.ts`, roles/utils
- Tests: OMS domain, persistence, API, frontend tests
