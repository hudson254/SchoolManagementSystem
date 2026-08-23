-- School Management System - PostgreSQL RLS (Row Level Security) Setup
-- ==================================================================
-- This script sets up complete tenant isolation via PostgreSQL RLS.
-- It MUST be run after migrations create the tables.
-- It can be safely re-run (idempotent).
-- ==================================================================

-- 1. Create the tenant context function
-- This function reads the current tenant ID from a custom PostgreSQL parameter.
-- The parameter is set per-session by the application before any query.
-- Using a custom parameter (app.tenant_id) is preferred over app.current_setting
-- because custom parameters are session-scoped, NOT transaction-scoped,
-- meaning they survive across transactions within the same connection
-- but are reset when the connection returns to the pool.
CREATE OR REPLACE FUNCTION app.current_tenant_id()
RETURNS uuid
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    tenant_id text;
BEGIN
    tenant_id := current_setting('app.tenant_id', true);
    IF tenant_id IS NULL OR tenant_id = '' THEN
        -- No tenant context set - return a sentinel value that matches no records
        RETURN '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;
    RETURN tenant_id::uuid;
EXCEPTION
    WHEN OTHERS THEN
        RETURN '00000000-0000-0000-0000-000000000000'::uuid;
END;
$$;

-- 2. Create application roles for RLS
DO $$
BEGIN
    -- sms_app_role: The role used by the application for normal operations
    -- This role does NOT have BYPASSRLS
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_app_role') THEN
        CREATE ROLE sms_app_role WITH LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    END IF;

    -- sms_migration_role: The role used for migrations and administrative tasks
    -- This role HAS BYPASSRLS so migrations can create tables and apply RLS policies
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_migration_role') THEN
        CREATE ROLE sms_migration_role WITH LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE BYPASSRLS;
    END IF;

    -- sms_readonly_role: For read-only reporting and analytics
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_readonly_role') THEN
        CREATE ROLE sms_readonly_role WITH LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    END IF;
END
$$;

-- 3. Revoke all privileges on all tables from public
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM PUBLIC;

-- 4. Grant appropriate privileges to application roles
GRANT USAGE ON SCHEMA public TO sms_app_role;
GRANT USAGE ON SCHEMA public TO sms_migration_role;
GRANT USAGE ON SCHEMA public TO sms_readonly_role;

-- 5. Define the RLS policy template function
-- This function generates standard RLS policies for tenant-scoped tables.
-- It is used by the migration to ensure consistent policies.

-- Helper function to enable RLS on a table and create standard policies
CREATE OR REPLACE FUNCTION app.enable_tenant_rls(table_name text)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    -- Enable RLS on the table
    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY;', table_name);
    
    -- Force RLS (even for table owners)
    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY;', table_name);
    
    -- SELECT policy: Only rows belonging to the current tenant
    EXECUTE format(
        'DROP POLICY IF EXISTS tenant_select_%I ON %I;',
        table_name, table_name
    );
    EXECUTE format(
        'CREATE POLICY tenant_select_%I ON %I FOR SELECT USING (tenant_id = app.current_tenant_id());',
        table_name, table_name
    );
    
    -- INSERT policy: Only insert rows with the current tenant's tenant_id
    EXECUTE format(
        'DROP POLICY IF EXISTS tenant_insert_%I ON %I;',
        table_name, table_name
    );
    EXECUTE format(
        'CREATE POLICY tenant_insert_%I ON %I FOR INSERT WITH CHECK (tenant_id = app.current_tenant_id());',
        table_name, table_name
    );
    
    -- UPDATE policy: Only update rows belonging to the current tenant, and prevent tenant_id changes
    EXECUTE format(
        'DROP POLICY IF EXISTS tenant_update_%I ON %I;',
        table_name, table_name
    );
    EXECUTE format(
        'CREATE POLICY tenant_update_%I ON %I FOR UPDATE USING (tenant_id = app.current_tenant_id()) WITH CHECK (tenant_id = app.current_tenant_id());',
        table_name, table_name
    );
    
    -- DELETE policy: Only delete rows belonging to the current tenant
    EXECUTE format(
        'DROP POLICY IF EXISTS tenant_delete_%I ON %I;',
        table_name, table_name
    );
    EXECUTE format(
        'CREATE POLICY tenant_delete_%I ON %I FOR DELETE USING (tenant_id = app.current_tenant_id());',
        table_name, table_name
    );
END;
$$;

-- 6. Grant EXECUTE on the function to the application role
GRANT EXECUTE ON FUNCTION app.current_tenant_id() TO sms_app_role;
GRANT EXECUTE ON FUNCTION app.current_tenant_id() TO sms_migration_role;
GRANT EXECUTE ON FUNCTION app.current_tenant_id() TO sms_readonly_role;
GRANT EXECUTE ON FUNCTION app.enable_tenant_rls(text) TO sms_migration_role;

-- 7. Note: The actual RLS application to each table is done via migration
-- The app.enable_tenant_rls() function is called per-table in the migration code.
-- This script only sets up the infrastructure.

-- 8. Set the default privileges so future tables created by sms_migration_role
-- are accessible by sms_app_role
ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration_role IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO sms_app_role;
ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration_role IN SCHEMA public
    GRANT SELECT ON TABLES TO sms_readonly_role;
ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration_role IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO sms_app_role;
ALTER DEFAULT PRIVILEGES FOR ROLE sms_migration_role IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO sms_readonly_role;