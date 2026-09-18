# OMS Architecture — Phase 1 Design Document

Status: **DRAFT — pending review.** Phase 1 deliverable. No OMS business code has
been written yet. Verified against branch `coordinator-repair`, commit `7e28489`.

> Correction note: `Documentation/ProductionReadiness/OMS_INTEGRATION.md` and
> `PRODUCTION_OMS_INTEGRATION_COMPLETE.md` describe OMS *infrastructure/configuration*
> (compose wiring, storage paths, env keys). They must not be read as evidence that an
> OMS *implementation* exists — `PRODUCTION_DEPLOYMENT_FINAL_REPORT.md` explicitly
> concludes it does not. This document is the baseline for building it.

---

## 1. Existing SMS architecture relevant to OMS (verified)

### 1.1 Solution layout (.NET 9, single backend)

| Project | Role relevant to OMS |
|---|---|
| `src/SMS.Domain` | Entities, enums, domain interfaces. All entities inherit `BaseEntity` (Guid `Id` = public API id, `TenantId`, audit fields, soft-delete fields, `RowVersion`) or `TenantAwareEntity`. Column names explicit snake_case via `[Column]`. |
| `src/SMS.Application` | CQRS via MediatR. Feature folders `Features/<Area>/Commands|Queries|DTOs`. FluentValidation validators co-located with commands. Pipeline behaviours: `ValidationBehavior`, `LoggingBehavior`. `PagedResult<T>` for pagination. |
| `src/SMS.Infrastructure` | Cross-cutting services: `AuditService`, `FileStorageService`, `UploadService`, `CurrentUserService`; options classes under `Options/`. |
| `src/SMS.Persistence` | `ApplicationDbContext` (IdentityDbContext), entity configuration inline in `OnModelCreating`, migrations in `Migrations/`, `TenantContextDbInterceptor` (sets PostgreSQL `app.tenant_id` session var for RLS). |
| `src/SMS.API` | Controllers under `Controllers/v1`, `BaseApiController` (`api/v{version:apiVersion}/...`), Asp.Versioning, policy-based authorization, middleware chain (CorrelationId → Logging → ExceptionHandling → SecurityHeaders → CSRF → TenantResolution → RateLimiting → Metrics), Serilog, config-gated Swagger. |
| `src/SMS.Multitenancy` | Tenant resolution (`ITenantStore`, `TenantContext`). |
| `src/SMS.Identity`, `SMS.Notifications`, `SMS.Reporting`, `SMS.Certificates`, `SMS.Shared` | Identity stores, SignalR hub, reporting/CSV/EPPlus, certificate module, shared DTOs. |

### 1.2 Conventions the OMS must follow (verified from code)

* **Entities**: extend `BaseEntity`/`TenantAwareEntity`; `Guid Id` generated in
  constructor; explicit `[Table("snake_case")]` / `[Column("snake_case")]`;
  virtual navigation properties; soft delete via `SoftDelete(deletedBy)` +
  `HasQueryFilter(e => !e.IsDeleted)`.
* **Persistence**: register `DbSet<>` in `ApplicationDbContext`, configure in
  `OnModelCreating` (required fields, max lengths, unique indexes, FK delete
  behaviour `Restrict` for ownership links). Migrations applied on startup.
* **CQRS**: `IRequest<T>` + `IRequestHandler<,>` + FluentValidation validator;
  repositories via domain interfaces; `IUnitOfWork` for `SaveChangesAsync` /
  transactions; audit via `IAuditService.LogActivityAsync(...)`.
* **API**: thin controllers (`[ApiVersion("1.0")]`, `[Authorize]`, policy per
  action) mapping to MediatR requests; DTO-only responses; `PagedResult<T>`;
  `ExceptionHandlingMiddleware` error mapping; CSV via `CsvHelper`; reports via
  EPPlus.
* **AuthN/AuthZ**: JWT bearer + cookie session, CSRF double-submit,
  `X-Tenant-Id` resolved by `TenantResolutionMiddleware`; roles
  `SystemAdministrator`, `Administrator`, `Coordinator`, `Lecturer`, `Student`,
  `Receptionist` (`RoleType` enum); role-group policies.
* **Multi-tenancy (defense in depth)**: `TenantId` on every tenant-owned entity +
  PostgreSQL Row-Level Security (`EnableRowLevelSecurity` migration pattern:
  `ENABLE/FORCE ROW LEVEL SECURITY` + sel/ins/upd/del policies on
  `tenant_id = app.current_tenant_id()`) + `TenantContextDbInterceptor` +
  application-level filtering. OMS tables MUST get the same treatment.
* **Files**: uploads via `FileStorageService` (`FileStorage:Path`, default
  `/app/uploads`, max 10 MB, extension allow-list) with path-traversal-safe
  `ResolveSafePath`; metadata in `UploadFile` entity (generated name, SHA-256,
  MIME fields; `OriginalFileName` never trusted). OMS attachments and manifests
  must reuse this model, not invent parallel storage.
* **Tests**: `tests/SMS.UnitTests` (xUnit + FluentAssertions + Moq, 408 tests),
  `tests/SMS.ApiTests` (WebApplicationFactory against REAL PostgreSQL via
  `docker/docker-compose.oms.yml` / `oms_test`, 111 tests), `SMS.IntegrationTests`,
  e2e, frontend vitest.
* **Frontend** (`frontend/sms-web`): React 19 + TypeScript + Vite, MUI v5
  (`theme.ts`), react-router v7 lazy pages, TanStack Query, axios client
  (`services/api.ts` — cookie auth + CSRF header + refresh single-flight),
  per-area services, contexts (`AuthContext`, `TenantContext`, `ThemeContext`
  dark mode), `Layout/Sidebar.tsx` role-gates nav with
  `user?.roles?.includes(role)`, notistack, zod + react-hook-form,

### 1.3 Existing OMS infrastructure discovered (configuration-only, no code)

| Key | Env var | Default | Read today by |
|---|---|---|---|
| `RoadsDb:File` | `ROADS_DB_FILE` | `/app/data/roads.db` | Nothing in `src/` (config only) |
| `WALRecovery:Target` | `WAL_RECOVERY_TARGET` | `latest` | Nothing |
| `WALRecovery:RecoveryPath` | `WAL_RECOVERY_PATH` | `/app/data/wal-recovery` | Nothing |
| `OrderManifestStorage:Enabled` | `ORDER_MANIFEST_STORAGE_ENABLED` | `true` | Nothing |
| `OrderManifestStorage:BasePath` | `ORDER_MANIFEST_STORAGE_PATH` | `/app/data/order-manifests` | Nothing |
| `OrderManifestStorage:FilePrefix` | `ORDER_MANIFEST_FILE_PREFIX` | `order-manifest-` | Nothing |
| `OrderManifestStorage:FileExtension` | — | `.json` | Nothing |
| `OrderManifestStorage:RetentionDays` | `ORDER_MANIFEST_RETENTION_DAYS` | `30` (prod) / `7` (oms test) | Nothing |

Wired in: `docker/docker-compose.prod.yml`, `docker/docker-compose.yml`,
`docker/docker-compose.oms.yml` (isolated `sms-oms-test` project, ports 5434/5080),
`.env.example`, `oms_env.sh`. Volumes: `api_data:/app/data`,
`api_uploads:/app/uploads`, `api_logs:/app/logs`,
`api_dataprotection:/app/dataprotection-keys`.
**Rule preserved: never mount a second volume over `/app/data`.**

`Program.cs` (~lines 780-830) provisions directories for these paths at startup.
Nothing else consumes the keys.

---

## 2. Proposed OMS architecture

OMS is implemented **inside** the existing solution as a new feature area — no
second architecture, no `sms-new`, no separate service:

```text
SMS.Domain
  Entities/        Order, OrderItem, OrderStatusHistory, OrderAttachment,
                   OrderManifest, OrderImport, OrderImportRow, RoadAccount
  Enums/           OrderStatus, OrderActionType, OrderImportStatus, RoadAccountType
  Interfaces/      IOrderRepository, IOrderManifestStorage, IRoadAccountingService,
                   IOrderNumberGenerator
SMS.Application
  Features/Orders/{Commands,Queries,DTOs}
  Features/OrderImports/{Commands,Queries}
  Features/RoadAccounting/{Commands,Queries}
  Features/OmsDashboard/Queries
SMS.Infrastructure
  Storage/OrderManifestStorage.cs     (implements IOrderManifestStorage, binds options)
  Services/RoadAccountingService.cs   (calculation isolated behind interface)
  Services/OrderNumberGenerator.cs
SMS.Persistence
  Data/ApplicationDbContext.cs        (+ DbSets, configs, RLS for OMS tables)
  Migrations/AddOms.cs                (additive only; enables RLS on OMS tables)
SMS.API
  Controllers/v1/OrdersController.cs, OrderManifestsController.cs,
  OrderImportsController.cs, RoadAccountingController.cs, OmsReportsController.cs
frontend/sms-web
  pages/oms/*  services/oms.service.ts  Sidebar section (role-gated)
tests/
  SMS.UnitTests/Orders/*              domain + application unit tests
  SMS.ApiTests/Controllers/*          OMS API / security tests
```

Reuse: `IUnitOfWork` transactions, `IAuditService`, `FileStorageService` patterns
(path-traversal guards), `PagedResult<T>`, MediatR pipeline, existing auth
policies/roles, existing `docker-compose.oms.yml` test environment unchanged.

### 2.1 Configuration binding (connects existing keys — nothing removed)

```csharp
// SMS.Infrastructure/Options/OrderManifestStorageOptions.cs
public class OrderManifestStorageOptions
{
    public bool Enabled { get; set; } = true;
    public string BasePath { get; set; } = "/app/data/order-manifests";
    public string FilePrefix { get; set; } = "order-manifest-";
    public string FileExtension { get; set; } = ".json";
    public int RetentionDays { get; set; } = 30;
    public long MaxFileSizeMB { get; set; } = 10;
}
// RoadsDb:File + WALRecovery:* bound to IOptions<RoadsDbOptions>/IOptions<WalRecoveryOptions>
// surfaced in a Phase 2 health check (dirs already provisioned in Program.cs).
```

---

## 3. Proposed domain model

All entities: `BaseEntity`-derived (Guid id, tenant, audit, soft delete), explicit
snake_case columns, RLS-protected.

```text
Order (table: oms_orders)
  OrderNumber        string  unique per tenant, generated (format = OPEN REQ)
  Title              string  required, 200
  Description        string? 2000
  Status             enum OrderStatus (indexed)
  RequestedByUserId  string (creator)
  ApprovedByUserId? / ApprovedAtUtc? / ApprovalRemarks?
  RejectedByUserId? / RejectedAtUtc? / RejectionRemarks?
  CancelledByUserId? / CancelledAtUtc? / CancellationReason?
  SubmittedAtUtc?  RequiredByDate?  RoadAccountId?
  TotalAmount decimal(18,2) (maintained from items; rounding rule = OPEN REQ)
  Currency string(3) default "KES" (OPEN REQ)
  RowVersion concurrency token

OrderItem (table: oms_order_items)
  OrderId FK → Order (Restrict), ItemCode?, Description required(500),
  Quantity int > 0, UnitPrice decimal(18,2) >= 0,
  LineTotal computed in domain (Quantity * UnitPrice), RowNumber (import trace)

OrderStatusHistory (table: oms_order_status_history) — append-only
  OrderId FK, FromStatus?, ToStatus, Action enum, PerformedByUserId,
  PerformedByUsername, PerformedAtUtc, Remarks?(1000)

OrderAttachment (table: oms_order_attachments)
  OrderId FK, UploadFileId FK → existing UploadFile (reuse upload pipeline),
  OriginalFileName, StoragePath, FileSizeBytes, Sha256Hash, ContentType,
  UploadedByUserId, UploadedAtUtc

OrderManifest (table: oms_order_manifests)
  OrderId FK, FileName (generated, collision-resistant:
    {FilePrefix}{tenantShort}-{orderId}-{yyyyMMddHHmmss}{FileExtension}),
  StoragePath (relative, tenant-segmented), FileSizeBytes, GeneratedByUserId,
  GeneratedAtUtc, DownloadedCount, LastDownloadedAtUtc?

OrderImport (table: oms_order_imports)
  FileName, Status enum (PendingValidation|Validated|Imported|Failed),
  TotalRows, ValidRows, InvalidRows, ImportedRows, SkippedRows,
  ErrorsJson, UploadedByUserId, ValidatedAtUtc?, ImportedAtUtc?

OrderImportRow (table: oms_order_import_rows)
  ImportId FK, RowNumber, RawJson, IsValid, ErrorMessage

RoadAccount (table: oms_road_accounts)
  Code string(50) unique per tenant, Name required(200), AccountType enum,
  Balance decimal(18,2) (rules = OPEN REQ), IsActive
  (order linkage via Order.RoadAccountId — OPEN REQ)
```

Indexes: `(TenantId, OrderNumber)` unique, `(TenantId, Status)`,
`(TenantId, CreatedAt)`, `(TenantId, RequestedByUserId)`, FK indexes,
`OrderImport (TenantId, Status)`.

---

## 4. Proposed order lifecycle (documented assumption, extensible)

```text
enum OrderStatus { Draft=1, Submitted=2, PendingApproval=3, Approved=4,
                   Rejected=5, Cancelled=6 }

Draft ──submit──▶ Submitted ──review──▶ PendingApproval ──approve──▶ Approved
 ▲                    │                        │
 └── edit (Draft only)│──reject───────────────┘──▶ Rejected
 any non-final ───────┴──cancel (rules = OPEN REQ)──────────▶ Cancelled
```

Enforced by a domain state machine (`OrderLifecycle.CanTransition(from,to)` +
`Order.ApplyTransition(...)`), never magic strings. Every transition appends
`OrderStatusHistory` (who/when/remarks). Valid transitions:

| From | Allowed |
|---|---|
| Draft | Submitted, Cancelled |
| Submitted | PendingApproval, Approved*, Rejected*, Cancelled |
| PendingApproval | Approved, Rejected, Cancelled |
| Approved | terminal (post-approval cancellation = OPEN REQ) |
| Rejected / Cancelled | terminal (reopen = OPEN REQ) |

\* direct Submitted→Approved/Rejected allowed so small tenants can skip review;
configurable flag. Creator must never approve own order (OPEN REQ to confirm).

---

## 5. Proposed roles / permissions

Map onto existing roles — no new identity system, no blanket grants:

| Permission (policy) | SysAdmin | Admin | Coordinator | Lecturer | Student | Receptionist |
|---|---|---|---|---|---|---|
| `OmsViewOrders` | ✓ | ✓ | ✓ | ✓ | – | – |
| `OmsCreateOrder` | ✓ | ✓ | ✓ | – | – | – |
| `OmsEditOrder` (own, Draft) | ✓ | ✓ | ✓ | – | – | – |
| `OmsSubmitOrder` | ✓ | ✓ | ✓ | – | – | – |
| `OmsApproveOrder` | ✓ | ✓ | – | – | – | – |
| `OmsRejectOrder` | ✓ | ✓ | – | – | – | – |
| `OmsCancelOrder` | ✓ | ✓ | ✓ (own) | – | – | – |
| `OmsUploadAttachment` | ✓ | ✓ | ✓ | – | – | – |
| `OmsDownloadAttachment` | ✓ | ✓ | ✓ | – | – | – |
| `OmsImportOrders` | ✓ | ✓ | – | – | – | – |
| `OmsManageManifests` | ✓ | ✓ | – | – | – | – |
| `OmsViewReports` | ✓ | ✓ | ✓ | – | – | – |
| `OmsManageRoadAccounting` | ✓ | ✓ | – | – | – | – |

---

## 6. Proposed API surface (existing convention `api/v{version}/...`)

```text
GET    /api/v1/oms-orders                 list (page,pageSize,search,status,from,to,sort)
POST   /api/v1/oms-orders                 create (Draft)
GET    /api/v1/oms-orders/{id}            detail
PUT    /api/v1/oms-orders/{id}            edit (Draft only)
POST   /api/v1/oms-orders/{id}/submit
POST   /api/v1/oms-orders/{id}/approve    { remarks? }
POST   /api/v1/oms-orders/{id}/reject     { remarks }
POST   /api/v1/oms-orders/{id}/cancel     { reason }
GET    /api/v1/oms-orders/{id}/history
GET    /api/v1/oms-orders/{id}/attachments
POST   /api/v1/oms-orders/{id}/attachments   (multipart, validated)
GET    /api/v1/oms-orders/{id}/attachments/{attachmentId}/download
GET    /api/v1/oms-orders/{id}/manifest            generate
GET    /api/v1/oms-orders/{id}/manifest/download   authorized download
GET    /api/v1/oms-imports                         import history
POST   /api/v1/oms-imports/validate                upload CSV → preview report
POST   /api/v1/oms-imports/{id}/confirm            transactional import
GET    /api/v1/oms-road-accounts                   list / CRUD (restricted)
GET    /api/v1/oms-reports/summary                 dashboard counts (tenant-aware)
```

Controller naming uses `oms-orders` etc. to avoid clashing with a possible future
generic `OrdersController`; route pattern otherwise identical to existing
controllers. Swagger annotated; DTO-only responses.

---

## 7. Proposed frontend structure

```text
src/services/oms.service.ts            axios wrapper (existing api client)
src/pages/oms/OmsDashboard.tsx         status counters + recent orders + imports
src/pages/oms/Orders.tsx               table (filters, pagination, actions by role)
src/pages/oms/OrderFormPage.tsx        create/edit + items editor
src/pages/oms/OrderDetailPage.tsx      lifecycle actions, history timeline,
                                       attachments, manifest download
src/pages/oms/ApprovalQueue.tsx        Submitted/PendingApproval filter
src/pages/oms/OrderImports.tsx         CSV upload → preview errors → confirm
src/pages/oms/RoadAccounting.tsx       accounts list + balances
src/types/oms.ts                       TS types
```

`App.tsx`: lazy routes `oms`, `oms/orders`, `oms/orders/new`, `oms/orders/:id`,
`oms/orders/:id/edit`, `oms/approvals`, `oms/imports`, `oms/road-accounting`.
Sidebar: new "Orders" section gated by role (Admin/Coordinator +
SystemAdministrator; approval queue admin-only). MUI theme, dark mode, notistack,
---

## 8. Proposed storage architecture

* **Attachments** flow through the existing `UploadService`/`FileStorageService`
  (`/app/uploads`, path-traversal-safe, SHA-256, MIME detection) with a new
  `UploadCategory.OmsDocument`; `OrderAttachment` links `UploadFile` → `Order`.
  Download only via authorized API endpoint (nginx never exposes `/app/data` or
  `/app/uploads` directly — `docker/nginx-frontend.conf` proxies, not serves).
* **Manifests** use a dedicated `OrderManifestStorage` bound to the existing
  config keys: files written under
  `{BasePath}/{tenantId}/{generated-name}{FileExtension}` — tenant-segmented,
  generated names only (no client paths), size limit, atomic write (temp file +
  move), metadata row in `oms_order_manifests`. Download validates tenant
  ownership then streams via API. Retention deletes files older than
  `RetentionDays` (run on manifest generation + startup background sweep).
* **RoadsDb** (`/app/data/roads.db`): path bound + health-checked; file format
  and consumer deferred (open requirement).

---

## 9. Proposed CSV workflow (two-stage)

```text
POST validate:  parse headers against documented schema, validate each row,
                persist OrderImport + OrderImportRow (status/errors), return
                preview { totalRows, validRows, invalidRows, errors[], rows }
POST confirm:   re-validate stored rows, BeginTransaction, insert Orders+Items
                (duplicate OrderNumber → skipped), commit, update counters,
                audit "OrderImported". Nothing partially imported.
```

Schema (proposed, documented assumption):
`OrderNumber*, Title*, ItemDescription*, Quantity*, UnitPrice, RequiredByDate, Notes`
(`*` required; header order-insensitive; UTF-8; first row = header).

---

## 10. Proposed Road Accounting architecture

Domain: `RoadAccount` + `IRoadAccountingService` (SMS.Infrastructure) exposing
`ComputeOrderAllocationAsync(order)` / `PostOrderToAccountAsync(...)`. **No
formula is implemented** until the rule is supplied (open requirement #18); the
service interface + persistence are built, the calculation returns a documented
"not configured" result until rules arrive. `RoadsDb:File` stays bound /
health-checked only.

---

## 11. Audit trail mapping (reuses IAuditService)

| Event | Action string | EntityName |
|---|---|---|
| created / edited / submitted / approved / rejected / cancelled | OrderCreated, OrderEdited, OrderSubmitted, OrderApproved, OrderRejected, OrderCancelled | Order |
| attachment up / download | OrderAttachmentUploaded / OrderAttachmentDownloaded | OrderAttachment |
| manifest generated / downloaded | OrderManifestGenerated / OrderManifestDownloaded | OrderManifest |
| CSV validate / confirm | OrderImportValidated / OrderImported | OrderImport |
| road accounting change | RoadAccountChanged | RoadAccount |

Plus append-only `OrderStatusHistory` for lifecycle detail.

---

## 12. Requirements still unknown

## 13. Files that will be created / modified (Phase 2+)

Created: entities/enums/interfaces listed in §2, `OrderManifestStorage.cs`,
`RoadAccountingService.cs`, `OrderNumberGenerator.cs`, OMS option classes,
`Features/Orders/**`, `Features/OrderImports/**`, `Features/RoadAccounting/**`,
`Features/OmsDashboard/**`, 5 controllers, migration `AddOms`, frontend pages +
service + types, tests (`OrderCreationTests`, `OrderLifecycleTests`,
`OrderApprovalTests`, `OrderValidationTests`, `OrderTenantIsolationTests`,
`RoadAccountingTests`, `OrderManifestStorageTests`, `ProductionStorageLayoutTests`,
API tests, CSV tests), `Documentation/OMS/*`.
Modified (additive only): `ApplicationDbContext.cs`, `Program.cs` (DI + policies),
`App.tsx` / `Sidebar.tsx`, `.env.example` (docs), `OMS_INTEGRATION.md`
(status correction).

## 14. Risks

1. **Business rules undefined** (numbering, statuses, approval ownership, cancel
   semantics, road-accounting math, currency) — mitigated by documented defaults +
   `OMS_OPEN_REQUIREMENTS.md`; everything configurable / isolated behind interfaces.
2. **RLS + migrations on a shared production DB** — OMS migration is additive
   (CREATE TABLE + RLS enable); SQL reviewed before applying; no DROP anywhere.
3. **`PendingModelChangesWarning` ignored in Program.cs** — OMS model changes must
   ship with a real migration, never rely on EnsureCreated drift.
4. **Concurrency** — `RowVersion` on Order prevents lost updates on transitions;
   transitions re-validated in the handler under a transaction.
5. **Test-env coupling** — OMS tests must not weaken the existing 408-unit /
   111-API suites; shared PostgreSQL requires unique data per test (existing
   `ApiTestFixture` conventions).
6. **File-system safety** — all writes use generated names within configured
   roots; cross-tenant file access tested explicitly.
7. **Scope creep** — phase gating; production deployment (Phase 9) is out of scope
   until separately authorized.

## 15. Phase 2 implementation plan

1. Domain: enums (`OrderStatus`, `OrderActionType`, `OrderImportStatus`,
   `RoadAccountType`), 8 entities, state machine, unit tests (OrderLifecycle,
   OrderValidation).
2. Persistence: `ApplicationDbContext` additions (DbSets, configs, query filters,
   unique indexes), migration `AddOms` (tables + indexes + RLS enable), review
   generated SQL; persistence-level tenant-isolation test.
3. Infrastructure: `OrderManifestStorageOptions` / `RoadsDbOptions` /
   `WalRecoveryOptions` binding, `OrderManifestStorage`, `OrderNumberGenerator`
   (default `ORD-<yyyy>-<6-seq-per-tenant>`, configurable), stub
   `RoadAccountingService`.
4. Application: commands / queries / validators / DTOs (create, edit, list with
   filters, detail, submit, approve, reject, cancel, history, attachments
   up/download, manifest generate/download, CSV validate/confirm, road accounts
   CRUD, dashboard summary) with `IUnitOfWork` transactions + audit calls.
5. Run: `dotnet build` + unit tests; regression suites must stay green.



