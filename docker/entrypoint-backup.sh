#!/bin/sh
set -e

echo "Starting backup service..."

# Run initial backup
echo "Running initial backup..."
/scripts/backup.sh

# Start backup loop
echo "Starting backup loop with interval: ${BACKUP_INTERVAL} seconds"

while true; do
    sleep ${BACKUP_INTERVAL}
    echo "Running scheduled backup..."
    /scripts/backup.sh
done