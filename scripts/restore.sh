#!/bin/bash
set -e

echo "Restoring database from backup..."

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

if [ -z "$1" ]; then
    echo -e "${RED}Usage: $0 <backup_file>${NC}"
    echo ""
    echo "Supported formats:"
    echo "  *.dump    - Custom-format pg_dump (created by backup.sh)"
    echo "  *.sql.gz  - Plain SQL compressed backup"
    echo ""
    echo "Available backups:"
    ls -la /var/backups/sms/*.{dump,sql.gz} 2>/dev/null || echo "No backups found in /var/backups/sms/"
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

# Determine file type and restore accordingly
case "$BACKUP_FILE" in
    *.dump)
        # Custom-format dump from pg_dump -Fc
        echo -e "${YELLOW}Detected custom-format dump. Using pg_restore...${NC}"
        pg_restore -U ${DB_USER:-sms_user} -d ${DB_NAME:-SchoolManagementSystem} -Fc -c --if-exists "$BACKUP_FILE"
        ;;
    *.sql.gz)
        # Plain SQL compressed backup
        echo -e "${YELLOW}Detected compressed SQL. Using gunzip + psql...${NC}"
        gunzip -c "$BACKUP_FILE" | psql -U ${DB_USER:-sms_user} -d ${DB_NAME:-SchoolManagementSystem}
        ;;
    *.sql)
        # Plain SQL uncompressed backup
        echo -e "${YELLOW}Detected plain SQL. Using psql...${NC}"
        psql -U ${DB_USER:-sms_user} -d ${DB_NAME:-SchoolManagementSystem} -f "$BACKUP_FILE"
        ;;
    *)
        echo -e "${RED}Unknown backup format: $BACKUP_FILE${NC}"
        echo "Supported formats: .dump, .sql.gz, .sql"
        exit 1
        ;;
esac

echo -e "${GREEN}Database restored successfully!${NC}"
