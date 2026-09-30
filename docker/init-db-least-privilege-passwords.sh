#!/bin/sh
# ==================================================================
# School Management System - least-privilege role passwords (FRESH INSTALL)
# ==================================================================
# Runs once, from /docker-entrypoint-initdb.d, immediately AFTER
# init-db-least-privilege.sql has created the sms_app and sms_migration
# roles. The docker entrypoint executes /docker-entrypoint-initdb.d in
# lexical order, so the numeric filename prefixes in the compose mounts
# (01-..., 02-...) are what guarantee this ordering.
#
# Why this file exists
# -------------------
# init-db-least-privilege.sql deliberately sets NO passwords: a password in
# that file would be a password in git. The consequence is that on a brand
# new volume the two roles exist but cannot log in, so the API - which now
# connects as sms_app - would fail to start with an authentication error.
# This script closes that gap by reading the secrets from the container
# environment, which is where the deployment already keeps them
# (SMS_DB_APP_PASSWORD / SMS_DB_MIGRATION_PASSWORD, the same variables the
# API service interpolates into its connection strings).
#
# Idempotency / safety contract
# -----------------------------
#   * Only ever runs when the data volume is being initialised, i.e. exactly
#     once per volume. It is NOT part of a container restart: the postgres
#     entrypoint skips /docker-entrypoint-initdb.d entirely when PGDATA is
#     already populated.
#   * Never creates a role. init-db-least-privilege.sql owns that. If a role
#     is missing here the script fails loudly instead of papering over it.
#   * Never overwrites a password when the corresponding variable is unset,
#     so adding this file to an existing deployment cannot silently rotate
#     a live credential to empty.
#   * Re-asserts NOSUPERUSER / NOBYPASSRLS on every ALTER ROLE, so this file
#     can never be the reason a role became privileged.
# ==================================================================

# POSIX /bin/sh, NOT bash, and the file must stay LF-terminated.
#
# Both of those are hard requirements, and both were learned the hard way
# (a fresh-volume test run failed with `env: can't execute 'bash`):
#   * postgres:16-alpine ships no bash at all, so a `#!/usr/bin/env bash`
#     shebang cannot be executed. Everything below is POSIX sh so it runs
#     under busybox ash as well as dash.
#   * this file is bind-mounted straight out of a Windows working tree, so
#     CRLF endings turn the shebang into `bash\r` and break it the same way.
#     .gitattributes pins `*.sh text eol=lf` so a checkout cannot reintroduce
#     this.
#
# The entrypoint may either execute this file or source it, so the body is
# wrapped in a function and only `exit` on failure (which aborts
# initialisation in BOTH modes, and is a no-op on success).
_sms_assign_role_passwords() {
    # `pipefail` is deliberately omitted: it is not POSIX and busybox ash
    # does not support it.
    set -u

    app_password="${SMS_DB_APP_PASSWORD:-}"
    migration_password="${SMS_DB_MIGRATION_PASSWORD:-}"

    if [ -z "${app_password}" ] && [ -z "${migration_password}" ]; then
        echo "init-db-least-privilege-passwords: SMS_DB_APP_PASSWORD and" \
             "SMS_DB_MIGRATION_PASSWORD are both unset; the roles were created" \
             "without passwords. Run scripts/provision-least-privilege-roles.sh" \
             "before starting the API." >&2
        return 1
    fi

    # The passwords are passed to psql as VARIABLES (--set) and substituted by
    # psql itself, so neither the repository nor this script ever contains
    # them. psql renders :'name' as a correctly single-quoted and escaped SQL
    # literal, so a password containing a quote or a semicolon is stored
    # correctly instead of breaking the statement.
    #
    # Two psql behaviours drive the shape of this block, both established
    # empirically against postgres:16 and both of which break the obvious
    # implementation:
    #
    #  1. --command does NOT interpolate. psql documents that a -c string
    #     "must be completely parsable by the server (i.e. it contains no
    #     psql-specific features)", and ':' interpolation is such a feature.
    #     The SQL therefore goes in via --file.
    #
    #  2. --file DOES interpolate, but NOT inside a dollar-quoted block. A
    #     DO $do$ ... $do$ body is treated as a quoted region, so
    #     nullif(:'app_password', '') reaches the server verbatim and is
    #     rejected with `syntax error at or near ":"`. Every reference to a
    #     psql variable is therefore kept OUTSIDE dollar quotes, in plain
    #     top-level ALTER ROLE statements, and the dollar-quoted part is
    #     reduced to the role-existence check, which needs no variables.
    #
    # The heredoc terminator below MUST sit at column 0: an indented `SQL` is
    # not recognised as a terminator by POSIX sh (busybox ash), which then
    # reports `syntax error: unexpected end of file (expecting ")")` and aborts
    # database initialisation. The body may be indented freely - that is just
    # literal text. This matches scripts/provision-least-privilege-roles.sh.
    sql_file="$(mktemp)"

    # The existence check. No psql variables are referenced here, so the
    # dollar-quoting is safe, and a missing role fails loudly instead of
    # being papered over with a fresh, wrongly-attributed CREATE ROLE.
    if ! cat > "${sql_file}" <<'SQL'
        DO $do$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_app') THEN
                RAISE EXCEPTION
                    'Role sms_app does not exist; init-db-least-privilege.sql must run first.';
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sms_migration') THEN
                RAISE EXCEPTION
                    'Role sms_migration does not exist; init-db-least-privilege.sql must run first.';
            END IF;
        END
        $do$;
SQL
    then
        rm -f "${sql_file}"
        echo "init-db-least-privilege-passwords: FAILED to stage the SQL." >&2
        return 1
    fi

    # The ALTER ROLE statements are appended ONLY when the corresponding
    # variable is set. That is what makes "unset means leave it alone" real:
    # emitting `PASSWORD :'app_password'` with an empty variable would set an
    # EMPTY password, which lets anyone authenticate as the role. Deciding
    # here, in the shell, makes the empty case impossible to express in SQL.
    #
    # LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS are re-asserted on
    # every run: the attributes, not just the password, are the security
    # property being established, so this file can never be the reason a role
    # became privileged.
    if [ -n "${app_password}" ]; then
        echo "ALTER ROLE sms_app WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT NOBYPASSRLS PASSWORD :'app_password';" >> "${sql_file}"
    else
        echo "WARNING: SMS_DB_APP_PASSWORD not set; sms_app left unchanged." >&2
    fi

    if [ -n "${migration_password}" ]; then
        echo "ALTER ROLE sms_migration WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS PASSWORD :'migration_password';" >> "${sql_file}"
    else
        echo "WARNING: SMS_DB_MIGRATION_PASSWORD not set; sms_migration left unchanged." >&2
    fi


    # Connection options deliberately mirror the postgres image's own
    # docker_process_sql: during the init phase the temporary server is
    # started with `listen_addresses=''`, so it is reachable ONLY over the
    # unix socket. Forcing --host=localhost (or inheriting a PGHOST of
    # "localhost") makes psql try TCP and fail with
    # "connection to server at localhost (::1), port 5432 failed: Connection
    # refused", which would abort initialisation.
    #
    #   * host     - omitted entirely unless PGHOST is actually set, letting
    #                libpq use its default (the socket).
    #   * user     - PGUSER, else POSTGRES_USER. Falling back to "postgres"
    #                would be wrong on a cluster whose bootstrap role is named
    #                something else, such as the test cluster's "testuser".
    #   * --no-password / --no-psqlrc - local socket auth during init is
    #                `trust`; these stop psql from ever blocking on a prompt.
    if [ -n "${PGHOST:-}" ]; then
        host_opt="--host=${PGHOST}"
    else
        host_opt=""
    fi

    if ! psql \
        ${host_opt} \
        --username="${PGUSER:-${POSTGRES_USER:-postgres}}" \
        --dbname="${PGDATABASE:-${POSTGRES_DB:-postgres}}" \
        --no-password \
        --no-psqlrc \
        --set=ON_ERROR_STOP=1 \
        --set=app_password="${app_password}" \
        --set=migration_password="${migration_password}" \
        --quiet \
        --file="${sql_file}"; then
        rm -f "${sql_file}"
        echo "init-db-least-privilege-passwords: FAILED to assign role passwords." >&2
        return 1
    fi

    # The staging file holds the SQL template only - the passwords travel as
    # psql variables - but it is removed regardless so nothing is left behind.
    rm -f "${sql_file}"

    return 0
}

_sms_assign_role_passwords
_sms_rc=$?
unset -f _sms_assign_role_passwords

if [ "${_sms_rc}" -ne 0 ]; then
    echo "init-db-least-privilege-passwords: aborting database initialisation." >&2
    exit 1
fi

# Success path intentionally does not `exit`: when the entrypoint sources
# this file, exiting here would abort a perfectly good initialisation.
true