# Least-privilege database roles

Two PostgreSQL roles replace the previous arrangement, in which the
application connected as the container's `POSTGRES_USER` — a superuser with
`BYPASSRLS`, `CREATEDB` and `CREATEROLE` that also owned every object.

See also [RLS enforcement](../Security/RLS-ENFORCEMENT.md).

## The roles

| | `sms_app` (runtime) | `sms_migration` (DDL) |
|---|---|---|
| Used by | API, OMS, reports, background workers | `migrate-database`, `seed-data`, EF migrations |
| `SUPERUSER` | no | no |
| `BYPASSRLS` | **no** | **no** |
| `CREATEDB` / `CREATEROLE` | no / no | no / no |
| Owns | nothing | database schema objects |
| Exempt from RLS? | no | yes — **because it owns the tables** |
| Connection string | `DefaultConnection` | `MigrationConnection` |

`sms_migration` is deliberately **not** given `BYPASSRLS`. A table owner is
already exempt from its own table's policies, so ownership is sufficient
for migrations while leaving the role unable to bypass policies on objects
it does not own. No role in the cluster holds `BYPASSRLS` except the
cluster bootstrap superuser.

## Why migrations needed their own connection

EF Core migrations were applied over the same connection string the
application used for requests. That only worked because the connection was
a superuser. Keeping it that way would have forced the runtime role to stay
over-privileged.

`DatabaseMigrationRunner`
(`src/SMS.Persistence/Data/DatabaseMigrationRunner.cs`) resolves
`ConnectionStrings:MigrationConnection` and falls back to
`DefaultConnection` for un-provisioned deployments — **logging a warning**,
so a misconfigured production box cannot silently keep running DDL as the
application role.

Used by: the `migrate-database` CLI, the `seed-data` CLI, the startup
migration gate in `Program.cs`, `DatabaseSeeder`, and the API test
fixtures.

## Privileges granted to `sms_app`

Enumerated, never `ALL PRIVILEGES`.

```sql
-- Schemas
GRANT USAGE ON SCHEMA public TO sms_app;
GRANT USAGE ON SCHEMA app   TO sms_app;   -- if present
REVOKE CREATE ON SCHEMA public FROM sms_app;

-- Tables: this is the complete set the application performs
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO sms_app;

-- Sequences
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO sms_app;

-- Functions
GRANT EXECUTE ON FUNCTION app.current_tenant_id() TO sms_app;  -- policies call it
REVOKE EXECUTE ON FUNCTION app.enable_tenant_rls(text) FROM PUBLIC, sms_app;

-- Database
GRANT CONNECT ON DATABASE <db> TO sms_app;

-- Startup migration gate reads this
GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO sms_app;
```

Deliberately **not** granted: `CREATE` on any schema, `REFERENCES`,
`TRUNCATE`, membership of `sms_migration`, `CREATE` on the database, or any
function DDL.

`ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration` repeats the table and
sequence grants so a table created by the next migration cannot be
inaccessible to the runtime role.

## Non-obvious requirement: database-level CREATE

PostgreSQL 15 changed `CREATE SCHEMA`: creating a schema now requires
`CREATE` on the **database**, not just on a schema. The RLS migration runs
`CREATE SCHEMA IF NOT EXISTS app`, so without

```sql
GRANT CONNECT, CREATE ON DATABASE <db> TO sms_migration;
```

it fails with `42501: permission denied for database <db>` on a freshly
---

## Ownership transfer

Tables and views are transferred first, then standalone sequences. A
sequence backing a serial/identity column cannot be re-pointed on its own
(`cannot change owner of sequence ... it is linked to table ...`); it
follows its table automatically, so the ordering is required.

The `app` schema **and the functions inside it** are transferred
explicitly. `CREATE OR REPLACE FUNCTION` requires ownership of the
function, not merely of its schema — without this the RLS migration fails
with `42501: must be owner of function enable_tenant_rls`.

Ownership of the `public` schema itself is left alone (PG15+ assigns it to
`pg_database_owner`); `sms_migration` is granted `CREATE` instead.

---

## Provisioning

### Fresh install

`docker/init-db-least-privilege.sql` runs from
`/docker-entrypoint-initdb.d` as the bootstrap superuser, before any table
exists. It creates the roles and grants the schema rights needed for the
first migration.

Add it to the compose `volumes` alongside the existing init scripts.

### Existing database (production, clones, the test cluster)

`docker-entrypoint-initdb.d` only runs when `PGDATA` is empty, so the above
never runs against an existing volume. Use:

```bash
SMS_APP_PASSWORD=... SMS_MIGRATION_PASSWORD=... \
DB_HOST=... DB_PORT=... DB_NAME=... \
DB_SUPERUSER=... DB_SUPERPASSWORD=... \
./scripts/provision-least-privilege-roles.sh
```

which applies both scripts and assigns passwords from the environment. The
script is idempotent and **verifies the resulting role attributes before
reporting success**, so a partially-applied run cannot be mistaken for a
good one.

### Credentials

No password appears in any SQL file, migration, source file, document or
image. Passwords are supplied at provisioning time from the deployment
secret store (the same `.env` file the compose files already consume) and
are passed to `psql` as variables, then interpolated with `format('%L')`.

`ApplicationDbContextFactory` previously embedded a username and password
and pointed `dotnet ef` at a superuser. It now resolves from
`SMS_DESIGN_TIME_CONNECTION`, then the nearest `appsettings.json`, then the
standard environment variable, and finally falls back to a
**credential-free** placeholder that cannot connect anywhere.

---

## Verifying

```sql
-- Roles are least privilege
SELECT rolname, rolsuper, rolbypassrls, rolcreatedb, rolcreaterole
  FROM pg_roles WHERE rolname IN ('sms_app','sms_migration');

-- The runtime role owns nothing
SELECT count(*) FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname='public' AND pg_get_userbyid(c.relowner) = 'sms_app';

-- The migration role owns the objects
SELECT pg_get_userbyid(relowner), count(*) FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname='public' AND c.relkind IN ('r','S')
 GROUP BY 1;

-- The runtime role cannot escalate
SELECT has_schema_privilege('sms_app','public','CREATE')  AS create_schema,
       has_database_privilege('sms_app', current_database(), 'CREATE') AS create_db,
       pg_has_role('sms_app','sms_migration','MEMBER')     AS is_member;
```

All three privilege probes must be `false`.

---

## Other database principals

| Principal | Role | Notes |
|---|---|---|
| Application runtime | `sms_app` | this document |
| Migrations / seeding | `sms_migration` | this document |
| Backup service (`sms-backup`) | still `DB_USER` from compose | runs `pg_dump`, needs full read — **not yet separated**; see below |
| Monitoring | none | Prometheus scrapes HTTP metrics only; no database exporter |
| Cluster bootstrap | `sms_user` / `testuser` | superuser; provisioning and restore only, never used by the application |

The backup role is the one remaining principal still sharing credentials
with the cluster bootstrap role. Separating it needs a role with `CONNECT`
plus read access to every table, and is tracked as follow-up work rather
than bundled here — losing a backup is worse than the current
arrangement, and it deserves its own tested change.
provisioned database. Granted to `sms_migration` only — never to
`sms_app`.