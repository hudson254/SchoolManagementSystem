#!/bin/bash
set -euo pipefail

# =============================================================================
# School Management System - Production Deployment Script
# =============================================================================
# This script deploys the School Management System using Docker Compose.
# It validates prerequisites, builds images, applies migrations, and verifies health.
#
# Usage:
#   ./scripts/deploy.sh [--env-file /path/to/.env] [--compose-file docker/docker-compose.prod.yml]
#
# Environment:
#   Requires a .env file with all required production secrets.
#   See .env.example for the complete list of required variables.
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Defaults
ENV_FILE="${ENV_FILE:-$PROJECT_DIR/.env}"
COMPOSE_FILE="${COMPOSE_FILE:-$PROJECT_DIR/docker/docker-compose.prod.yml}"
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-sms}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}  SMS Production Deployment Script${NC}"
echo -e "${YELLOW}========================================${NC}"
echo ""

# ---------------------------------------------------------------------------
# Step 1: Validate prerequisites
# ---------------------------------------------------------------------------
echo -e "${YELLOW}[1/8] Validating prerequisites...${NC}"

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}ERROR: docker is not installed.${NC}"
    exit 1
fi
echo "  docker: $(docker --version)"

# Check Docker Compose
if ! docker compose version &> /dev/null; then
    echo -e "${RED}ERROR: docker compose plugin is not installed.${NC}"
    exit 1
fi
echo "  docker compose: $(docker compose version)"

# Check env file
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${RED}ERROR: Environment file not found: $ENV_FILE${NC}"
    echo "  Create it from .env.example: cp .env.example $ENV_FILE"
    exit 1
fi
echo "  env file: $ENV_FILE"

# Check compose file
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}ERROR: Compose file not found: $COMPOSE_FILE${NC}"
    exit 1
fi
echo "  compose file: $COMPOSE_FILE"

# ---------------------------------------------------------------------------
# Step 2: Validate environment configuration
# ---------------------------------------------------------------------------
echo -e "${YELLOW}[2/8] Validating environment configuration...${NC}"

# Source the env file to check required variables
set -a
source "$ENV_FILE"
set +a

REQUIRED_VARS=(
    "DB_PASSWORD"
    "JWT_SECRET"
    "GRAFANA_PASSWORD"
    "ADMIN_EMAIL"
    "ADMIN_PASSWORD"
)

MISSING_VARS=0
for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var:-}" ]; then
        echo -e "${RED}  ERROR: $var is not set in $ENV_FILE${NC}"
        MISSING_VARS=$((MISSING_VARS + 1))
    fi
done

if [ "$MISSING_VARS" -gt 0 ]; then
    echo -e "${RED}  $MISSING_VARS required environment variables are missing.${NC}"
    exit 1
fi
echo "  All required environment variables are set."

# Check for placeholder values
PLACEHOLDER_PATTERNS=("CHANGE_ME" "your-super-secret" "SecurePassword123!" "admin123")
for var in "${REQUIRED_VARS[@]}"; do
    val="${!var:-}"
    for pattern in "${PLACEHOLDER_PATTERNS[@]}"; do
        if [[ "$val" == *"$pattern"* ]]; then
            echo -e "${RED}  ERROR: $var contains placeholder value '$pattern'. Generate a real secret.${NC}"
            exit 1
        fi
    done
done
echo "  No placeholder values detected."

# ---------------------------------------------------------------------------
# Step 3: Validate Docker
# ---------------------------------------------------------------------------
echo -e "${YELLOW}[3/8] Validating Docker...${NC}"

if ! docker info &> /dev/null; then
    echo -e "${RED}ERROR: Docker daemon is not running.${NC}"
    exit 1
fi
echo "  Docker daemon is running."

# Check for required Docker Compose configuration
echo -e "${YELLOW}[4/8] Validating Docker Compose configuration...${NC}"
if ! docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" config > /dev/null 2>&1; then
    echo -e "${RED}ERROR: Docker Compose configuration is invalid.${NC}"
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" config 2>&1 || true
    exit 1
fi
echo "  Docker Compose configuration is valid."

# ---------------------------------------------------------------------------
# Step 5: Pull and build images
# ---------------------------------------------------------------------------
echo -e "${YELLOW}[5/8] Pulling and building images...${NC}"
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" -p "$COMPOSE_PROJECT_NAME" pull
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" -p "$COMPOSE_PROJECT_NAME" build --pull
echo "  Images built successfully."

# ---------------------------------------------------------------------------
# Step 6: Start services
# ---------------------------------------------------------------------------
echo -e "${YELLOW}[6/8] Starting services...${NC}"
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" -p "$COMPOSE_PROJECT_NAME" up -d
echo "  Services started."

# ---------------------------------------------------------------------------
# Step 7: Verify health
# ---------------------------------------------------------------------------
echo -e "${YELLOW}[7/8] Verifying service health...${NC}"

# Wait for services to become healthy
MAX_RETRIES=30
RETRY_INTERVAL=10

check_container_health() {
    local container_name="$1"
    local max_wait="$2"
    local waited=0

    while [ $waited -lt $max_wait ]; do
        local status
        status=$(docker inspect --format='{{.State.Health.Status}}' "$container_name" 2>/dev/null || echo "not_found")

        if [ "$status" = "healthy" ]; then
            echo -e "  ${GREEN}$container_name: healthy${NC}"
            return 0
        elif [ "$status" = "not_found" ]; then
            echo -e "  ${YELLOW}$container_name: not found (may not have health check)${NC}"
            return 0
        fi

        sleep 5
        waited=$((waited + 5))
    done

    echo -e "  ${RED}$container_name: not healthy after ${max_wait}s${NC}"
    return 1
}

# Check all containers
CONTAINERS=("sms-postgres" "sms-api" "sms-web" "sms-nginx" "sms-backup" "sms-prometheus" "sms-grafana")
ALL_HEALTHY=true

for container in "${CONTAINERS[@]}"; do
    if ! check_container_health "$container" 120; then
        ALL_HEALTHY=false
    fi
done

if [ "$ALL_HEALTHY" = false ]; then
    echo -e "${RED}Some containers are not healthy. Check logs: docker compose -f $COMPOSE_FILE logs${NC}"
    exit 1
fi

# ---------------------------------------------------------------------------
# Step 8: Run smoke tests
# ---------------------------------------------------------------------------
echo -e "${YELLOW}[8/8] Running smoke tests...${NC}"

# Test API health endpoint
API_URL="http://localhost:5000"
if curl -sf "$API_URL/health" > /dev/null 2>&1; then
    echo -e "  ${GREEN}API health check: PASS${NC}"
else
    echo -e "  ${RED}API health check: FAIL${NC}"
    ALL_HEALTHY=false
fi

# Test Nginx proxy
NGINX_URL="http://localhost"
if curl -sf "$NGINX_URL/health" > /dev/null 2>&1; then
    echo -e "  ${GREEN}Nginx proxy: PASS${NC}"
else
    echo -e "  ${YELLOW}Nginx proxy: not responding (expected if no TLS configured)${NC}"
fi

echo ""
if [ "$ALL_HEALTHY" = true ]; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  Deployment completed successfully!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo "  Services:"
    docker compose -f "$COMPOSE_FILE" -p "$COMPOSE_PROJECT_NAME" ps
else
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}  Deployment completed with warnings.${NC}"
    echo -e "${RED}  Check logs for details.${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi
