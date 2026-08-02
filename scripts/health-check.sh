#!/bin/bash
set -e

echo "Running health check..."

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

API_URL=${API_URL:-"https://localhost:5001"}
FRONTEND_URL=${FRONTEND_URL:-"https://localhost"}

check_endpoint() {
    local name=$1
    local url=$2
    local expected_status=${3:-200}
    
    echo -n "Checking $name... "
    
    # Use curl with -k for self-signed certs
    status=$(curl -k -s -o /dev/null -w "%{http_code}" "$url")
    
    if [ "$status" -eq "$expected_status" ]; then
        echo -e "${GREEN}✓ OK${NC}"
        return 0
    else
        echo -e "${RED}✗ FAILED (status: $status)${NC}"
        return 1
    fi
}

echo -e "${YELLOW}Checking system health...${NC}"
echo ""

# Check API health
check_endpoint "API Health" "$API_URL/health" 200
check_endpoint "API Readiness" "$API_URL/health/ready" 200
check_endpoint "API Liveness" "$API_URL/health/live" 200

# Check frontend
check_endpoint "Frontend" "$FRONTEND_URL" 200

# Check database (via API)
check_endpoint "Database" "$API_URL/health/database" 200

echo ""
echo -e "${YELLOW}System health check completed.${NC}"