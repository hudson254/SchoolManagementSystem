#!/usr/bin/env bash
#
# OMS (Order Management System) environment setup + command helpers.
#
# Purpose
#   Supplies the non-secret environment used by docker/docker-compose.oms.yml
#   and by the test suites that run against it, and wraps the exact commands
#   needed to validate / start / test / stop that environment.
#
# Usage (documented, copy-paste ready)
#   source ./oms_env.sh                 # load defaults + helper functions
#   oms_check_env                       # verify tooling + variables
#   oms_show_env                        # print the effective (non-secret) config
#   oms_validate                        # docker compose config --quiet
#   oms_up                              # docker compose up -d --build
#   oms_test                            # dotnet test tests/SMS.ApiTests
#   oms_down                            # docker compose down
#
# Safe to source
#   Strict mode (set -euo pipefail) is only enabled when this file is EXECUTED,
#   never when it is sourced, so `source ./oms_env.sh` cannot change the
#   caller's shell options or exit the caller's shell on a failure.
#
# Secrets
#   NO production secret is defined here. Every value below is a non-production
#   test default and every one can be overridden by exporting it first.
#   Production secrets are never read from this file. They must be supplied by
#   the deployment environment (.env / docker/.env on the production host, see
#   Documentation/ProductionReadiness/OMS_INTEGRATION.md) and are listed by
#   name only:
#     DB_PASSWORD, JWT_SECRET, ADMIN_EMAIL, ADMIN_PASSWORD, FRONTEND_URL,
#     GRAFANA_PASSWORD
#
# Shell: bash (uses BASH_SOURCE). No PowerShell syntax is used anywhere.

# ---------------------------------------------------------------------------
# Repository root (resolved from this file so it works from any directory)
# ---------------------------------------------------------------------------
OMS_ENV_FILE="${BASH_SOURCE[0]:-$0}"
OMS_REPO_ROOT="$(cd "$(dirname "${OMS_ENV_FILE}")" && pwd)"
export OMS_REPO_ROOT

# Canonical OMS compose file. NOTE: every compose file in this repository lives
# in docker/ (see docker/docker-compose*.yml); there is no copy at the root.
export OMS_COMPOSE_FILE="${OMS_COMPOSE_FILE:-docker/docker-compose.oms.yml}"
export OMS_API_URL="${OMS_API_URL:-http://localhost:5080}"

# ---------------------------------------------------------------------------
# Test-only defaults (non-secret). Override by exporting before sourcing.
# ---------------------------------------------------------------------------
export OMS_DB_NAME="${OMS_DB_NAME:-oms_test}"
export OMS_DB_USER="${OMS_DB_USER:-oms_test_user}"
export OMS_DB_PASSWORD="${OMS_DB_PASSWORD:-testpass123}"
export OMS_POSTGRES_PORT="${OMS_POSTGRES_PORT:-5434}"
export OMS_API_PORT="${OMS_API_PORT:-5080}"

# Test JWT secret (>= 64 chars for HMAC-SHA256). Test value only.
export JWT_SECRET="${JWT_SECRET:-test-jwt-secret-min-64-characters-long-for-hmac-sha256-testing}"
export FRONTEND_URL="${FRONTEND_URL:-http://localhost:3000}"

# OMS storage contract (kept aligned with docker/docker-compose.oms.yml).
export ROADS_DB_FILE="${ROADS_DB_FILE:-/app/data/roads.db}"
export WAL_RECOVERY_TARGET="${WAL_RECOVERY_TARGET:-latest}"
export WAL_RECOVERY_PATH="${WAL_RECOVERY_PATH:-/app/data/wal-recovery}"
export ORDER_MANIFEST_STORAGE_PATH="${ORDER_MANIFEST_STORAGE_PATH:-/app/data/order-manifests}"
export ORDER_MANIFEST_FILE_PREFIX="${ORDER_MANIFEST_FILE_PREFIX:-order-manifest-}"
export ORDER_MANIFEST_RETENTION_DAYS="${ORDER_MANIFEST_RETENTION_DAYS:-7}"

# ---------------------------------------------------------------------------
# Helper functions
# ---------------------------------------------------------------------------

# Verify the tooling and files the OMS environment depends on.
# Returns 1 (fail-fast) when something required is missing.
oms_check_env() {
    oms_failures=0

    for oms_tool in docker dotnet; do
        if ! command -v "$oms_tool" >/dev/null 2>&1; then
            echo "ERROR: required tool not found on PATH: $oms_tool" >&2
            oms_failures=$((oms_failures + 1))
        fi
    done

    if ! docker compose version >/dev/null 2>&1; then
        echo "ERROR: 'docker compose' (v2) is not available; is the daemon running?" >&2
        oms_failures=$((oms_failures + 1))
    fi

    if [ ! -f "${OMS_REPO_ROOT}/${OMS_COMPOSE_FILE}" ]; then
        echo "ERROR: compose file not found: ${OMS_REPO_ROOT}/${OMS_COMPOSE_FILE}" >&2
        oms_failures=$((oms_failures + 1))
    fi

    if [ "$oms_failures" -gt 0 ]; then
        echo "oms_check_env: FAILED ($oms_failures problem(s))" >&2
        return 1
    fi

    echo "oms_check_env: OK"
    return 0
}

# Print the effective non-secret configuration. Secret values are never echoed.
oms_show_env() {
    echo "=== OMS Environment Configuration ==="
    echo "OMS_REPO_ROOT               : ${OMS_REPO_ROOT}"
    echo "OMS_COMPOSE_FILE            : ${OMS_COMPOSE_FILE}"
    echo "OMS_API_URL                 : ${OMS_API_URL}"
    echo "OMS_DB_NAME                 : ${OMS_DB_NAME}"
    echo "OMS_DB_USER                 : ${OMS_DB_USER}"
    echo "OMS_DB_PASSWORD             : ${OMS_DB_PASSWORD:+**** (set)}"
    echo "OMS_POSTGRES_PORT           : ${OMS_POSTGRES_PORT}"
    echo "OMS_API_PORT                : ${OMS_API_PORT}"
    echo "JWT_SECRET                  : ${JWT_SECRET:+**** (set)}"
    echo "FRONTEND_URL                : ${FRONTEND_URL}"
    echo "ROADS_DB_FILE               : ${ROADS_DB_FILE}"
    echo "WAL_RECOVERY_TARGET         : ${WAL_RECOVERY_TARGET}"
    echo "WAL_RECOVERY_PATH           : ${WAL_RECOVERY_PATH}"
    echo "ORDER_MANIFEST_STORAGE_PATH : ${ORDER_MANIFEST_STORAGE_PATH}"
    echo "ORDER_MANIFEST_RETENTION_DAYS: ${ORDER_MANIFEST_RETENTION_DAYS}"
    echo "====================================="
}

oms_validate() {
    echo "> docker compose -f ${OMS_COMPOSE_FILE} config --quiet"
    docker compose -f "${OMS_COMPOSE_FILE}" config --quiet
}

oms_up() {
    echo "> docker compose -f ${OMS_COMPOSE_FILE} up -d --build"
    docker compose -f "${OMS_COMPOSE_FILE}" up -d --build
}

oms_ps() {
    docker compose -f "${OMS_COMPOSE_FILE}" ps
}

oms_logs() {
    docker compose -f "${OMS_COMPOSE_FILE}" logs --tail 200 "$@"
}

oms_health() {
    curl --silent --show-error --fail "${OMS_API_URL}/health" || return
    echo
}

# Existing SMS tests use WebApplicationFactory, NOT the running API container.
# Scope the connection override to a subshell; never inherit a production DSN.
oms_test() (
    set -euo pipefail
    cd "${OMS_REPO_ROOT}"
    docker compose -f "${OMS_COMPOSE_FILE}" exec -T postgres \
        sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' >/dev/null
    export ASPNETCORE_ENVIRONMENT=Testing
    export ConnectionStrings__DefaultConnection="Host=localhost;Port=${OMS_POSTGRES_PORT};Database=${OMS_DB_NAME};Username=${OMS_DB_USER};Password=${OMS_DB_PASSWORD}"
    dotnet test tests/SMS.ApiTests/SMS.ApiTests.csproj --configuration Release
)

# Non-destructive teardown. Keeps the named test volumes so test state survives
# a down/up cycle. NEVER add -v here: that would delete data volumes.
oms_down() {
    echo "> docker compose -f ${OMS_COMPOSE_FILE} down"
    docker compose -f "${OMS_COMPOSE_FILE}" down
}

# ---------------------------------------------------------------------------
# When EXECUTED (not sourced), enable strict mode and print help.
# ---------------------------------------------------------------------------
if [ "${BASH_SOURCE[0]:-$0}" = "${0}" ]; then
    set -euo pipefail
    echo "OMS Environment Script (this file is meant to be SOURCED)"
    echo
    echo "  source ./oms_env.sh     # load defaults + helpers into the current shell"
    echo "  oms_check_env           # verify tooling and variables"
    echo "  oms_show_env            # show effective non-secret configuration"
    echo "  oms_validate            # validate the OMS compose file"
    echo "  oms_up | oms_ps | oms_logs | oms_health | oms_test | oms_down"
    echo
    oms_show_env
fi
