# OMS Open Requirements

Status: Phase 1 deliverable. Every item below is a business rule that is **not
specified** anywhere in the repository (verified by search across docs,
configuration, code and git history). OMS implementation uses the **proposed
default** so work is not blocked, but each default is an assumption that must be
confirmed by the product owner. Any item marked **BLOCKING** changes data or
API shape if answered differently, so it should be resolved before Phase 9
(production readiness) at the latest.

| # | Requirement | Proposed default (assumption) | Impact if changed |
|---|---|---|---|
| 1 | **BLOCKING** — Exact order number format | `ORD-<yyyy>-<6-digit per-tenant sequence>`, generated server-side, unique per tenant, immutable | Numbering logic only (isolated in `IOrderNumberGenerator`) |
| 2 | **BLOCKING** — Exact order statuses | `Draft, Submitted, PendingApproval, Approved, Rejected, Cancelled` (see OMS_ARCHITECTURE §4) | Enum stored as int; changing requires mapping + migration |
| 3 | Which role can create orders | SystemAdministrator, Administrator, Coordinator | Permission matrix change only |
| 4 | Which role can approve orders | SystemAdministrator, Administrator | Permission matrix change only |
| 5 | Can a user approve their own order? | No — rejected with a validation error | Handler check only |
| 6 | Is the Submitted → PendingApproval review step mandatory? | No — approvers can act directly on Submitted (review step optional) | Minor workflow change |
| 7 | Are separate Creator / Reviewer / Approver roles required beyond existing SMS roles? | Not required initially; existing roles only | New role definitions + seeding |
| 8 | **BLOCKING** — Required order fields (header) | `Title` required; order number and items ≥ 1 required at submit time | Validator changes |
| 9 | **BLOCKING** — Required order item fields | `Description`, `Quantity` (> 0) required; `UnitPrice` optional (default 0); `ItemCode` optional | Validator changes |
| 10 | Required fields at which stage — can a Draft be saved with zero items? | Yes; items validated at submit, not at create | Validator design |
| 11 | May orders be edited after approval? | No — Approved/Rejected/Cancelled are terminal and read-only | Handler state checks |
| 12 | May approved orders be cancelled? | No; cancel allowed from Draft/Submitted/PendingApproval only | State machine change |
| 13 | May rejected/cancelled orders be reopened? | No (reopen not in initial lifecycle) | State machine change |
| 14 | Who may cancel an order? | Creator (own drafts/submitted) + Admin/SystemAdmin (any non-final) | Policy change |
| 15 | Is a reason required for reject / cancel? | Required for reject; required for cancel | Validator changes |
| 16 | **BLOCKING** — Currency | `KES` default, per-order override, ISO-4217 3-letter | Migration if multi-currency |
| 17 | **BLOCKING** — Total amount rounding rules | Decimal(18,2); line total = Quantity × UnitPrice exactly; order total = sum of line totals, rounded half-even | Calculation change |
| 18 | **BLOCKING** — RoadAccounting rules: what is a "road", what is allocated, what is the posting formula, how does `Balance` change? | **Unknown — no calculation implemented.** Persistence + `IRoadAccountingService` interface provided; computation returns "not configured" until rules are supplied | Entire Road Accounting feature |
| 19 | What is `RoadsDb:File` (`/app/data/roads.db`)? SQLite? Who writes/reads it? Schema? | Treated as opaque, externally-owned file: path bound + health-checked only, never written by SMS | Road Accounting design |
| 20 | What are `WALRecovery:Target` / `RecoveryPath` for — which system produces the WAL? | Treated as external-system contract: paths provisioned + health-checked only | Possibly nothing (may be vestigial) |
| 21 | **BLOCKING** — Manifest format and content | JSON document: order header + items + totals + status history + generated timestamp; extension `.json`, prefix `order-manifest-` per existing config | Serializer change only |
| 22 | What is the manifest for (delivery note? receipt? regulatory doc)? Signature/QR needed? | Plain machine-readable JSON, no signature/QR | Feature additions |

| 23 | **BLOCKING** — CSV import schema | Header row, order-insensitive columns: `OrderNumber*, Title*, ItemDescription*, Quantity*, UnitPrice, RequiredByDate, Notes`; one row = one order item; consecutive rows with the same OrderNumber merge into one order | Parser/mapping change |
| 24 | CSV: duplicate OrderNumbers (within file or vs existing) — skip, update, or error? | Skip with row error in report | Import logic |
| 25 | CSV: max file size / row count | 10 MB / 10,000 rows | Validator change |
| 26 | Does a CSV import create orders in `Draft` or `Submitted` status? | `Draft` | Import logic |
| 27 | Attachment: allowed file types for order documents | Reuse existing `FileStorage:AllowedExtensions` list | Config only |
| 28 | Attachment: how many per order? | Max 20 | Validator change |
| 29 | Notifications: should lifecycle transitions notify users (SignalR/email)? | Deferred to a later phase; not in Phase 2-6 scope | Scope |
| 30 | OMS reporting: what reports beyond dashboard counters? | Dashboard status counters + recent orders only | Scope |
| 31 | API route prefix: `oms-orders` (proposed) vs nested `orders` under `/api/v1/oms/`? | `oms-orders` (matches flat existing controller convention) | Route/DTO naming only |
| 32 | Tenant visibility: are OMS orders visible to the Lecturer role at all (view-only)? | Yes, view-only (proposed) | Policy change |
| 33 | Retention: should expired manifests be deleted silently or flagged? | Deleted by background sweep; audit log records the deletion count | Implementation detail |

## Summary

* **BLOCKING items**: 1, 2, 8, 9, 16, 17, 18, 21, 23.
* All other items have safe defaults that can be changed without schema rewrites.
* Road Accounting (#18) is the only area where implementation is genuinely
  **gated**: no calculation will be invented. Everything else proceeds with the
  documented defaults above.
