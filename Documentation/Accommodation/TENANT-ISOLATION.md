# Accommodation tenant isolation

Accommodation is the module that proved tenant isolation works end to end,
and it is the module with the strongest cross-tenant test coverage in the
system.

- `tests/SMS.ApiTests/Controllers/AccommodationApiFixture.cs` — seeds two
  fully populated tenants into real PostgreSQL
- `tests/SMS.ApiTests/Controllers/AccommodationIsolationTests.cs` — **22**
  isolation tests, asserted at the HTTP boundary
- `tests/SMS.IntegrationTests/Database/TenantRowLevelSecurityTests.cs` —
  database-layer proof for the same module

## What is protected

All three tenant-owned tables carry row level security:

| Table | Policies |
|---|---|
| `Lanes` | SELECT, INSERT, UPDATE, DELETE |
| `Houses` | SELECT, INSERT, UPDATE, DELETE |
| `AccommodationAssignments` | SELECT, INSERT, UPDATE, DELETE |

Each uses `tenant_id = app.current_tenant_id()`, and each tenant context
comes from the authenticated, validated request — never from an unchecked
client header. See
[RLS enforcement](../Security/RLS-ENFORCEMENT.md).

## Two test layers, two different claims

### Application layer — `AccommodationIsolationTests` (22 tests)

Issues HTTP requests as **Tenant A only** and asserts that no Tenant B row
appears in a response. Covers lane occupancy reports, student and lecturer
accommodation lists, lane listings, house listings, report exports and role
authorization.

This proves the **EF Core query filter** works.

Its critical property is that every isolation test first calls
`AssertForeignSeedIsReal()`, which opens a **Tenant B** context and proves
the foreign lane, student, lecturer and free house genuinely exist.
Without that, a "pass" could just mean the seed silently failed — or that
the query returned nothing at all.

### Database layer — `TenantRowLevelSecurityTests`

Removes the application-layer defences and shows the database still holds:

- `IgnoreQueryFilters()` — the EF filter is gone; Tenant B's rows are still
  invisible.
- Raw SQL — no ORM, no filter, no interceptor; still invisible, including
  when the foreign row is addressed by primary key.
- Cross-tenant INSERT, UPDATE and DELETE are **rejected**, not merely
  filtered out of a result set.
- A missing, empty or malformed tenant context returns **no rows**.
- A pooled connection cannot carry Tenant A's context into Tenant B's
  request.

## Positive controls

A model that returned nothing would pass every isolation test, so the
positive side is asserted explicitly:

- Tenant A reads its own lanes and houses.
- Tenant B reads its own.
- Tenant A's own INSERT, UPDATE and DELETE still succeed.
- Occupancy reports, exports and the empty-house report still return the
  tenant's own data.

## Attack simulation

`IgnoreQueryFilters()` was temporarily injected into 55 Accommodation read
paths (`AccommodationRepository`, `AccommodationReportRepository`) against
the NOBYPASSRLS runtime role:

```
AccommodationIsolationTests:  22 passed, 0 failed
```

Before RLS the same mutation broke **6 isolation tests** and **2 export
tests**. That contrast is the point: the database now provides protection
that does not depend on the application behaving.

The mutation was reverted immediately. `git diff
src/SMS.Persistence/Repositories/` is empty.

## Notes and known edges

**`Houses.LaneId` can reference another tenant's lane.** Referential
integrity checks run with row security disabled, so a foreign key does not
prevent a cross-tenant reference. It is not a disclosure path — the
referenced lane stays invisible under the `Lanes` policy — but it is a
referential-integrity gap. Hardening would mean composite foreign keys
including `tenant_id`, or a trigger.

**`OccupantId` is a denormalised marker, not a foreign key.** A single FK
cannot enforce both the student and the lecturer case, so referential
integrity for occupants is enforced by `AccommodationAssignments`, which
carries dedicated `StudentId` / `LecturerId` foreign keys with filtered
unique indexes. All three tables are RLS-protected, so a cross-tenant
occupant reference is still not readable.

**The seeding fixture creates Tenant B's data through a Tenant B context**
(`CreateTenantDb(TenantB)`) with both tenant interceptors attached. It must
not use a privileged back door: seeding through the same mechanism
production uses is what keeps the tests honest.