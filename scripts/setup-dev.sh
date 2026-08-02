#!/bin/bash
set -e

echo "Setting up development environment..."

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$DIR/.."

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker not found. Please install Docker first.${NC}"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}Docker Compose not found. Please install Docker Compose first.${NC}"
    exit 1
fi

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}.NET SDK not found. Please install .NET 9 SDK first.${NC}"
    exit 1
fi

# Create .env file if not exists
if [ ! -f .env ]; then
    echo -e "${YELLOW}Creating .env file from template...${NC}"
    cp .env.example .env
    echo -e "${YELLOW}Please review and update .env file with your settings.${NC}"
fi

# Generate SSL certificates
echo -e "${YELLOW}Generating SSL certificates...${NC}"
./scripts/generate-ssl.sh

# Start services
echo -e "${YELLOW}Starting services...${NC}"
docker-compose -f docker/docker-compose.dev.yml up -d

# Wait for services to be ready
echo -e "${YELLOW}Waiting for services to be ready...${NC}"
sleep 10

# Run migrations
echo -e "${YELLOW}Running database migrations...${NC}"
./scripts/migrate.sh

# Seed data
echo -e "${YELLOW}Seeding database...${NC}"
./scripts/seed.sh

# Run health check
echo -e "${YELLOW}Running health check...${NC}"
./scripts/health-check.sh

echo -e "${GREEN}Development environment setup complete!${NC}"
echo ""
echo -e "${GREEN}Access the application:${NC}"
echo "  - Web Application: https://localhost"
echo "  - API Documentation: https://localhost:5001/swagger"
echo "  - pgAdmin: http://localhost:5050 (admin@school.com / admin123)"
echo "  - Grafana: http://localhost:3001 (admin / admin123)"
echo ""
echo -e "${YELLOW}Default credentials:${NC}"
echo "  - Admin: admin@school.com / Admin123!"
echo "  - Student: student@school.com / Student123!"
echo "  - Lecturer: lecturer@school.com / Lecturer123!"