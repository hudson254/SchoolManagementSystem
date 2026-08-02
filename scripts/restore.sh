#!/bin/bash
set -e

echo "Restoring database from backup..."

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

if [ -z "$1" ]; then
    echo -e "${RED}Usage: $0 <backup_file.sql.gz>${NC}"
    echo ""
    echo "Available backups:"
    ls -la /var/backups/sms/*.sql.gz 2>/dev/null || echo "No backups found in /var/backups/sms/"
    exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "$BACKUP_FILE" ]; then
    echo -e "${RED}Backup file not found: $BACKUP_FILE${NC}"
    exit 1
fi

echo -e "${YELLOW}Restoring from: $BACKUP_FILE${NC}"

# Ask for confirmation
read -p "This will overwrite the current database. Are you sure? (y/N) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Restore cancelled.${NC}"
    exit 0
fi

# Determine if running in Docker
if [ -f /.dockerenv ] || [ -f /run/.containerenv ]; then
    echo -e "${YELLOW}Running in container...${NC}"
    gunzip -c "$BACKUP_FILE" | psql -U ${DB_USER:-sms_user} -d ${DB_NAME:-SchoolManagementSystem}
else
    echo -e "${YELLOW}Running locally...${NC}"
    
    # Check if PostgreSQL is running
    if ! pg_isready -h localhost -p 5432 -U ${DB_USER:-sms_user} > /dev/null 2>&1; then
        echo -e "${RED}PostgreSQL is not running. Please start it first.${NC}"
        exit 1
    fi
    
    gunzip -c "$BACKUP_FILE" | psql -U ${DB_USER:-sms_user} -d ${DB_NAME:-SchoolManagementSystem}
fi

echo -e "${GREEN}Database restored successfully!${NC}"