# Row Level Security Enforcement

Status: **enforced.** The application runs as a `NOSUPERUSER`/`NOBYPASSRLS`
role that owns nothing, and PostgreSQL Row Level Security is switched on for
70 tenant-owned tables.

- Cluster: PostgreSQL 16.15 (`postgres:16-alpine`)
- Runtime role: `sms_app` — `NOSUPERUSER`, `NOBYPASSRLS`, `NOCREATEDB`, `NOCREATEROLE`, `LOGIN`
- Migration role: `sms_migration` — `NOSUPERUSER`, `NOBYPASSRLS`, `NOCREATEDB`, `NOCREATEROLE`, `LOGIN`, owns the tables
- RLS enabled tables: **70** (of 71 tenant-scoped tables; `Tenants` is exempt by design)
- Policies: `SELECT`/`INSERT`/`UPDATE`/`DELETE` on every protected table

Related: [Least-privilege roles](../Database/LEAST-PRIVILEGE-ROLES.md) ·
[Rollout and rollback](PRODUCTION-ROLLOUT-AND-ROLLBACK.md) ·
[Accommodation isolation](../Accommodation/TENANT-ISOLATION.md)

---

## 1. Two independent layers

Tenant isolation is enforced twice, on purpose. Neither layer is
sufficient alone.

### Layer 1 — application: EF Core global query filters

`ApplicationDbContext.OnModelCreating` applies a tenant filter to every
entity that has a `Guid TenantId` **and** implements `ITenantAwareEntity`.
The filter captures the `DbContext` instance (not a resolved `Guid`), so it
is re-evaluated per request rather than baked into the cached model.

This layer protects the normal request path. It does **not** protect
anything else:

- `IgnoreQueryFilters()` removes it entirely.
- Raw SQL never goes through the ORM, so it is never applied.
- A background job or a maintenance script may run with no `HttpContext`
  and therefore no tenant.

### Layer 2 — database: PostgreSQL Row Level Security

Policies on every tenant-owned table restrict all four commands to
`tenant_id = app.current_tenant_id()`. PostgreSQL applies them regardless
of how the statement was produced — ORM, raw ADO.NET, `psql`, or a future
service.

### Why both are required

Enabling RLS while the application still connected as a superuser would
have produced *no* enforcement at all. PostgreSQL skips policies
unconditionally for `SUPERUSER` and `BYPASSRLS` roles, and
`FORCE ROW LEVEL SECURITY` does not override that. Before this work the
application connected as the container's `POSTGRES_USER`, which is a
superuser — so 284 tenant policies existed in the database and were never
evaluated by anything.

**Roll them back independently:** the EF filter can be bypassed from
application code; RLS cannot. If the EF filter regresses, the database
still holds. If RLS is rolled back, the EF filter still holds. That is the
defense-in-depth argument, and it is only true because both layers are
tested separately.

---

## 2. Why the role had to change first

Before:

```
rolname    | rolsuper | rolbypassrls | rolcreatedb | rolcreaterole
-----------+----------+--------------+-------------+---------------
testuser   | t        | t            | t           | t
```

`testuser` (production: `sms_user`) is the `POSTGRES_USER` of the official
PostgreSQL image, which the entrypoint creates as a superuser. It also
owned the database, the `app` schema, all 82 tables, both sequences and
both `app` functions.

| Role | SUPERUSER | BYPASSRLS | CREATEDB | CREATEROLE | Owns |
---

## 3. How PostgreSQL receives the tenant

The authenticated, validated tenant reaches PostgreSQL as the
`app.tenant_id` session GUC:

```
TenantResolutionMiddleware
  -> reads X-Tenant-Id (or the configured default)
  -> validates against the Tenants registry (must exist and be active)
  -> stores the registry row's own Id in HttpContext.Items["TenantId"]
ITenantContext
  -> reads HttpContext.Items["TenantId"] lazily
TenantConnectionDbInterceptor / TenantContextDbInterceptor
  -> SELECT set_config('app.tenant_id', @tenant, false)
app.current_tenant_id()
  -> the value the policies compare against
```

### Properties that matter

**The value is never taken from an arbitrary header.** `X-Tenant-Id` is
only a *lookup key*: the middleware resolves it against the `Tenants`
registry and stores the registry row's own `Id`. An unknown or inactive
tenant produces HTTP 400 and never reaches the database layer.

**The value is validated again at the boundary.** Both interceptors accept
it only if it parses as a `Guid`. Anything else — whitespace,
`not-a-guid`, an injection attempt — is published as the empty string,
which `app.current_tenant_id()` maps to the all-zero sentinel.

**The value is bound as a parameter**, never spliced into SQL text.

### Connection pooling

This is the part most likely to leak a tenant, because `set_config(...,
false)` is session-scoped and a physical connection keeps that setting
after it is returned to the pool.

Two interceptors publish the context, and both **fail closed** — if the
context cannot be published, the command is aborted rather than executed
under an unknown tenant:

| Interceptor | Fires on | Covers |
|---|---|---|
| `TenantConnectionDbInterceptor` | `ConnectionOpened` / `ConnectionOpenedAsync` | Everything on the connection, including raw ADO.NET that never touches the ORM |
| `TenantContextDbInterceptor` | every EF Core command | ORM commands, plus any long-lived connection reused inside one scope |
---

## 4. Tables protected

RLS is enabled on every table in `public` that carries a tenant column,
discovered from `information_schema` rather than hard-coded: **70 tables**.

Accommodation: `Houses`, `Lanes`, `AccommodationAssignments`.
Identity: `AspNetUsers`, `AspNetUserRoles`.
OMS: `oms_orders`, `oms_order_items`, `oms_order_imports`,
`oms_order_import_rows`, `oms_order_manifests`, `oms_order_attachments`,
`oms_order_sequences`, `oms_order_status_history`, `oms_road_accounts`.
Requests: `sms_requests`, `sms_request_comments`, `sms_request_attachments`,
`sms_request_status_history`, `sms_request_number_sequences`,
`sms_request_types`.
Academic: `Courses`, `units`, `Enrollments`, `StudentEnrollments`,
`Students`, `Lecturers`, `Grades`, `GradeBands`, `GradingScales`,
`Assessments`, `AssessmentExemptions`, `AssessmentTemplates`,
`AssessmentTypes`, `Attendances`, `Assignments`, `AssignmentSubmissions`,
`assignment_issue_reports`, `CourseOfferingEnrollments`,
`CourseOfferingLecturers`, `CourseOfferingUnits`, `CourseOfferings`,
`Semesters`, `AcademicYears`, `Departments`, `Programmes`, `ProgrammeUnits`,
`UnitAllocations`, `UnitResults`, `Titles`, `Timetables`, `Rooms`,
`Classes`, `Classrooms`, `Buildings`, `Blocks`, `LectureNotes`,
`upload_files`, `AuditLogs`, `Notifications`, `LoginHistories`,
`PasswordResetRequests`, `ReportVerifications`, `ModerationRecords`,
`GradeChangeHistories`, `StudentAssessmentMarks`,
`StudentCertificateEligibilities`, `CertificateRules`.

To list the live set:

```sql
SELECT relname FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname = 'public' AND c.relkind = 'r' AND c.relrowsecurity
 ORDER BY relname;
```

### Deliberately NOT protected

**`Tenants` — the bootstrap registry.** It has a tenant column, but
`TenantStore.GetTenantAsync` must read it to *discover* which tenant a
request belongs to. That read is what establishes the tenant context in
the first place. Policing it is circular: the policy would evaluate
against a tenant that is not yet known, match nothing, and every request
would fail with `400 Invalid tenant`.
### `AspNetUserRoles` uses a different predicate

```sql
"UserId" IN (SELECT u."Id" FROM "AspNetUsers" u
             WHERE u."TenantId" = app.current_tenant_id())
```

rather than `tenant_id = app.current_tenant_id()`.

Enabling RLS exposed a latent data bug here. ASP.NET Identity's
`UserStore` writes an `IdentityUserRole<string>` — the TPH **base** type —
while `TenantId` is declared on the derived `SMS.Domain.Entities.UserRole`.
Under TPH a base-typed instance writes only base columns, so the context's
tenant-stamping loop never matched and **every role assignment in the
database had a NULL `TenantId`**. A policy comparing that column rejected
every role assignment (`new row violates row-level security policy for
table "AspNetUserRoles"`) and would have hidden every existing row,
silently breaking all role authorization.

Deriving the tenant from the owning user is authoritative and cannot be
spoofed by a bogus column value; the sub-select is itself scoped by the
`AspNetUsers` policy. The migration also backfills the historical NULLs,
and `SaveChangesAsync` now stamps the derived-typed rows the application
creates itself.

### `FORCE ROW LEVEL SECURITY` is deliberately not used

The tables are owned by `sms_migration`, which needs unrestricted access to
apply migrations, backfill data and run the bootstrap seed. A table owner is
already exempt from its own table's policies unless `FORCE` is set; `FORCE`
would remove that exemption and break both. The runtime role is not the
owner, so plain `ENABLE` is already **fully enforced for it**.

`app.enable_tenant_rls()` was redefined to `ENABLE`-only so the deployment
helper and the migration cannot drift apart. `FORCE` becomes appropriate
later, once ownership moves to a role that never connects at runtime.

---

## 5. Policy design

Every protected table gets all four commands. A policy set that only
filters `SELECT` would still permit a cross-tenant `UPDATE` or `DELETE`.

```sql
-- SELECT / DELETE
USING (tenant_id = app.current_tenant_id())

-- INSERT
WITH CHECK (tenant_id = app.current_tenant_id())

-- UPDATE: USING scopes which rows may be touched,
--         WITH CHECK blocks moving a row into another tenant
USING      (tenant_id = app.current_tenant_id())
WITH CHECK (tenant_id = app.current_tenant_id())
```

The `WITH CHECK` on `UPDATE` is the one most often omitted. Without it a
tenant could `UPDATE` its own row and set `tenant_id` to another tenant,
effectively donating the record.
`Update_MovingOwnRowIntoAnotherTenant_IsRejectedByTheDatabase` covers it.

---

---

## 7. Testing

### How the RLS tests differ from the application-layer tests

`AccommodationIsolationTests` proves an HTTP request issued as Tenant A
never surfaces Tenant B's rows. That proves the **EF Core query filter**
works.

`TenantRowLevelSecurityTests` / `TenantRowLevelSecurityWriteTests` prove
the **database** refuses, by removing every application-layer defence:

- `IgnoreQueryFilters()` strips the EF filter — the foreign rows are still
  invisible.
- Raw SQL bypasses Entity Framework entirely — still invisible.
- Cross-tenant INSERT/UPDATE/DELETE are rejected, not merely filtered.
- Missing, empty or malformed tenant context yields nothing.
- A pooled connection cannot carry one tenant's context into another's
  request.

Three rules keep these from being vacuous:

1. **Role attributes are asserted first.** A suite that silently ran as a
   superuser would prove nothing, so
   `RuntimeRole_IsLeastPrivilege_NotSuperuserAndNotBypassRls` checks
   `rolsuper`/`rolbypassrls`/`rolcreatedb`/`rolcreaterole` and that the
   role owns no protected table.
2. **Every "cannot see" is paired with ground truth.** The fixture reads
   as the table *owner*, which is exempt from the policies, and asserts
   the foreign row really exists. Without that, a zero-row result is
   indistinguishable from a seed that silently failed.
3. **Positive controls are asserted too.** A model that returned nothing
   would pass every isolation test, so own-tenant reads *and writes* are
   asserted explicitly — including that an own-tenant UPDATE and DELETE
   still succeed.

### Results (PostgreSQL 16.15, RLS enforced, runtime role `sms_app`)

| Suite | Result |
|---|---|
| `SMS.UnitTests` | **723 / 723** |
| `SMS.ApiTests` | **187 / 187** |
| `SMS.IntegrationTests` | **86 / 86** (55 pre-existing + 31 RLS) |
| Accommodation isolation | **22 / 22** |
| OMS | included in the 187 API tests |
| Authentication, reporting | included in the 187 API tests |

### Attack simulation (repeat of the original compromise)

`IgnoreQueryFilters()` was temporarily injected into 55 Accommodation read
paths (`AccommodationRepository`, `AccommodationReportRepository`) against
the NOBYPASSRLS runtime role:

```
AccommodationIsolationTests:  22 passed, 0 failed
```

Before row level security the same mutation broke 6 isolation tests and 2
export tests. The mutation was reverted immediately;
`git diff src/SMS.Persistence/Repositories/` is empty and the full API
suite passes 187/187. That is the demonstration that the database now
provides protection the application layer never had.

---

## 8. Fail-closed behaviour

| Situation | Result |
|---|---|
---

## 9. Remaining risks

1. **Cross-tenant foreign keys are not prevented.** RLS restricts rows,
   not the relationship graph (§6). Not a disclosure path, but a
   referential-integrity gap.
2. **Eleven tables have no tenant column** and so have no database-layer
   protection — including `Certificates`, `AspNetUserClaims` and
   `AspNetUserTokens`. Adding `TenantId` to them is a schema migration.
3. **`Tenants` is unprotected by necessity.** Documented in §4.
4. **`AutomaticCertificateGenerator` runs with no tenant.** It executes on
   a 24-hour timer in a background scope with no `HttpContext`, so
   `ITenantContext.TenantId` is empty and it now sees no rows. This is
   **unchanged behaviour** — the EF query filter already resolved to
   `Guid.Empty` and returned nothing — but it means automatic certificate
   generation is effectively dormant. RLS made this *visible*, not *new*;
   it deserves its own ticket.
5. **`FORCE ROW LEVEL SECURITY` is not enabled**, so the owning role
   (`sms_migration`) is exempt. Correct for now; revisit when ownership
   moves to a role that never connects at runtime.
6. **Four globally-seeded reference tables** (`AssessmentTypes`,
   `CertificateRules`, `GradeBands`, `GradingScales`) contain rows with
   the all-zero tenant, written by seed migrations. Those specific rows
   are protected and therefore invisible to every tenant. Confirmed
   harmless — the application creates and reads these through its own
   tenant-scoped paths — but worth confirming they are not relied on as
   global lookups.
7. **`__EFMigrationsHistory` and `__EFMigrationsHistory`-adjacent
   operations run as the migration role**, not the runtime role.
| No tenant resolved | `app.tenant_id = ''` → sentinel → **no rows** |
| Malformed tenant | published as empty → sentinel → **no rows** |
| Well-formed but unknown tenant | matches nothing → **no rows** |
| Tenant context unavailable (publish fails) | **command aborted** — never run under an unknown tenant |
| Row with NULL tenant column | `NULL = current_tenant()` is not true → **invisible** |

The model is *no tenant context = no tenant rows*, never *all rows*.

`InvalidTenantContext_ReturnsNoRows` covers the malformed cases and
`PooledConnection_AfterATenantRequest_AnUnresolvedContextSeesNothing` covers
the worst-case leak.
## 6. Foreign keys and cross-tenant joins

Referential-integrity checks are performed with row security disabled, so a
foreign key does **not** prevent a row in Tenant A from referencing a row
belonging to Tenant B. RLS restricts the row, not the graph.

Concretely: a `House` in Tenant A can carry a `LaneId` pointing at a Tenant
B lane, because the `Houses` policy only constrains `Houses.tenant_id`.

This is **not** a data-disclosure leak — Tenant B's lane row is still
invisible to Tenant A, and the `Lanes` policy blocks reading it — but it is
a referential-integrity gap. Hardening options, none applied here because
each is a schema change with its own migration risk: composite foreign keys
including `tenant_id` on both sides, or a `BEFORE INSERT OR UPDATE` trigger
validating that the referenced row belongs to the same tenant. Recorded
under Remaining risks.

Its four policies are **dropped** by the migration rather than left inert,
so the exemption cannot be undone by a well-meaning "just enable it on
everything". `Tenants_RegistryRemainsReadableWithoutATenantContext` pins
this.

Residual risk: a caller able to reach a tenant-listing endpoint would
learn the set of tenant ids. No such endpoint exists; the registry is read
only by id, inside the middleware, before authentication.

**11 tables with no tenant column** — `AspNetRoles`, `AspNetRoleClaims`,
`AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `Certificates`,
`CertificateTemplates`, `CertificateAuditLogs`, `DigitalSignatures`,
`CourseProgrammes`, `__EFMigrationsHistory`. Roles and role claims are
genuinely global. `AspNetUserClaims`, `AspNetUserTokens` and `Certificates`
are reachable only through a user or student row the application has
already resolved, so there is no direct leak path — but they are not
independently protected. See Remaining risks.

Publishing on connection **open** is what makes pooling safe by
construction: Npgsql rents a physical connection per scope and returns it
on close, so every new rental rewrites the context before anything can
read it. Both interceptors deliberately write even when no tenant is
resolved — an earlier version *skipped* the write for the tenant
resolution read, which left a stale tenant standing on the pooled
connection.

> **Rule for raw ADO.NET.** Open through Entity Framework —
> `db.Database.OpenConnectionAsync()`, not `GetDbConnection().OpenAsync()`.
> Only the former raises `ConnectionOpened` and therefore publishes the
> context. `OrderNumberGenerator` and `RequestNumberGenerator` already do
> this; two test helpers did not and were corrected.
|---|---|---|---|---|---|
| `sms_app` | no | **no** | no | no | nothing |
| `sms_migration` | no | **no** | no | no | schema objects |

Neither role has `BYPASSRLS`. `sms_migration` is exempt from the policies
because it **owns** the tables, not because of the attribute — so it still
cannot bypass a policy on an object it does not own. That distinction
matters: a `BYPASSRLS` credential bypasses everything in the database,
including tables added by other teams.