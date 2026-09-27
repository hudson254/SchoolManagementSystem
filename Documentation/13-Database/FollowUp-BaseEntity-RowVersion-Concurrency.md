# Follow-Up: `BaseEntity.RowVersion` Provides Ineffective Optimistic Concurrency

| | |
|---|---|
| **Status** | Open — deliberately NOT addressed by the enrollment entity-state repair |
| **Severity** | High (data integrity / security-relevant), but **not** a live outage |
| **Type** | Concurrency hardening / technical debt |
| **Raised** | 2026-09-27, during the enrollment entity-state repair |
| **Related** | `src/SMS.Domain/Common/BaseEntity.cs` |

## Summary

`BaseEntity.RowVersion` is mapped as a concurrency token but the stored value
stays NULL, so the optimistic-concurrency check it appears to provide does not
actually protect anything. This is a genuine weakness and should be fixed — but
it is a **separate, independently testable change** and must not be bundled
with unrelated repairs.

## The defect

```csharp
// src/SMS.Domain/Common/BaseEntity.cs
[Column("row_version")]
[Timestamp]
public byte[]? RowVersion { get; set; }
```

`[Timestamp]` tells EF Core this is a store-generated concurrency token, so EF
adds `row_version` to the `WHERE` clause of every `UPDATE` and expects the
database to populate and return it. Nothing ever populates it: the column is
`bytea`, has no default, and PostgreSQL has no trigger to maintain it. The
value therefore remains NULL on every row.

## Observed behaviour

A two-context test on real PostgreSQL demonstrated the consequence:

```text
Context A: saved successfully.
Context B: stale write accepted.   <-- the protection did not engage
```

The same test against PostgreSQL's `xmin` system column correctly **rejected**
the stale write, confirming the mechanism works when the token is actually
maintained.

## Why it is not the cause of the enrollment HTTP 500

During the enrollment repair the 500 was conclusively traced to a different and
immediate cause: EF Core classified newly created `Enrollment` entities reached
through a navigation collection as `Modified` instead of `Added`, issued an
`UPDATE` for a row that did not exist, and raised `DbUpdateConcurrencyException`.
That was fixed by persisting new enrollments through the repository
(`IEnrollmentRepository.AddAsync`).

The `RowVersion` weakness did not contribute to that failure and was left
untouched on purpose.

## Recommended remediation (future work)

Migrate the concurrency token to PostgreSQL's `xmin` system column, or adopt
another deliberate strategy. Note that `Order` and `Request` have **already**
been given a PostgreSQL-native concurrency mapping:

```csharp
// src/SMS.Persistence/Data/ApplicationDbContext.cs
modelBuilder.Entity<Order>().Property<uint>("xmin").IsRowVersion();
```

A follow-up should decide deliberately between:

1. **`xmin` everywhere** (consistent with `Order`/`Request`; no schema change
   required because `xmin` is a system column excluded from DDL), or
2. **an application-managed token** (e.g. a trigger or a generated `uuid`
   version column bumped on write), or
3. **removing the token** where no optimistic concurrency is actually required,
   so the model stops advertising protection it does not have.

Any option touching many tables must be scoped, reviewed and tested on its own.

## Do not conflate with

- The enrollment entity-state repair (`044b4d4`) — a different root cause.
- The deferred PostgreSQL Row Level Security configuration — also separate, and
  likewise deliberately not enabled as part of these repairs.
