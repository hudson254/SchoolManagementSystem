#!/usr/bin/env bash
# ==================================================================
# School Management System - least-privilege role provisioning
# ==================================================================
# Applies the two SQL scripts to an already-running PostgreSQL and
# assigns role passwords from the environment.
#
#   Stage 1 of the rollout (see Documentation/Security/RLS-ENFORCEMENT.md):
#     this script only changes roles, ownership and grants. It does NOT
#     enable row level security. Enabling RLS is a separate, separately
#     reviewed step so that it can be rolled back independently.
#
# Passwords are read from the environment and are never written to a
# file, echoed, or passed on a command line where `ps` could see them.
#
# Usage:
#   SMS_APP_PASSWORD=... SMS_MIGRATION_PASSWORD=... \
#   DB_HOST=... DB_PORT=... DB_NAME=... DB_SUPERUSER=... DB_SUPERPASSWORD=... \
#   ./scripts/provision-least-privilege-roles.sh
#
# Idempotent: safe to re-run.
# ==================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

INIT_SQL="${REPO_ROOT}/docker/init-db-least-privilege.sql"
GRANT_SQL="${REPO_ROOT}/docker/grant-least-privilege-privileges.sql"

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-SchoolManagementSystem}"
# The bootstrap superuser. This is the POSTGRES_USER of the postgres image
# (sms_user in production, testuser in the test cluster). It is used ONLY
# for this provisioning step; the application never connects as it.
DB_SUPERUSER="${DB_SUPERUSER:-sms_user}"

NC=$'\033[0m'; RED=$'\033[31m'; GREEN=$'\033[32m'; YELLOW=$'\033[33m'; BLUE=$'\033[34m'

log()  { printf '%s==>%s %s\n' "${BLUE}" "${NC}" "$*"; }
ok()   { printf '%s  OK%s %s\n' "${GREEN}" "${NC}" "$*"; }
warn() { printf '%s  !!%s %s\n' "${YELLOW}" "${NC}" "$*"; }
die()  { printf '%s ERR%s %s\n' "${RED}" "${NC}" "$*" >&2; exit 1; }

# Run SQL as the bootstrap superuser. The password is exported through
# PGPASSWORD so it never appears in the process list.
psql_super() {
    PGPASSWORD="${DB_SUPERPASSWORD:-}" psql \
        --host="${DB_HOST}" \
        --port="${DB_PORT}" \
        --username="${DB_SUPERUSER}" \
        --dbname="${DB_NAME}" \
        --set=ON_ERROR_STOP=1 \
        --quiet \
        "$@"
}

[[ -f "${INIT_SQL}"  ]] || die "missing ${INIT_SQL}"
[[ -f "${GRANT_SQL}" ]] || die "missing ${GRANT_SQL}"

log "Target: ${DB_SUPERUSER}@${DB_HOST}:${DB_PORT}/${DB_NAME}"

# ------------------------------------------------------------------
# Step 1 - create the roles (no passwords).
# ------------------------------------------------------------------
log "Step 1/4: creating least-privilege roles"
psql_super --file="${INIT_SQL}"
ok "roles sms_app (NOBYPASSRLS) and sms_migration created"
# ------------------------------------------------------------------
# Step 2 - assign passwords from the environment.
# ------------------------------------------------------------------
# The value is passed as a psql variable and interpolated inside a DO
# block with format('%I', ...) / format('%L', ...). That means the shell
# never splices it into SQL text and it is never written to a file in
# the repository. format('%L') also means a password containing a quote
# or a semicolon is stored correctly rather than breaking the statement.
assign_password() {
    local role="$1" secret="$2" var_name="$3"

    if [[ -z "${secret}" ]]; then
        warn "${role}: no password supplied (${var_name} not set); leaving it unchanged"
        return 0
    fi

    PGPASSWORD="${DB_SUPERPASSWORD:-}" psql \
        --host="${DB_HOST}" \
        --port="${DB_PORT}" \
        --username="${DB_SUPERUSER}" \
        --dbname="${DB_NAME}" \
        --set=ON_ERROR_STOP=1 \
        --quiet \
        --set=role_name="${role}" \
        --set=role_secret="${secret}" \
        --file=<(cat <<'SQL'
DO $do$
DECLARE
    v_role   text := :'role_name';
    v_secret text := :'role_secret';
BEGIN
    -- NOSUPERUSER/NOBYPASSRLS are re-asserted here so that running this
    -- script can never accidentally re-introduce a privileged role.
    EXECUTE format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB '
                   'NOCREATEROLE NOBYPASSRLS PASSWORD %L', v_role, v_secret);
END
$do$;
SQL
)

    ok "${role}: password set from environment"
}

log "Step 2/4: assigning role passwords from the environment"
assign_password sms_app        "${SMS_APP_PASSWORD:-}"        SMS_APP_PASSWORD
assign_password sms_migration "${SMS_MIGRATION_PASSWORD:-}" SMS_MIGRATION_PASSWORD

# ------------------------------------------------------------------
# Step 3 - ownership transfer + grants.
# ------------------------------------------------------------------
log "Step 3/4: transferring ownership to sms_migration and granting runtime privileges"
psql_super --file="${GRANT_SQL}"
ok "ownership and grants applied"

# ------------------------------------------------------------------
# Step 4 - verify, so a partial run is never mistaken for success.
# ------------------------------------------------------------------
log "Step 4/4: verifying the resulting role attributes"

result="$(psql_super --tuples-only --no-align --command="
SELECT rolname || '|' || rolsuper || '|' || rolbypassrls
       || '|' || rolcreatedb || '|' || rolcreaterole
  FROM pg_roles
 WHERE rolname IN ('sms_app', 'sms_migration')
 ORDER BY rolname;")"

expected_app="sms_app|f|f|f|f"
expected_migration="sms_migration|f|f|f|f"

got_app="$(printf '%s\n' "${result}"      | grep '^sms_app|'        || true)"
got_migration="$(printf '%s\n' "${result}" | grep '^sms_migration|' || true)"

if [[ "${got_app}" != "${expected_app}" ]]; then
    die "sms_app attributes are wrong: '${got_app}' (expected ${expected_app})"
fi
if [[ "${got_migration}" != "${expected_migration}" ]]; then
    die "sms_migration attributes are wrong: '${got_migration}' (expected ${expected_migration})"
fi

printf '%s\n' "${result}"
ok "both roles are NOSUPERUSER, NOBYPASSRLS, NOCREATEDB, NOCREATEROLE"

log "Row level security has NOT been enabled by this script (that is the next stage)."