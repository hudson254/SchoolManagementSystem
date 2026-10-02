#!/bin/bash
# =============================================================================
# School Management System - nginx configuration verification
# =============================================================================
# WHY THIS SCRIPT EXISTS
# The nginx configuration used to be bind-mounted as a SINGLE FILE:
#     ./nginx.conf:/etc/nginx/nginx.conf:ro
# A single-file bind mount is resolved to ONE inode when the container is
# created. `git checkout` REPLACES the file (new inode, new device) rather than
# rewriting it in place, so the running container kept serving the PREVIOUS
# configuration while the repository already showed the new one. That produced a
# real stale-configuration condition during a production deployment: a
# service-worker cache-header fix was committed but the live server still
# served the old file, and only an explicit container recreate resolved it.
#
# The compose files now mount the ./nginx DIRECTORY and start nginx with
# `-c /etc/nginx/sms/nginx.conf`. A directory mount re-resolves the file on
# every read, so `nginx -t && nginx -s reload` is always sufficient and no
# recreate is needed. This script makes that guarantee CHECKABLE rather than
# assumed, and is the documented verification step that must run after every
# nginx configuration change.
#
# CHECKS PERFORMED (in order, fail-fast)
#   1. host file checksum     - what the repository says should be deployed
#   2. in-container checksum  - what the container actually has. A mismatch is
#                               the inode trap, detected BEFORE anything reloads.
#   3. nginx -t               - validity, before touching the live process
#   4. nginx -s reload        - apply without downtime
#   5. process still healthy  - the master survived the reload
#   6. external behaviour     - /hub must not redirect; sw.js must not be
#                               pinned immutable
#
# Usage:
#   ./scripts/verify-nginx-config.sh [--compose-file <path>] [--env-file <path>]
#
# Environment:
#   NGINX_CONTAINER  container name (default: sms-nginx)
#   BASE_URL         externally reachable base URL (default: https://127.0.0.1)
#
# Exit codes: 0 = verified, 1 = a check failed.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

ENV_FILE="${ENV_FILE:-$PROJECT_DIR/.env}"
COMPOSE_FILE="${COMPOSE_FILE:-$PROJECT_DIR/docker/docker-compose.prod.yml}"
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-sms}"
NGINX_CONTAINER="${NGINX_CONTAINER:-sms-nginx}"
BASE_URL="${BASE_URL:-https://127.0.0.1}"

while [ $# -gt 0 ]; do
    case "$1" in
        --compose-file) COMPOSE_FILE="$2"; shift 2 ;;
        --env-file)    ENV_FILE="$2";    shift 2 ;;
        -h|--help)     sed -n '2,40p' "${BASH_SOURCE[0]}"; exit 0 ;;
        *) echo "Unknown argument: $1" >&2; exit 2 ;;
    esac
done

# Where nginx reads its configuration inside the container. MUST match the
# `command:` in the compose file.
NGINX_CONF_IN_CONTAINER="/etc/nginx/sms/nginx.conf"

fail() { echo -e "  ${RED}FAIL:${NC} $*" >&2; exit 1; }

echo -e "${YELLOW}========================================${NC}"
echo -e "  SMS nginx configuration verification${NC}"
echo -e "${YELLOW}========================================${NC}"
echo "  container     : $NGINX_CONTAINER"
echo "  base url      : $BASE_URL"
echo ""

command -v docker >/dev/null 2>&1 || fail "docker is not installed or not on PATH"

docker ps --format '{{.Names}}' | grep -qx "$NGINX_CONTAINER" \
    || fail "container '$NGINX_CONTAINER' is not running"
# ── Step 1: host file checksum ────────────────────────────────────────────
HOST_CONF="$PROJECT_DIR/docker/nginx/nginx.conf"
[ -f "$HOST_CONF" ] || fail "host configuration not found: $HOST_CONF"
HOST_SUM="$(md5sum "$HOST_CONF" | awk '{print $1}')"
echo "  [1/6] host configuration"
echo "        file      : $HOST_CONF"
echo "        md5       : $HOST_SUM"

# ── Step 2: configuration actually inside the container ───────────────────
# This is the check that would have caught the single-file inode trap: with the
# old single-file mount the container kept a DIFFERENT inode than the host.
echo "  [2/6] container configuration"
CONTAINER_SUM="$(docker exec "$NGINX_CONTAINER" md5sum "$NGINX_CONF_IN_CONTAINER" 2>/dev/null | awk '{print $1}' || true)"
[ -n "$CONTAINER_SUM" ] || fail "could not read $NGINX_CONF_IN_CONTAINER inside the container"
echo "        md5       : $CONTAINER_SUM"

if [ "$HOST_SUM" != "$CONTAINER_SUM" ]; then
    echo -e "        ${RED}STALE CONFIGURATION DETECTED${NC}"
    echo ""
    echo "    The host file and the container's copy differ, so nginx is serving"
    echo "    a configuration that is no longer the deployed one. Nothing has been"
    echo "    reloaded yet. Recreate the container to re-resolve the mount:"
    echo ""
    echo "      docker compose -f '$COMPOSE_FILE' --env-file '$ENV_FILE' \\"
    echo "        -p '$COMPOSE_PROJECT_NAME' up -d --force-recreate nginx"
    exit 1
fi
echo -e "        ${GREEN}match${NC} - the container holds the deployed configuration"

# ── Step 3: validate BEFORE touching the running process ──────────────────
echo "  [3/6] validating configuration"
if ! docker exec "$NGINX_CONTAINER" nginx -t; then
    fail "nginx -t failed; the running configuration was NOT reloaded"
fi

# ── Step 4: reload ────────────────────────────────────────────────────────
echo "  [4/6] reloading"
if ! docker exec "$NGINX_CONTAINER" nginx -s reload; then
    fail "nginx -s reload failed"
fi
sleep 2

# ── Step 5: the process survived and still parses ─────────────────────────
echo "  [5/6] process health"
docker exec "$NGINX_CONTAINER" nginx -t >/dev/null 2>&1 \
    || fail "configuration unreadable after reload"
docker ps --format '{{.Names}}' | grep -qx "$NGINX_CONTAINER" \
    || fail "container '$NGINX_CONTAINER' is not running after reload"
echo "        container still running"
# ── Step 6: externally observable behaviour ────────────────────────────────
echo "  [6/6] external behaviour"

# The SignalR hub must answer DIRECTLY. A 301/302 here is exactly the defect
# that stopped the WebSocket upgrade: a handshake cannot follow a redirect.
hub_code="$(curl -sk -o /dev/null -w '%{http_code}' "$BASE_URL/hub" 2>/dev/null || true)"
case "$hub_code" in
    301|302) fail "/hub returns $hub_code - the client cannot follow this redirect" ;;
    401|403) echo "        /hub          : $hub_code (reachable, authentication required)" ;;
    200|101) echo "        /hub          : $hub_code" ;;
    000|"")  fail "/hub is unreachable from $BASE_URL" ;;
    *)      fail "/hub returned unexpected status $hub_code" ;;
esac

# Negotiation must also be reachable without a redirect.
negotiate_code="$(curl -sk -o /dev/null -w '%{http_code}' -X POST "$BASE_URL/hub/negotiate" 2>/dev/null || true)"
case "$negotiate_code" in
    301|302) fail "/hub/negotiate returns $negotiate_code - unexpected redirect" ;;
    401|403) echo "        /hub/negotiate: $negotiate_code (reachable, CSRF/auth enforced)" ;;
    000|"")  fail "/hub/negotiate is unreachable from $BASE_URL" ;;
    *)      echo "        /hub/negotiate: $negotiate_code" ;;
esac

# The service worker must still be revalidated, never pinned immutable, or a
# newly deployed frontend could never reach an already-installed client.
sw_cache="$(curl -skI "$BASE_URL/sw.js" 2>/dev/null | tr -d '\r' | grep -i '^cache-control:' | head -1 || true)"
case "$sw_cache" in
    *immutable*) fail "sw.js is served immutable: $(echo "$sw_cache")" ;;
    "")          fail "sw.js returned no Cache-Control header" ;;
    *)           echo "        /sw.js        : ${sw_cache#Cache-Control: }" ;;
esac

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  nginx configuration verified${NC}"
echo -e "${GREEN}========================================${NC}"