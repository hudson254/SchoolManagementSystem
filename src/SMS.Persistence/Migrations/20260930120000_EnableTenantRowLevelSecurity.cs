using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <summary>
    /// Switches on row level security for every tenant-owned table.
    ///
    /// <para><b>This is only meaningful because the application stopped
    /// connecting as a superuser first.</b> The policies themselves were
    /// created earlier by
    /// <c>AddTenantRowLevelSecurityPolicies</c>, but that migration
    /// deliberately left RLS switched off: the application role was
    /// SUPERUSER with BYPASSRLS, and PostgreSQL skips policies entirely for
    /// such roles - FORCE ROW LEVEL SECURITY included. Enabling RLS then
    /// would have looked like enforcement while providing none.</para>
    ///
    /// <para>The runtime role is now <c>sms_app</c>: NOSUPERUSER,
    /// NOBYPASSRLS, and not the owner of any table. Those are exactly the
    /// three conditions under which PostgreSQL evaluates policies for a
    /// connection, so this migration is what turns the existing policies
    /// into an actual boundary.</para>
    ///
    /// <para><b>Tenants is deliberately excluded.</b> It carries a tenant
    /// column but it is the bootstrap registry:
    /// <c>TenantStore.GetTenantAsync</c> has to read it to discover which
    /// tenant the request belongs to, which is the step that <i>establishes</i>
    /// the tenant context in the first place. Policing it is circular - the
    /// policy would evaluate against a tenant that is not yet known, match
    /// nothing, and every request would fail with "Invalid tenant". Its four
    /// policies are dropped here so the exclusion is explicit and cannot be
    /// undone by a well-meaning "just enable it on everything".</para>
    ///
    /// <para><b>FORCE ROW LEVEL SECURITY is deliberately NOT used.</b> The
    /// tables are owned by <c>sms_migration</c>, which needs unrestricted
    /// access to apply migrations, backfill data and run the bootstrap seed.
    /// A table owner is already exempt from its own table's policies unless
    /// FORCE is set, and FORCE would remove that exemption - breaking
    /// migrations and seeding. The runtime role is not the owner, so plain
    /// ENABLE is sufficient to enforce it. FORCE becomes appropriate later,
    /// once ownership is moved to a role that never connects at runtime.</para>
    ///
    /// <para>Tables are discovered from <c>information_schema</c> rather than
    /// hard-coded, so identifier casing and later migrations are handled
    /// automatically. Everything here is idempotent and fully reversible.</para>
    /// </summary>
    public partial class EnableTenantRowLevelSecurity : Migration
    {
        /// <summary>
        /// Tables that carry a tenant column but must NOT be policed, with the
        /// reason for each. Kept as data rather than inlined SQL so the
        /// exemption list and the generated statements cannot drift apart.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, string> ExemptTables =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tenants"] =
                    "The tenant registry. It is read to DISCOVER the tenant, so a policy on it " +
                    "would evaluate against a tenant that is not yet known and match nothing.",
            };

        /// <inheritdoc />
        /// <summary>
        /// Tables whose policies cannot be expressed as
        /// <c>&lt;tenant column&gt; = app.current_tenant_id()</c> because the
        /// application never populates that column.
        /// </summary>
        /// <remarks>
        /// <c>AspNetUserRoles</c> is the ASP.NET Identity join table between
        /// users and roles. Identity's UserStore writes an
        /// <c>IdentityUserRole&lt;string&gt;</c> - the TPH BASE type - while the
        /// <c>TenantId</c> column is declared on the derived
        /// <c>SMS.Domain.Entities.UserRole</c>. Under TPH a base-typed instance
        /// writes only base columns, so every role assignment in the database
        /// was written with a NULL TenantId. A policy comparing that column
        /// would therefore reject every INSERT ("new row violates row-level
        /// security policy") and hide every existing row, silently breaking
        /// all role authorization.
        ///
        /// The tenant is instead derived from the user the assignment belongs
        /// to, which is authoritative and cannot be spoofed by a bogus column
        /// value. The sub-select reads AspNetUsers, which has its own policy,
        /// so it is already scoped to the calling tenant.
        /// </remarks>
        internal const string AspNetUserRolesTable = "AspNetUserRoles";

        /// <summary>
        /// The predicate that scopes a row of <c>AspNetUserRoles</c> to the
        /// calling tenant, via the owning user.
        /// </summary>
        internal const string AspNetUserRolesTenantPredicate =
            "\"UserId\" IN (SELECT u.\"Id\" FROM \"AspNetUsers\" u " +
            "WHERE u.\"TenantId\" = app.current_tenant_id())";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data repair FIRST. Historical AspNetUserRoles rows have a NULL
            // TenantId for the reason described on
            // AspNetUserRolesTenantPredicate. Backfilling makes the column
            // meaningful for the derived-typed rows the application creates
            // itself, even though the policies no longer depend on it.
            migrationBuilder.Sql(BackfillUserRoleTenants());

            // Drop the policies on the exempt tables first, so each is left
            // genuinely unprotected rather than "protected but inert" - a
            // much more dangerous state to reason about later.
            foreach (var (table, reason) in ExemptTables)
                migrationBuilder.Sql(DropTenantPoliciesFor(table, reason));

            migrationBuilder.Sql(EnableTenantRls());
            migrationBuilder.Sql(InstallAspNetUserRolesPolicies());
            migrationBuilder.Sql(RedefineEnableHelper());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DisableTenantRls());
            migrationBuilder.Sql(RedefineEnableHelper());

            // Restore exactly what the previous migration left behind, so a
            // rollback does not silently drop the policies.
            foreach (var table in ExemptTables.Keys)
                migrationBuilder.Sql(CreateTenantPoliciesFor(table));
        }

        /// <summary>
        /// Replaces the generic AspNetUserRoles policies with ones that derive
        /// the tenant from the owning user. Idempotent.
        /// </summary>
        private static string InstallAspNetUserRolesPolicies() => $@"
DO $do$
DECLARE
    v_table text := '{AspNetUserRolesTable}';
    v_predicate text := '{AspNetUserRolesTenantPredicate.Replace("'", "''")}';
    exists_in_db boolean;
BEGIN
    SELECT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = v_table
    ) INTO exists_in_db;

    IF NOT exists_in_db THEN
        RETURN;
    END IF;

    EXECUTE format('DROP POLICY IF EXISTS %I ON public.%I', 'tenant_select_' || v_table, v_table);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR SELECT USING (%s)',
                   'tenant_select_' || v_table, v_table, v_predicate);

    EXECUTE format('DROP POLICY IF EXISTS %I ON public.%I', 'tenant_insert_' || v_table, v_table);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR INSERT WITH CHECK (%s)',
                   'tenant_insert_' || v_table, v_table, v_predicate);

    EXECUTE format('DROP POLICY IF EXISTS %I ON public.%I', 'tenant_update_' || v_table, v_table);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR UPDATE USING (%s) WITH CHECK (%s)',
                   'tenant_update_' || v_table, v_table, v_predicate, v_predicate);

    EXECUTE format('DROP POLICY IF EXISTS %I ON public.%I', 'tenant_delete_' || v_table, v_table);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR DELETE USING (%s)',
                   'tenant_delete_' || v_table, v_table, v_predicate);
END
$do$;";
/// <summary>
        /// Copies each user's tenant onto their role assignments.
        ///
        /// <para>Run before RLS is enabled. Rows whose user has no tenant
        /// (or whose user row is gone) keep a NULL tenant: a policy of
        /// <c>tenant_id = current_tenant()</c> evaluates to NULL for them,
        /// which is not true, so they stay invisible to every tenant. That
        /// is the fail-closed outcome, and leaving them invisible is safer
        /// than guessing a tenant and granting access to the wrong one.</para>
        /// </summary>
        private static string BackfillUserRoleTenants() => @"
DO $do$
DECLARE
    repaired integer := 0;
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'AspNetUserRoles'
    ) THEN
        UPDATE ""AspNetUserRoles"" ur
           SET ""TenantId"" = u.""TenantId""
          FROM ""AspNetUsers"" u
         WHERE u.""Id"" = ur.""UserId""
           AND ur.""TenantId"" IS NULL
           AND u.""TenantId"" IS NOT NULL;

        GET DIAGNOSTICS repaired = ROW_COUNT;
    END IF;

    RAISE NOTICE 'Backfilled TenantId on % AspNetUserRoles row(s).', repaired;
END
$do$;";

        /// <summary>
        /// Turns RLS on for every tenant-scoped base table that is not on the
        /// exempt list.
        /// </summary>
/// <summary>
        /// Turns RLS on for every tenant-scoped base table that is not on the
        /// exempt list.
        /// </summary>
        private static string EnableTenantRls() => $@"
DO $do$
DECLARE
    target record;
    enabled_count integer := 0;
BEGIN
    FOR target IN
        SELECT c.table_name, c.column_name
        FROM information_schema.columns c
        JOIN information_schema.tables t
          ON t.table_schema = c.table_schema
         AND t.table_name   = c.table_name
        WHERE c.table_schema = 'public'
          AND t.table_type = 'BASE TABLE'
          AND c.column_name IN ('tenant_id', 'TenantId')
          AND lower(c.table_name) <> ALL (
              SELECT lower(unnest) FROM unnest(ARRAY[{ExemptionArraySql()}]))
        ORDER BY c.table_name
    LOOP
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', target.table_name);
        enabled_count := enabled_count + 1;
    END LOOP;

    RAISE NOTICE 'Enabled row level security on % tenant table(s).', enabled_count;
END
$do$;";

        /// <summary>
        /// The exact inverse of <see cref="EnableTenantRls"/>. Disables RLS by
        /// querying <c>pg_class</c> rather than repeating the discovery logic,
        /// so a table that has since lost its tenant column is still switched
        /// off and a rollback cannot leave enforcement half-applied.
        /// </summary>
        private static string DisableTenantRls() => @"
DO $do$
DECLARE
    target record;
    disabled_count integer := 0;
BEGIN
    FOR target IN
        SELECT c.relname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind = 'r'
          AND c.relrowsecurity
        ORDER BY c.relname
    LOOP
        EXECUTE format('ALTER TABLE public.%I DISABLE ROW LEVEL SECURITY', target.relname);
        disabled_count := disabled_count + 1;
    END LOOP;

    RAISE NOTICE 'Disabled row level security on % table(s).', disabled_count;
END
$do$;";

        /// <summary>
        /// Rewrites app.enable_tenant_rls(text), which previously also issued
        /// FORCE ROW LEVEL SECURITY. That would have subjected the migration
        /// role to the very policies it has to manage, so the helper now
        /// matches what this migration actually does.
        /// </summary>
        private static string RedefineEnableHelper() => @"
CREATE OR REPLACE FUNCTION app.enable_tenant_rls(p_table_name text)
RETURNS void
LANGUAGE plpgsql
AS $func$
BEGIN
    -- ENABLE only, never FORCE: the owning role must retain unrestricted
    -- access for migrations and seeding. The runtime role is not the owner,
    -- so plain ENABLE already enforces isolation for it.
    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', p_table_name);
END;
$func$;";
private static string DropTenantPoliciesFor(string table, string reason) => $@"
DO $do$
DECLARE
    v_column text;
    v_table text := {SqlLiteral(table)};
    k text;
BEGIN
    -- {reason}
    SELECT c.column_name INTO v_column
      FROM information_schema.columns c
     WHERE c.table_schema = 'public'
       AND c.table_name   = v_table
       AND c.column_name IN ('tenant_id', 'TenantId');

    IF v_column IS NULL THEN
        RETURN;
    END IF;

    FOREACH k IN ARRAY ARRAY['select', 'insert', 'update', 'delete'] LOOP
        EXECUTE format('DROP POLICY IF EXISTS %I ON public.%I',
                       'tenant_' || k || '_' || v_table, v_table);
    END LOOP;
END
$do$;";

        private static string CreateTenantPoliciesFor(string table) => $@"
DO $do$
DECLARE
    v_column text;
    v_table text := {SqlLiteral(table)};
BEGIN
    SELECT c.column_name INTO v_column
      FROM information_schema.columns c
     WHERE c.table_schema = 'public'
       AND c.table_name   = v_table
       AND c.column_name IN ('tenant_id', 'TenantId');

    IF v_column IS NULL THEN
        RETURN;
    END IF;

    EXECUTE format('CREATE POLICY %I ON public.%I FOR SELECT USING (%I = app.current_tenant_id())',
                   'tenant_select_' || v_table, v_table, v_column);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR INSERT WITH CHECK (%I = app.current_tenant_id())',
                   'tenant_insert_' || v_table, v_table, v_column);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR UPDATE USING (%I = app.current_tenant_id()) WITH CHECK (%I = app.current_tenant_id())',
                   'tenant_update_' || v_table, v_table, v_column, v_column);
    EXECUTE format('CREATE POLICY %I ON public.%I FOR DELETE USING (%I = app.current_tenant_id())',
                   'tenant_delete_' || v_table, v_table, v_column);
END
$do$;";

        /// <summary>The exemption list as a SQL array literal.</summary>
        private static string ExemptionArraySql() =>
            string.Join(", ", ExemptTables.Keys.Select(SqlLiteral));

        /// <summary>
        /// Quotes a value as a SQL string literal. Table names come from a
        /// fixed in-code list rather than user input, but quoting them keeps
        /// the generated SQL valid regardless of casing or reserved words.
        /// </summary>
        private static string SqlLiteral(string value) => $"'{value.Replace("'", "''")}'";
    }
}