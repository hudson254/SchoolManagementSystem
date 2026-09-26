using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <summary>
    /// Establishes the PostgreSQL tenant Row Level Security *infrastructure* that
    /// <c>docker/init-db-rls.sql</c> describes, and creates the tenant policies for
    /// every table that actually carries a tenant column.
    ///
    /// <para><b>Why this migration exists.</b> The original
    /// <c>20260823120000_EnableRowLevelSecurity</c> was committed without its
    /// <c>[Migration]</c>/<c>[DbContext]</c> attributes and without a
    /// <c>.Designer.cs</c>, so Entity Framework never discovered it. It has therefore
    /// never been applied to any environment, and production still has no RLS at all.
    /// That file is not revived here: its timestamp sorts between two migrations that
    /// production has already applied, so making it discoverable would create a
    /// migration-history inconsistency, and its body is also incorrect (it references a
    /// non-existent <c>app</c> schema and eight tables that do not exist under the
    /// names it quotes).</para>
    ///
    /// <para><b>What this migration deliberately does NOT do: it does not run
    /// <c>ENABLE ROW LEVEL SECURITY</c>.</b> The application connects as
    /// <c>sms_user</c>, which is a superuser with BYPASSRLS and owns every table.
    /// PostgreSQL bypasses RLS unconditionally for such roles, and
    /// <c>FORCE ROW LEVEL SECURITY</c> does not override that. Enabling RLS today
    /// would therefore provide <i>no actual tenant isolation</i> while adding a large
    /// surface for breakage. The policies created here are inert until the application
    /// is moved onto a NOBYPASSRLS role.</para>
    ///
    /// <para>Creating the policies now is still valuable: it proves, at migration time
    /// and against the real schema, that every tenant-scoped table can be policed, and
    /// it makes the later "switch RLS on" step a single, separately reviewed
    /// operation. <see cref="Down"/> reverses it completely.</para>
    ///
    /// <para>The table and column names are discovered from
    /// <c>information_schema</c> rather than hard-coded, so this stays correct
    /// regardless of identifier casing (<c>units</c> vs <c>Units</c>,
    /// <c>tenant_id</c> vs <c>TenantId</c> on the Identity tables) and automatically
    /// covers tables created by later migrations.</para>
    /// </summary>
    public partial class AddTenantRowLevelSecurityPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateTenantRlsInfrastructure());
            migrationBuilder.Sql(CreateTenantPolicies());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DropTenantPolicies());
            migrationBuilder.Sql(
                "DROP FUNCTION IF EXISTS app.enable_tenant_rls(text);" +
                Environment.NewLine +
                "DROP FUNCTION IF EXISTS app.current_tenant_id();" +
                Environment.NewLine +
                "DROP SCHEMA IF EXISTS app;");
        }

        /// <summary>
        /// Creates the <c>app</c> schema and the two functions the tenant design is
        /// built on. Both are re-runnable and match the definitions in
        /// <c>docker/init-db-rls.sql</c>, so the deployment-time and migration-time
        /// paths cannot drift apart.
        /// </summary>
        private static string CreateTenantRlsInfrastructure() => @"
CREATE SCHEMA IF NOT EXISTS app;

-- Resolves the tenant for the current connection.
--
-- The application sets the app.tenant_id session variable through
-- TenantContextDbInterceptor, which runs on the first command issued on each
-- pooled connection. When the variable is unset (or unparsable) this returns an
-- all-zero sentinel that matches no row, so a connection without tenant context
-- can never read another tenant's data.
CREATE OR REPLACE FUNCTION app.current_tenant_id()
RETURNS uuid
LANGUAGE plpgsql
STABLE
AS $func$
DECLARE
    tid text;
BEGIN
    tid := current_setting('app.tenant_id', true);
    IF tid IS NULL OR tid = '' THEN
        RETURN '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;
    RETURN tid::uuid;
EXCEPTION WHEN OTHERS THEN
    RETURN '00000000-0000-0000-0000-000000000000'::uuid;
END;
$func$;

-- Helper used to switch RLS on for a single table once the application has been
-- moved onto a NOBYPASSRLS role. It is defined here for completeness of the
-- design but is deliberately NOT invoked by this migration.
CREATE OR REPLACE FUNCTION app.enable_tenant_rls(p_table_name text)
RETURNS void
LANGUAGE plpgsql
AS $func$
BEGIN
    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY;', p_table_name);
    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY;', p_table_name);
END;
$func$;
";

        /// <summary>
        /// Creates the four tenant policies (SELECT/INSERT/UPDATE/DELETE) on every
        /// base table in <c>public</c> that exposes a tenant column, using that table's
        /// real column name. Tables without a tenant column are skipped, as are
        /// non-table relations and anything outside <c>public</c>.
        ///
        /// <para>Row Level Security is intentionally left switched off: see the class
        /// remarks. The policies are therefore created but not enforced.</para>
        /// </summary>
        private static string CreateTenantPolicies() => @"
DO $do$
DECLARE
    target record;
    policied integer := 0;
BEGIN
    FOR target IN
        SELECT c.table_name, c.column_name
        FROM information_schema.columns c
        WHERE c.table_schema = 'public'
          AND c.column_name IN ('tenant_id', 'TenantId')
          AND c.table_name IN (
                SELECT t.table_name
                FROM information_schema.tables t
                WHERE t.table_schema = 'public'
                  AND t.table_type = 'BASE TABLE'
              )
        ORDER BY c.table_name
    LOOP
        -- The complete policy name is passed to %I as a single identifier. Building
        -- it by concatenating the prefix and the table name, then quoting once,
        -- avoids the tokenisation error that results from quoting the table name
        -- separately and appending it to a bare prefix.
        EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_select_' || target.table_name, target.table_name);
        EXECUTE format('CREATE POLICY %I ON %I FOR SELECT USING (%I = app.current_tenant_id())',
                       'tenant_select_' || target.table_name, target.table_name, target.column_name);

        EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_insert_' || target.table_name, target.table_name);
        EXECUTE format('CREATE POLICY %I ON %I FOR INSERT WITH CHECK (%I = app.current_tenant_id())',
                       'tenant_insert_' || target.table_name, target.table_name, target.column_name);

        EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_update_' || target.table_name, target.table_name);
        EXECUTE format('CREATE POLICY %I ON %I FOR UPDATE USING (%I = app.current_tenant_id()) WITH CHECK (%I = app.current_tenant_id())',
                       'tenant_update_' || target.table_name, target.table_name, target.column_name, target.column_name);

        EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_delete_' || target.table_name, target.table_name);
        EXECUTE format('CREATE POLICY %I ON %I FOR DELETE USING (%I = app.current_tenant_id())',
                       'tenant_delete_' || target.table_name, target.table_name, target.column_name);

        policied := policied + 1;
    END LOOP;

    RAISE NOTICE 'Created tenant RLS policies on % table(s). RLS is intentionally NOT enabled; the application role must be a NOBYPASSRLS role first.', policied;
END
$do$;
";

        /// <summary>
        /// Removes every tenant policy this migration may have created, by name
        /// pattern rather than from a stored list, so a table that no longer has a
        /// tenant column is still cleaned up.
        /// </summary>
        private static string DropTenantPolicies() => @"
DO $do$
DECLARE
    t record;
    kinds text[] := ARRAY['select', 'insert', 'update', 'delete'];
    k text;
BEGIN
    FOR t IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
        FOREACH k IN ARRAY kinds LOOP
            EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_' || k || '_' || t.tablename, t.tablename);
        END LOOP;
    END LOOP;
END
$do$;
";
    }
}

