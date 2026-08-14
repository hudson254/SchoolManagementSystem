# Backup and Recovery Guide

## Purpose

This guide provides detailed procedures for backing up and restoring the School Management System data.

## Scope

This guide covers:
- Manual backup procedures
- Automated backup configuration
- Backup verification
- Restore procedures
- Point-in-time recovery
- Disaster recovery
- Recovery validation

## Backup Types

### 1. Full Database Backup
A complete snapshot of the PostgreSQL database.

### 2. File Storage Backup
Backup of uploaded files, generated reports, and certificates.

### 3. Configuration Backup
Backup of environment configuration and application settings.

## Manual Backup Procedures

### Database Backup (pg_dump)

```bash
# Backup entire database
pg_dump -h localhost -U sms_admin -d sms_db -F c -f sms_backup_$(date +%Y%m%d).dump

# Backup with compression
pg_dump -h localhost -U sms_admin -d sms_db -F c -Z 9 -f sms_backup_$(date +%Y%m%d).dump

# Backup specific schema only
pg_dump -h localhost -U sms_admin -d sms_db -n public -f sms_schema_backup.sql
```

### File Storage Backup

```bash
# Backup uploads directory
tar -czf uploads_backup_$(date +%Y%m%d).tar.gz uploads/

# Backup certificates
tar -czf certs_backup_$(date +%Y%m%d).tar.gz certificates/
```

### Configuration Backup

```bash
# Backup environment files
cp .env .env.backup_$(date +%Y%m%d)
cp docker-compose.yml docker-compose.yml.backup_$(date +%Y%m%d)
```

## Automated Backup Script

The system includes an automated backup script at `scripts/backup.sh`:

```bash
#!/bin/bash
# Automated backup script
BACKUP_DIR="/backups/sms"
DATE=$(date +%Y%m%d_%H%M%S)

# Database backup
pg_dump -h $DB_HOST -U $DB_USER -d $DB_NAME -F c -f "$BACKUP_DIR/db_$DATE.dump"

# File storage backup
tar -czf "$BACKUP_DIR/uploads_$DATE.tar.gz" /app/uploads

# Cleanup old backups (keep 30 days)
find $BACKUP_DIR -type f -mtime +30 -delete
```

### Configuring Automated Backups

**Using cron (Linux):**
```bash
# Edit crontab
crontab -e

# Add daily backup at 2 AM
0 2 * * * /path/to/scripts/backup.sh
```

**Using Docker:**
The production Docker Compose includes a backup service:
```bash
docker-compose exec backup ./backup.sh
```

## Backup Verification

### Verify Database Backup Integrity

```bash
# Test backup file integrity
pg_restore -l sms_backup.dump > /dev/null 2>&1 && echo "Backup valid" || echo "Backup corrupt"

# List contents of backup
pg_restore -l sms_backup.dump | head -20
```

### Verify File Storage Backup

```bash
# Test archive integrity
tar -tzf uploads_backup.tar.gz > /dev/null 2>&1 && echo "Archive valid" || echo "Archive corrupt"

# List contents
tar -tzf uploads_backup.tar.gz | head -20
```

## Restore Procedures

### Database Restore

```bash
# Restore from custom format dump
pg_restore -h localhost -U sms_admin -d sms_db -c sms_backup.dump

# Restore with parallel jobs (faster for large databases)
pg_restore -h localhost -U sms_admin -d sms_db -j 4 -c sms_backup.dump

# Restore to a different database name
createdb -U sms_admin sms_db_restored
pg_restore -h localhost -U sms_admin -d sms_db_restored sms_backup.dump
```

### File Storage Restore

```bash
# Restore uploads
tar -xzf uploads_backup.tar.gz -C /app/

# Restore with specific path
tar -xzf uploads_backup.tar.gz -C /restore/path/
```

### Full System Restore

1. Stop the application:
```bash
docker-compose down
```

2. Restore database:
```bash
docker-compose up -d postgres
docker-compose exec -T postgres pg_restore -U sms_admin -d sms_db < backup.dump
```

3. Restore file storage:
```bash
tar -xzf uploads_backup.tar.gz -C /app/
```

4. Restart the application:
```bash
docker-compose up -d
```

5. Run any pending migrations:
```bash
docker-compose exec api dotnet run -- migrate-database
```

## Point-in-Time Recovery

### Prerequisites
- PostgreSQL WAL archiving enabled
- Base backup
- All WAL segments since the base backup

### Recovery Steps

1. Restore base backup
2. Configure recovery.conf with target time
3. Start PostgreSQL in recovery mode
4. Verify data at target time

## Disaster Recovery

### Recovery Plan

1. **Assess damage**: Determine what data needs to be restored
2. **Prepare environment**: Set up clean infrastructure
3. **Restore database**: Using latest valid backup
4. **Restore files**: Restore uploads and certificates
5. **Verify integrity**: Check data consistency
6. **Test access**: Verify application functionality
7. **Monitor**: Watch for issues post-recovery

### Recovery Time Objectives (RTO)

- **Database restore**: 1-2 hours for full backup
- **File storage restore**: 30 minutes
- **Full system recovery**: 2-4 hours

### Recovery Point Objectives (RPO)

- **Database**: Up to 24 hours (daily backup)
- **File storage**: Up to 24 hours (daily backup)
- **Configuration**: Up to 7 days (weekly backup)

## Recovery Validation

### Post-Recovery Checklist

- [ ] Database restored successfully
- [ ] All tables present and populated
- [ ] Application starts without errors
- [ ] Users can log in
- [ ] File uploads accessible
- [ ] Reports can be generated
- [ ] Certificates can be verified
- [ ] Notifications are working
- [ ] Audit logs are intact
- [ ] Performance is normal

### Testing Restores

Regularly test restore procedures:
1. Restore to a test environment
2. Verify data integrity
3. Run application tests
4. Document any issues

## Backup Storage

### Recommended Storage Strategy

- **Local**: Keep 7 days of daily backups
- **Offsite**: Keep 30 days of daily backups
- **Monthly**: Keep 12 monthly backups
- **Annual**: Keep yearly backups indefinitely

### Storage Locations

```
/backups/sms/
├── daily/          # Last 7 days
├── weekly/         # Last 4 weeks
├── monthly/        # Last 12 months
└── annual/         # Indefinite
```

## Common Backup Issues

### Backup Fails
- Check disk space
- Verify database connectivity
- Check file permissions

### Restore Fails
- Verify backup file is not corrupted
- Check PostgreSQL version compatibility
- Ensure sufficient disk space

### Slow Backup
- Increase resources for backup process
- Use parallel backup jobs
- Schedule during low-traffic periods

## Related Documentation

- [Database Administration Guide](../13-Database/README.md)
- [Maintenance Guide](../15-Maintenance/README.md)
- [System Administration Guide](../06-System-Administration/README.md)
- [Troubleshooting Guide](../16-Troubleshooting/README.md)
