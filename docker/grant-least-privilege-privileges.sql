-- ==================================================================
-- School Management System - Least-privilege grants (EXISTING DATABASE)
-- ==================================================================
-- This is the script that matters for an already-migrated database: it
-- is the upgrade path used by production, by a restored clone, and by
-- the local test cluster.
--
-- docker-entrypoint-initdb.d only runs when PGDATA is empty, so
-- init-db-least-privilege.sql never runs against an existing volume.
-- This script has to be re-runnable against a live schema and is
-- therefore fully idempotent.
--
-- Must be executed by a role that can change ownership (the bootstrap
-- superuser, or any role that already owns every table).
--
-- Run it with:
--   psql -v ON_ERROR_STOP=1 -d <database> -f docker/grant-least-privilege-privileges.sql
--
-- Idempotent: safe to re-run.
-- ==================================================================

\set ON_ERROR_STOP on

-- ------------------------------------------------------------------
-- 0. Refuse to run if the roles are missing, rather than failing later
--    with a confusing "permission denied for schema public".
-- ------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_app') THEN
        RAISE EXCEPTION
            'Role sms_app does not exist. Run docker/init-db-least-privilege.sql first.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_migration') THEN
        RAISE EXCEPTION
            'Role sms_migration does not exist. Run docker/init-db-least-privilege.sql first.';
    END IF;
END
$$;

-- ------------------------------------------------------------------
-- 1. Hand ownership of the schema's objects to sms_migration.
-- ------------------------------------------------------------------
-- Why ownership moves at all: the tables are currently owned by the
-- runtime role (POSTGRES_USER of the postgres image, a superuser). A
-- superuser owner can never be subject to RLS, and it means the runtime
-- credentials are a key to the entire cluster.
--
-- sms_migration is NOSUPERUSER and NOBYPASSRLS. Because it OWNS the
-- tables it is exempt from their policies, which is exactly what a
-- migration/seed role needs, but it cannot bypass policies on anything
-- it does not own.
--
-- Ownership of the `public` SCHEMA itself is deliberately left alone.
-- PG15+ assigns it to pg_database_owner, and changing it has no security
-- benefit here; sms_migration gets CREATE on it instead.
GRANT CREATE, USAGE ON SCHEMA public TO sms_migration;

-- PostgreSQL 15 changed CREATE SCHEMA: creating a new schema now requires
-- the CREATE privilege on the *database*, not merely on a schema. The RLS
-- migration runs CREATE SCHEMA IF NOT EXISTS app, so without this the
-- migration fails with "42501 permission denied for database <name>" the
-- first time the RLS infrastructure is created on a freshly provisioned
-- database. Granted to sms_migration only - never to the runtime role.
DO $$
DECLARE
    v_db text := current_database();
BEGIN
    EXECUTE format('GRANT CONNECT, CREATE ON DATABASE %I TO sms_migration', v_db);
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO sms_app', v_db);
END
$$;

DO $$
DECLARE
    r record;
BEGIN
    -- Tables/views first. A sequence that backs a serial/identity column
    -- cannot be re-pointed on its own ("cannot change owner of sequence ...
    -- it is linked to table ..."), but it follows its table automatically,
    -- so transferring the table transfers the sequence with it. Doing the
    -- tables first is therefore required, not merely tidier.
    FOR r IN
        SELECT c.relname, c.relkind
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind IN ('r', 'v', 'm', 'f')
          AND pg_get_userbyid(c.relowner) <> 'sms_migration'
    LOOP
        EXECUTE format('ALTER %s public.%I OWNER TO sms_migration',
                       CASE r.relkind
                           WHEN 'v' THEN 'VIEW'
                           WHEN 'm' THEN 'MATERIALIZED VIEW'
                           WHEN 'f' THEN 'FOREIGN TABLE'
                           ELSE 'TABLE'
                       END,
                       r.relname);
    END LOOP;

    -- Standalone sequences (not attached to a column). Any that were
    -- attached have already moved with their table.
    FOR r IN
        SELECT c.relname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind = 'S'
          AND pg_get_userbyid(c.relowner) <> 'sms_migration'
    LOOP
        EXECUTE format('ALTER SEQUENCE public.%I OWNER TO sms_migration', r.relname);
    END LOOP;
END
$$;

-- The `app` schema holds the RLS helper functions and is created by the
-- RLS migration. It is owned by the runtime role today, so move it too.
--
-- Function ownership has to be transferred explicitly: CREATE OR REPLACE
-- FUNCTION and ALTER FUNCTION both require the caller to own the function,
-- not merely the schema that contains it. Without this the RLS migration
-- fails with "42501: must be owner of function enable_tenant_rls".
DO $$
DECLARE
    r record;
BEGIN
    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'app') THEN
        EXECUTE 'ALTER SCHEMA app OWNER TO sms_migration';

        FOR r IN
            SELECT p.oid::regprocedure AS signature
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE n.nspname = 'app'
              AND pg_get_userbyid(p.proowner) <> 'sms_migration'
        LOOP
            EXECUTE format('ALTER FUNCTION %s OWNER TO sms_migration', r.signature);
        END LOOP;
    END IF;
END
$$;

-- ------------------------------------------------------------------
-- 2. Grant the runtime role exactly the privileges it needs.
-- ------------------------------------------------------------------
-- Enumerated, never "ALL PRIVILEGES". The runtime role is deliberately
-- NOT granted:
--   * CREATE on any schema   - it must never define objects or policies
--   * REFERENCES / TRUNCATE  - it must never act on another table's keys
--   * any function DDL
GRANT USAGE ON SCHEMA public TO sms_app;
REVOKE CREATE ON SCHEMA public FROM sms_app;

-- Runtime DML. This is the complete set the application performs: EF Core
-- SELECT/INSERT/UPDATE/DELETE plus ASP.NET Identity's user/role work.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO sms_app;

-- Sequence use. Only two identity sequences exist, but a future Identity
-- mapping may add more, and a missing USAGE grant is a runtime crash.
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO sms_app;

-- PostgreSQL grants EXECUTE on every function to PUBLIC by default.
-- app.current_tenant_id() is invoked by the RLS policies themselves, so
-- the runtime role must keep EXECUTE on it. app.enable_tenant_rls(text)
-- performs ALTER TABLE and must NOT be reachable from the runtime role,
-- so it is revoked rather than left on the default grant.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'app'
          AND p.proname = 'current_tenant_id'
    ) THEN
        EXECUTE 'GRANT USAGE ON SCHEMA app TO sms_app';
        EXECUTE 'GRANT EXECUTE ON FUNCTION app.current_tenant_id() TO sms_app';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'app'
          AND p.proname = 'enable_tenant_rls'
    ) THEN
        EXECUTE 'REVOKE EXECUTE ON FUNCTION app.enable_tenant_rls(text) FROM PUBLIC';
        EXECUTE 'REVOKE EXECUTE ON FUNCTION app.enable_tenant_rls(text) FROM sms_app';
    END IF;
END
$$;

-- Nothing in the database should be writable by an anonymous connection.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

-- ------------------------------------------------------------------
-- 3. Keep the migration history readable.
-- ------------------------------------------------------------------
-- Program.cs runs "SELECT ... FROM __EFMigrationsHistory" on every boot
-- before it decides whether to migrate. That table is not tenant-owned,
-- so it needs a plain SELECT and no RLS.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_tables WHERE schemaname = 'public'
          AND tablename = '__EFMigrationsHistory'
    ) THEN
        EXECUTE 'GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO sms_app';
    END IF;
END
$$;

-- ------------------------------------------------------------------
-- 4. Default privileges, so the next migration cannot produce a table
--    the runtime role is locked out of.
-- ------------------------------------------------------------------
ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO sms_app;

ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO sms_app;

-- ------------------------------------------------------------------
-- 5. Verification. The script reports success; these checks make a
--    partially-applied run impossible to mistake for a good one.
-- ------------------------------------------------------------------
DO $$
DECLARE
    missing text;
BEGIN
    SELECT string_agg(c.relname, ', ')
      INTO missing
      FROM pg_class c
      JOIN pg_namespace n ON n.oid = c.relnamespace
     WHERE n.nspname = 'public'
       AND c.relkind = 'r'
       AND NOT has_table_privilege('sms_app', c.oid, 'SELECT')
       AND NOT has_table_privilege('sms_app', c.oid, 'INSERT')
       AND NOT has_table_privilege('sms_app', c.oid, 'UPDATE')
       AND NOT has_table_privilege('sms_app', c.oid, 'DELETE');

    IF missing IS NOT NULL THEN
        RAISE EXCEPTION 'sms_app is missing runtime DML on: %', missing;
    END IF;
END
$$;

-- The runtime role must never hold a privilege that would let it change
-- the schema, define a policy, or act on somebody else's data wholesale.
DO $$
DECLARE
    bad text;
BEGIN
    IF has_schema_privilege('sms_app', 'public', 'CREATE') THEN
        bad := 'CREATE on schema public';
    ELSIF has_database_privilege('sms_app', current_database(), 'CREATE') THEN
        bad := 'CREATE on database';
    ELSIF pg_has_role('sms_app', 'sms_migration', 'MEMBER') THEN
        bad := 'membership of sms_migration';
    END IF;

    IF bad IS NOT NULL THEN
        RAISE EXCEPTION 'sms_app must not hold: %', bad;
    END IF;
END
$$;