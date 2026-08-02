#!/bin/sh
set -e

echo "Backup script started at $(date -u +"%Y-%m-%dT%H:%M:%SZ")"

# Validate required environment
: "${DB_HOST:?DB_HOST is required}"
: "${DB_NAME:?DB_NAME is required}"
: "${DB_USER:?DB_USER is required}"
: "${DB_PASSWORD:?DB_PASSWORD is required}"
: "${BACKUP_DIR:=/backups}"
: "${BACKUP_RETENTION_DAYS:=30}"

# Create backup directory if missing
mkdir -p "${BACKUP_DIR}"

# Use password without leaking it into process lists where possible
export PGPASSWORD="${DB_PASSWORD}"

TIMESTAMP=$(date -u +"%Y%m%dT%H%M%SZ")
BACKUP_FILE="${BACKUP_DIR}/${DB_NAME}_${TIMESTAMP}.dump"

echo "Creating backup: ${BACKUP_FILE}"
pg_dump -h "${DB_HOST}" -U "${DB_USER}" -d "${DB_NAME}" -Fc -f "${BACKUP_FILE}"

echo "Backup created successfully: ${BACKUP_FILE}"

# Retention pruning
echo "Pruning backups older than ${BACKUP_RETENTION_DAYS} days..."
find "${BACKUP_DIR}" -type f -name "${DB_NAME}_*.dump" -mtime "+${BACKUP_RETENTION_DAYS}" -delete

echo "Backup script completed at $(date -u +"%Y-%m-%dT%H:%M:%SZ")"
