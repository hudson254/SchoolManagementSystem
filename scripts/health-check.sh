#!/bin/bash
set -euo pipefail

# =============================================================================
# School Management System - Production Health Check Script
# =============================================================================
# Checks all production services and returns meaningful exit codes.
# Supports configuration via environment variables.
#
# Usage:
#   ./scripts/health-check.sh [--api-url URL] [--frontend-url URL] [--prometheus-url URL] [--grafana-url URL]
#
# Environment variables (with defaults):
#   API_URL=http://localhost:5000
#   FRONTEND_URL=http://localhost:3000
#   NGINX_URL=http://localhost
#   PROMETHEUS_URL=http://localhost:9090
#   GRAFANA_URL=http://localhost:3001
# =============================================================================

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration with environment variable overrides
API_URL="${API_URL:-http://localhost:5000}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost:3000}"
NGINX_URL="${NGINX_URL:-http://localhost}"
PROMETHEUS_URL="${PROMETHEUS_URL:-http://localhost:9090}"
GRAFANA_URL="${GRAFANA_URL:-http://localhost:3001}"

FAILED=0

check_endpoint() {
    local name=$1
    local url=$2
    local expected_status=${3:-200}
    local timeout=${4:-10}

    echo -n "Checking $name ($url)... "

    # Use curl with configurable timeout
    status=$(curl -s -o /dev/null -w "%{http_code}" --max-time "$timeout" "$url" 2>/dev/null || echo "failed")

    if [ "$status" = "$expected_status" ]; then
        echo -e "${GREEN}✓ OK${NC}"
        return 0
    else
        echo -e "${RED}✗ FAILED (status: $status, expected: $expected_status)${NC}"
        FAILED=1
        return 1
    fi
}

check_tcp() {
    local name=$1
    local host=$2
    local port=$3
    local timeout=${4:-5}

    echo -n "Checking $name ($host:$port)... "

    if timeout "$timeout" bash -c "echo > /dev/tcp/$host/$port" 2>/dev/null; then
        echo -e "${GREEN}✓ OPEN${NC}"
        return 0
    else
        echo -e "${RED}✗ CLOSED${NC}"
        FAILED=1
        return 1
    fi
}

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}  SMS Production Health Check${NC}"
echo -e "${YELLOW}========================================${NC}"
echo ""

# API Health
echo -e "${YELLOW}--- API Services ---${NC}"
check_endpoint "API Health" "$API_URL/health" 200
check_endpoint "API Readiness" "$API_URL/health/ready" 200
check_endpoint "API Liveness" "$API_URL/health/live" 200
check_endpoint "API Database" "$API_URL/health/database" 200

# Frontend
echo ""
echo -e "${YELLOW}--- Frontend ---${NC}"
check_endpoint "Frontend" "$FRONTEND_URL" 200

# Nginx
echo ""
echo -e "${YELLOW}--- Nginx Proxy ---${NC}"
check_endpoint "Nginx HTTP" "$NGINX_URL" 200 10
check_endpoint "Nginx API Proxy" "$NGINX_URL/api/v1/auth/login" 400 10

# Monitoring
echo ""
echo -e "${YELLOW}--- Monitoring ---${NC}"
check_endpoint "Prometheus" "$PROMETHEUS_URL/-/healthy" 200
check_endpoint "Grafana" "$GRAFANA_URL/api/health" 200

# Docker containers
echo ""
echo -e "${YELLOW}--- Docker Containers ---${NC}"
if command -v docker &> /dev/null; then
    for container in sms-postgres sms-api sms-web sms-nginx sms-backup sms-prometheus sms-grafana; do
        echo -n "Checking container $container... "
        status=$(docker inspect --format='{{.State.Status}}' "$container" 2>/dev/null || echo "not_found")
        if [ "$status" = "running" ]; then
            echo -e "${GREEN}✓ RUNNING${NC}"
        elif [ "$status" = "not_found" ]; then
            echo -e "${YELLOW}⚠ NOT FOUND (may not be deployed)${NC}"
        else
            echo -e "${RED}✗ $status${NC}"
            FAILED=1
        fi
    done
else
    echo -e "${YELLOW}  Docker not available - skipping container checks${NC}"
fi

echo ""
if [ "$FAILED" -eq 0 ]; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  All health checks passed!${NC}"
    echo -e "${GREEN}========================================${NC}"
    exit 0
else
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}  Some health checks failed!${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi
