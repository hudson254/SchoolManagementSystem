# Production rollout and rollback

How to move a deployment onto the least-privilege role and enforced row
level security, and how to undo it.

See also [RLS enforcement](RLS-ENFORCEMENT.md) and
[Least-privilege roles](../Database/LEAST-PRIVILEGE-ROLES.md).

## Principle: the two changes are independent and separately reversible

1. **Role change** — move the application onto `sms_app` / `sms_migration`.
   RLS stays **off**.
2. **RLS enablement** — the `20260930120000_EnableTenantRowLevelSecurity`
   migration.

Doing (1) first and verifying it before (2) is the whole point. If RLS is
enabled while the application still runs as a superuser there is no
enforcement, and if the role is changed and RLS is left on, every query
that does not carry a tenant context silently returns nothing.

Each has its own rollback and neither requires a database restore.

---

## Stages

### Stage 1 — create and validate the roles (RLS still off)

```bash
# On the target (production or a restored clone).
SMS_APP_PASSWORD="$NEW_APP_PASSWORD" \
SMS_MIGRATION_PASSWORD="$NEW_MIGRATION_PASSWORD" \
DB_HOST=... DB_PORT=... DB_NAME=SchoolManagementSystem \
DB_SUPERUSER=sms_user DB_SUPERPASSWORD="$DB_PASSWORD" \
./scripts/provision-least-privilege-roles.sh
```

Verify with the queries in
[LEAST-PRIVILEGE-ROLES.md](../Database/LEAST-PRIVILEGE-ROLES.md#verifying).
All three privilege probes must be `false`.

Nothing about the running application changes yet. This stage is safe to
repeat.

### Stage 2 — run production as `sms_app`, RLS still off

Set on the API service:

```yaml
environment:
  ConnectionStrings__DefaultConnection: "Host=postgres;Database=SchoolManagementSystem;Username=sms_app;Password=${SMS_DB_APP_PASSWORD}..."
  ConnectionStrings__MigrationConnection: "Host=postgres;Database=SchoolManagementSystem;Username=sms_migration;Password=${SMS_DB_MIGRATION_PASSWORD}..."
```

Recreate the API container and confirm:

- `/health` returns 200.
- Login, `/me`, refresh and password reset work.
- Accommodation lists, reports and exports work.
- OMS requests, orders, attachments and comments work.
- No `permission denied` in the logs — a missing grant is the first thing
  to suspect.

Leave it running for a full business cycle. Nothing has changed
functionally: the runtime role has exactly the DML the application was
already performing.

**Rollback for this stage:** restore the previous
`ConnectionStrings__DefaultConnection` and recreate the container. The
`sms_user` superuser role is untouched and still works.

### Stage 3 — enable RLS on the clone or staging database first

Never enable RLS on production first.

```bash
# A fresh cluster must be able to provision the roles itself. Verify that first.
docker compose -f docker/docker-compose.test.yml down -v
docker compose -f docker/docker-compose.test.yml up -d

# Restore a production-representative clone, then:
SMS_DESIGN_TIME_CONNECTION="Host=...;Database=<clone>;Username=sms_migration;Password=..." \
  dotnet ef database update --project src/SMS.Persistence
dotnet test tests/SMS.UnitTests
dotnet test tests/SMS.ApiTests
dotnet test tests/SMS.IntegrationTests
```

Expect `769 / 187 / 120`. Run the three suites **sequentially**: running
two of them concurrently against one PostgreSQL server produces spurious
connection failures that look like regressions.

The integration suite provisions its own database (`sms_rls_test`) and
runs 31 database-level RLS tests plus 34 privilege/role-escalation tests
against a real `NOBYPASSRLS` connection. The RLS classes share one
collection fixture and must not run against each other; see
`TenantRowLevelSecurityCollection`.

The counts moved from `723 / 187 / 86` when the least-privilege roles were
wired into the deployment configuration: 46 unit tests were added for the
production migration-connection contract and a static audit of the compose
files, and 34 integration tests for the `Tenants` privilege boundary and
role escalation. No existing test was removed, skipped or weakened.

### Stage 4 — back up production

```bash
docker compose -f docker/docker-compose.prod.yml exec -T postgres \
  pg_dump -U sms_user -Fc SchoolManagementSystem > /var/backups/sms/pre-rls-$(date +%Y%m%d_%H%M%S).dump
```

Verify the dump is non-empty **and** restorable before continuing. A
backup nobody has checked is not a backup.

### Stage 5 — enable RLS in production

```bash
SMS_DB_MIGRATION_PASSWORD=... docker compose -f docker/docker-compose.prod.yml \
  run --rm api migrate-database
```

### Stage 6 — verify and monitor

```sql
SELECT count(*) FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname='public' AND c.relkind='r' AND c.relrowsecurity;   -- expect 70
```

Then repeat the live tenant-isolation check: authenticate as a user of
tenant A, request an accommodation report, and confirm no tenant B row
appears. Watch the logs for `new row violates row-level security policy`,
which indicates a code path writing without a tenant context.
---

## Rollback

The recommendation is **not** to restore the database. Neither change
requires it.

### Symptom: requests return empty results (RLS too strict)

Most likely a code path running with no tenant context.

```bash
# Disable RLS without touching roles, grants or data.
SMS_DESIGN_TIME_CONNECTION="Host=...;Database=SchoolManagementSystem;Username=sms_migration;Password=..." \
  dotnet ef database update 20260928153648_AddAccommodationReportIndexes --project src/SMS.Persistence
```

`Down` disables RLS on every table it finds enabled and restores the
`Tenants` policies. The application keeps running as `sms_app`, which is
still the safe state. Re-enable with `dotnet ef database update` once the
defect is fixed.

```sql
-- Confirm it actually rolled back.
SELECT count(*) FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname='public' AND c.relkind='r' AND c.relrowsecurity;
```

### Symptom: `permission denied` (a grant is missing)

```sql
SELECT has_table_privilege('sms_app', 'public."<Table>"', 'SELECT,INSERT,UPDATE,DELETE');
```

Add the missing grant and re-run
`docker/grant-least-privilege-privileges.sql`. It is idempotent and will
not disturb RLS.

### Symptom: the application cannot start (migrations fail)

Check the order of operations. The common cause is applying migrations
over the runtime connection. Confirm
`ConnectionStrings__MigrationConnection` is set and that `sms_migration`
owns the objects:

```sql
SELECT pg_get_userbyid(relowner), count(*) FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname='public' AND c.relkind IN ('r','S') GROUP BY 1;
```

Re-run `docker/grant-least-privilege-roles.sh`; it transfers ownership and
can be re-run safely.

### Symptom: total outage, need the previous posture back

```bash
# 1. Point the API back at the bootstrap role and recreate.
#    ConnectionStrings__DefaultConnection -> Username=sms_user
docker compose -f docker/docker-compose.prod.yml up -d --force-recreate api

# 2. Confirm health.
curl -fsS https://<host>/health
```

This restores the old behaviour exactly. The new roles, grants and policies
are all still in place and can be re-adopted at leisure.

### What rollback does NOT require

A database restore. The migration only runs `ALTER TABLE`, creates and
drops policies, and performs one idempotent data backfill on
`AspNetUserRoles.TenantId` (copied from `AspNetUsers.TenantId`; rows with
no resolvable user keep `NULL` and stay invisible, which is the fail-closed
outcome). A restore would be more destructive than the problem.

---

## Deployment checklist

- [ ] Clone validated: same schema, roles, grants, extensions, migration
      history and data volume as production
- [ ] Full suite green on the clone with RLS enabled
- [ ] `scripts/provision-least-privilege-roles.sh` re-run against
      production; verification queries return the expected values
- [ ] `ConnectionStrings__DefaultConnection` and `MigrationConnection` set
      from the secret store; **no password in compose or git**
- [ ] Production backup taken **and verified restorable**
- [ ] Rollback rehearsed on the clone
- [ ] Post-enable: `/health` 200, login works, one live cross-tenant
      isolation check performed, logs clean
- [ ] Backups still running under the previous (unchanged) credentials