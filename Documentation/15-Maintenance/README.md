# Maintenance Guide

## Purpose

This guide provides procedures for routine maintenance of the School Management System.

## Routine Maintenance Tasks

### Daily Tasks

- **Health Check Verification**: Verify API health endpoint returns healthy
- **Error Log Review**: Check for new errors in error logs
- **Backup Verification**: Verify last night's backup completed successfully
- **Disk Space Monitoring**: Check available disk space

### Weekly Tasks

- **Database Vacuum**: Run PostgreSQL VACUUM to reclaim storage
- **Log Rotation**: Archive old log files
- **User Account Review**: Review inactive or locked accounts
- **Performance Review**: Check API response times

### Monthly Tasks

- **Database Reindex**: Rebuild indexes for optimal performance
- **Security Audit**: Review audit logs for suspicious activity
- **Backup Restore Test**: Test restoring from backup
- **Certificate Expiry Check**: Check SSL/TLS certificate expiry

### Quarterly Tasks

- **Software Update Review**: Check for available updates
- **Security Patch Application**: Apply security patches
- **Full Backup Test**: Perform full system restore test
- **Capacity Planning Review**: Assess storage and resource needs

## Monitoring

### Health Endpoints

The system provides health check endpoints:

- `/health` - Basic health check
- `/health/ready` - Readiness check
- `/health/live` - Liveness check

### Monitoring with Prometheus

Prometheus metrics are available at `/metrics`:
- Request count and duration
- Error rates by endpoint
- Database connection pool statistics
- Memory and CPU usage

### Monitoring with Grafana

The system includes pre-configured Grafana dashboards:
- API Performance Dashboard
- Database Metrics Dashboard
- System Resource Dashboard
- Error Rate Dashboard

### Setting Up Alerts

Alertmanager configuration is in `docker/alertmanager.yml`:
```yaml
route:
  receiver: 'admin'
  routes:
    - match:
        severity: critical
      receiver: 'admin-critical'

receivers:
  - name: 'admin'
    email_configs:
      - to: 'admin@example.com'
```

## Log Cleanup

### Log File Locations

- Application logs: `logs/sms-*.json` and `logs/sms-*.txt`
- Nginx logs: Container stdout
- PostgreSQL logs: Container stdout

### Log Rotation Configuration

Serilog is configured for daily log rotation:
```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/sms-.json",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### Manual Log Cleanup

```bash
# Remove logs older than 30 days
find logs/ -name "*.json" -mtime +30 -delete
find logs/ -name "*.txt" -mtime +30 -delete

# Archive logs before deletion
tar -czf logs_archive_$(date +%Y%m).tar.gz logs/*.json
```

## Database Maintenance

### Vacuum

```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U sms_admin -d sms_db

# Run VACUUM
VACUUM (VERBOSE, ANALYZE);

# Run VACUUM FULL (requires exclusive lock)
VACUUM FULL VERBOSE;
```

### Reindexing

```bash
# Reindex specific table
REINDEX TABLE students;

# Reindex entire database
REINDEX DATABASE sms_db;
```

### Update Statistics

```bash
# Update database statistics
ANALYZE VERBOSE;

# Analyze specific table
ANALYZE VERBOSE students;
```

### Check Database Integrity

```bash
# Check database for corruption
docker-compose exec postgres pg_checksums -c -D /var/lib/postgresql/data

# Check specific table
docker-compose exec postgres psql -U sms_admin -d sms_db -c "SELECT * FROM pg_stat_user_tables WHERE relname = 'students';"
```

## Performance Tuning

### Database Performance

- Monitor slow queries in PostgreSQL logs
- Add indexes for frequently queried columns
- Optimize connection pool size
- Increase shared_buffers for larger databases

### Application Performance

- Enable response caching where appropriate
- Optimize database queries
- Use connection pooling
- Configure appropriate timeouts

### Frontend Performance

- Enable gzip compression in Nginx
- Configure browser caching headers
- Optimize API response sizes
- Use lazy loading for components

## Health Checks

### API Health Check

```bash
# Basic health check
curl http://localhost:8080/health

# Response
{
  "status": "Healthy",
  "duration": 15.2,
  "entries": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "Database connection successful",
      "duration": 5.1
    }
  ]
}
```

### Docker Health Check

```bash
# Check container status
docker-compose ps

# View container logs
docker-compose logs --tail=50 api

# Check resource usage
docker stats
```

## System Updates

### Checking for Updates

```bash
# Check git for updates
git fetch
git log --oneline HEAD..origin/main

# Check for package updates
dotnet list package --outdated
```

### Applying Updates

1. Backup database and files
2. Pull latest code
3. Update dependencies
4. Rebuild application
5. Run migrations
6. Restart services

### Rollback Plan

1. Stop services
2. Revert code to previous version
3. Restore database from backup
4. Restart services
5. Verify functionality

## Storage Management

### Monitoring Storage

```bash
# Check disk usage
df -h

# Check directory sizes
du -sh /app/uploads/
du -sh /app/logs/

# Check database size
docker-compose exec postgres psql -U sms_admin -d sms_db -c "SELECT pg_size_pretty(pg_database_size('sms_db'));"
```

### Cleanup Tasks

- Remove old log files
- Archive old backups
- Clean up temporary uploads
- Remove orphaned file records

## Best Practices

1. **Schedule maintenance during low-traffic periods**
2. **Always backup before making changes**
3. **Test changes in staging before production**
4. **Document all maintenance activities**
5. **Monitor system after maintenance**
6. **Keep a maintenance log**
7. **Review and update maintenance procedures regularly**

## Related Documentation

- [System Administration Guide](../06-System-Administration/README.md)
- [Backup and Recovery Guide](../14-Backup-and-Recovery/README.md)
- [Database Guide](../13-Database/README.md)
- [Troubleshooting Guide](../16-Troubleshooting/README.md)
