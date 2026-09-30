-- ==================================================================
-- School Management System - Least-privilege database roles (FRESH INSTALL)
-- ==================================================================
-- Runs once, in /docker-entrypoint-initdb.d, as the cluster bootstrap
-- superuser (POSTGRES_USER) BEFORE any migration has created a table.
--
-- It therefore does two things and nothing else:
--
--   1. Creates the two long-lived application roles.
--   2. Gives the migration role the schema rights it needs to create the
--      objects that the later migration run will produce.
--
-- It deliberately does NOT grant table privileges and does NOT touch
-- ownership: at this point in a fresh install there are no tables yet.
-- Those concerns belong to `grant-least-privilege-privileges.sql`, which
-- runs after migrations and is also the upgrade path for an existing
-- database.
--
-- NO PASSWORDS ARE SET HERE. A password would have to live in this file,
-- which means it would live in git. Role passwords are assigned out of
-- band from the deployment secret store - see
-- scripts/provision-least-privilege-roles.sh and
-- Documentation/Security/RLS-ENFORCEMENT.md.
--
-- Idempotent: safe to re-run.
-- ==================================================================

-- ------------------------------------------------------------------
-- 1. The migration / schema-owner role.
\set ON_ERROR_STOP on
-- ------------------------------------------------------------------
-- NOSUPERUSER and, importantly, NOBYPASSRLS. This role does NOT need the
-- BYPASSRLS attribute: it bypasses row level security because it OWNS the
-- tables (a table owner is exempt from that table's policies unless the
-- table is additionally FORCEd). Owning the schema is what lets EF Core
-- apply migrations. Keeping the attribute off means that even if this role's
-- credentials leak, it cannot walk straight past every policy in the
-- database - it can only bypass the policies on the objects it owns.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_migration') THEN
        CREATE ROLE sms_migration
            LOGIN
            NOSUPERUSER
            NOCREATEDB
            NOCREATEROLE
            NOINHERIT
            NOBYPASSRLS;
    END IF;

    -- ------------------------------------------------------------------
    -- 2. The runtime application role. This is the role the API, the
    --    OMS, the reporting queries and the background workers all use.
    --
    --    NOSUPERUSER + NOBYPASSRLS is the whole point of this migration:
    --    it is what makes ENABLE ROW LEVEL SECURITY actually mean
    --    something. The remaining attributes keep a compromised API from
    --    escalating inside the cluster.
    -- ------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_app') THEN
        CREATE ROLE sms_app
            LOGIN
            NOSUPERUSER
            NOCREATEDB
            NOCREATEROLE
            INHERIT
            NOBYPASSRLS;
    END IF;

    -- The runtime role must never be able to become the migration role.
    -- Guarded on the membership actually existing, so the script is quiet
    -- on a clean install and still removes a membership introduced by
    -- mistake or by an earlier, looser configuration.
    IF EXISTS (
        SELECT 1
        FROM pg_auth_members am
        JOIN pg_roles grantee ON grantee.oid = am.member
        JOIN pg_roles granted  ON granted.oid  = am.roleid
        WHERE grantee.rolname = 'sms_app'
          AND granted.rolname  = 'sms_migration'
    ) THEN
        EXECUTE 'REVOKE sms_migration FROM sms_app';
    END IF;
END
$$;

-- ------------------------------------------------------------------
-- 3. Schema privileges required before the first migration runs.
-- ------------------------------------------------------------------
-- CREATE on public  -> sms_migration may create the tables.
-- USAGE  on public  -> sms_app can resolve the names in its queries.
-- NO CREATE for sms_app: the runtime role must never be able to define
-- objects, a function, or a policy.
GRANT CREATE, USAGE ON SCHEMA public TO sms_migration;
GRANT USAGE ON SCHEMA public TO sms_app;

-- PostgreSQL 15 changed CREATE SCHEMA: creating a new schema now requires
-- the CREATE privilege on the *database*, not merely on a schema. The RLS
-- migration does CREATE SCHEMA IF NOT EXISTS app, so without this the
-- migration fails with "42501 permission denied for database <name>" the
-- first time the RLS infrastructure is created on a freshly provisioned
-- database. It is granted to sms_migration only; the runtime role must
-- never hold it (see the verification block at the end of
-- grant-least-privilege-privileges.sql).
DO $$
DECLARE
    v_db text := current_database();
BEGIN
    EXECUTE format('GRANT CONNECT, CREATE ON DATABASE %I TO sms_migration', v_db);
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO sms_app', v_db);
END
$$;

-- PUBLIC must not be able to create objects in the application schema.
-- PG15+ already removes this by default, but stating it keeps the
-- guarantee independent of the cluster's creation version.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

-- ------------------------------------------------------------------
-- 4. Default privileges for objects sms_migration will create.
-- ------------------------------------------------------------------
-- Every table/sequence a future migration creates is granted to the
-- runtime role immediately, so a newly migrated deployment never reaches
-- production with a table the application cannot read.
--
-- These are enumerated on purpose. "GRANT ALL PRIVILEGES" is never used:
-- the runtime role is not granted REFERENCES or TRUNCATE, and it is not
-- granted any DDL right of any kind.
--
-- KNOWN NARROWING, applied by grant-least-privilege-privileges.sql:
-- the grant above necessarily includes "Tenants", because ALTER DEFAULT
-- PRIVILEGES cannot name an exception. That script revokes
-- INSERT/UPDATE/DELETE from sms_app on it and keeps SELECT. Run it after
-- migrations on any database, fresh or existing - it is idempotent.
ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO sms_app;

ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO sms_app;

-- The migration history table is read by the startup migration gate on
-- every boot. It is not tenant-owned, so it needs a plain SELECT.
ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration IN SCHEMA public
    GRANT SELECT ON TABLES TO sms_migration;

-- ------------------------------------------------------------------
-- 5. Signal that role PASSWORDS still have to be assigned.
-- ------------------------------------------------------------------
-- The roles created above have LOGIN but no password, so they cannot
-- authenticate yet. init-db-least-privilege-passwords.sh reads
-- SMS_DB_APP_PASSWORD / SMS_DB_MIGRATION_PASSWORD from the container
-- environment and assigns them. Nothing is hard-coded here: a password in
-- this file would be a password in git.
DO $$
BEGIN
    RAISE NOTICE
        'sms_app / sms_migration created. Assign their passwords with '
        'SMS_DB_APP_PASSWORD / SMS_DB_MIGRATION_PASSWORD, then run '
        'docker/grant-least-privilege-privileges.sql after migrations.';
END
$$;