#!/bin/bash
set -e

echo "Seeding database..."

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Get the directory of this script
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$DIR/.."

# Check if running in Docker
if [ -f /.dockerenv ] || [ -f /run/.containerenv ]; then
    echo -e "${YELLOW}Running in container...${NC}"
    dotnet run --project src/SMS.API -- seed-data
else
    echo -e "${YELLOW}Running locally...${NC}"

    # Check if PostgreSQL is running (Docker maps to 5433 on host)
    if ! pg_isready -h localhost -p 5433 -U sms_user > /dev/null 2>&1; then
        echo -e "${YELLOW}PostgreSQL is not running. Please start it first.${NC}"
        echo "  docker-compose -f docker/docker-compose.yml up -d postgres"
        exit 1
    fi

    # Run seed
    dotnet run --project src/SMS.API -- seed-data
fi

echo -e "${GREEN}Database seeded successfully!${NC}"
