# System Administrator Guide

## Purpose

This guide documents system administration tasks for the School Management System, including setup, configuration, monitoring, maintenance, and troubleshooting.

## System Setup

### Initial Configuration

After installation, configure the following:

1. **Database Connection**: Verify connection string in `appsettings.json` or environment variables
2. **JWT Secret**: Set a strong, unique JWT signing key
3. **CORS Origins**: Configure allowed frontend URLs
4. **File Storage**: Set upload directory and limits
5. **Rate Limiting**: Configure API rate limits
6. **Logging**: Verify log output destinations

### Environment Configuration Files

The system supports multiple environment configurations:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Testing.json` - Testing environment
- `appsettings.Staging.json` - Staging environment
- `appsettings.Production.json` - Production overrides

## User Account Management

### Creating User Accounts

1. Access the admin panel
2. Navigate to **Users** > **Create User**
3. Fill in required fields (name, email, username)
4. Assign roles (Administrator, Coordinator, Lecturer, Student, Receptionist)
5. Set initial password (must meet complexity requirements)
6. Click **Save**

### Managing User Roles

Users can have multiple roles. To modify roles:
1. Navigate to **Users** > **User List**
2. Search for the user
3. Click **Edit**
4. Select/deselect roles
5. Save changes

### Password Management

- **Reset Password**: Generates a temporary password for the user
- **Force Password Change**: Requires user to change password on next login
- **Password Policy**: Configured in Identity options (minimum 12 chars, complexity requirements)

### Account Lockout

- Automatic lockout after 5 failed login attempts
- Lockout duration: 15 minutes
- Administrators can manually unlock accounts

## Role and Permission Management

### Built-in Roles

| Role | Description |
|------|-------------|
| Administrator | Full system access |
| Coordinator | Academic coordination |
| Lecturer | Teaching and grading |
| Student | Learning and enrollment |
| Receptionist | Limited administrative access |

### Authorization Policies

```csharp
AdministratorAccess  → Administrator only
ModeratorAccess      → Administrator, Coordinator
LecturerAccess       → Administrator, Coordinator, Lecturer
StudentAccess        → Administrator, Coordinator, Lecturer, Student
ReceptionistAccess   → Administrator, Coordinator, Receptionist
```

## Course and Academic Management

### Creating Courses

1. Navigate to **Courses** > **Create Course**
2. Enter course code, name, description, programme
3. Set duration and credit hours
4. Save

### Managing Units

1. Navigate to **Units** > **Create Unit**
2. Enter unit code, name, credit hours
3. Assign to department
4. Save

### Academic Years and Semesters

1. Navigate to **Academic** > **Academic Years**
2. Create academic year with start/end dates
3. Create semesters within the academic year
4. Set semester dates and status

## Report Generation

### Available Reports

- Student Enrollment Reports
- Grade Reports (by unit, student, course)
- Attendance Reports
- Accommodation Reports (occupancy, maintenance)
- User Activity Reports
- Audit Log Reports

### Generating a Report

1. Navigate to **Reports**
2. Select report type
3. Configure filters (date range, department, course, etc.)
4. Click **Generate**
5. Download as PDF or Excel

### Report Authentication

Generated reports include:
- QR code for verification
- SHA-256 hash
- Watermark
- Verification token

## Notification Management

### Creating Notifications

1. Navigate to **Notifications** > **Create**
2. Select recipients (all users, by role, specific users)
3. Enter title and message
4. Set priority
5. Send

### Notification Channels

- In-app (SignalR real-time)
- SMS (Twilio, if configured)

## Audit Logs

### Viewing Audit Logs

1. Navigate to **System** > **Audit Logs**
2. Filter by date, user, action, entity
3. Export logs for analysis

### Audit Log Retention

Logs are retained indefinitely. Archive or purge old logs periodically to manage storage.

## Security Settings

### Password Policy Configuration

Configured in `appsettings.json`:
```json
{
  "Identity": {
    "Password": {
      "RequiredLength": 12,
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": true,
      "RequiredUniqueChars": 4
    },
    "Lockout": {
      "MaxFailedAccessAttempts": 5,
      "DefaultLockoutTimeSpan": "00:15:00"
    }
  }
}
```

### Rate Limiting

Configure rate limiting to prevent abuse:
```json
{
  "RateLimiting": {
    "Enabled": true,
    "MaxRequests": 100,
    "WindowSeconds": 60
  }
}
```

## Performance Monitoring

### Health Checks

Available at `/health` endpoint. Returns:
- Overall status
- Database connectivity
- Response time

### Metrics

Prometheus metrics available at `/metrics` endpoint:
- Request counts
- Response times
- Error rates
- Active connections

### Monitoring with Grafana

The system includes Grafana dashboards for:
- API performance
- Database metrics
- System resources
- Error rates

## System Health

### Health Dashboard

The system health dashboard shows:
- API status
- Database connectivity
- Cache status (Redis)
- Storage usage
- Background job status

### Common Health Issues

| Issue | Indicator | Resolution |
|-------|-----------|------------|
| Database down | Health check fails | Check PostgreSQL container |
| Storage full | Upload failures | Clear old files, increase storage |
| Redis unavailable | Cache misses | Check Redis container |
| High memory | Slow responses | Scale up resources |

## Storage Management

### File Upload Storage

- Default path: `uploads/`
- Configurable in `FileStorage:Path`
- Supported file types: PDF, DOC, DOCX, PPT, PPTX, images
- Max file size: Configurable

### Storage Maintenance

- Monitor disk usage regularly
- Archive old uploads
- Clean up temporary files
- Set up log rotation

## Log Management

### Log Locations

- Console: stdout (Docker)
- JSON logs: `logs/sms-{date}.json`
- Text logs: `logs/sms-{date}.txt`

### Log Rotation

Logs are rotated daily. Configure retention period in Serilog configuration.

### Monitoring Logs

Use `docker compose logs -f api` to view live logs.

## Database Maintenance

### Regular Maintenance Tasks

- **Vacuum**: Run periodically to reclaim storage
- **Analyze**: Update statistics for query optimizer
- **Reindex**: Rebuild indexes for performance
- **Backup**: Regular full backups

### Monitoring Database Performance

- Check connection pool usage
- Monitor query performance
- Review slow query logs
- Check index usage

## Software Updates

### Update Process

1. Backup database
2. Pull latest code: `git pull`
3. Rebuild: `docker compose build`
4. Apply migrations: `docker compose exec api dotnet SMS.API.dll migrate-database`
5. Restart services: `docker compose up -d`

### Rollback Process

1. Stop services: `docker compose down`
2. Revert code: `git checkout <previous-tag>`
3. Restore database from backup
4. Restart services: `docker compose up -d`

## Frequently Asked Questions

**Q: How do I reset the admin password?**
A: Run the seed command again or use the password reset flow.

**Q: How do I add a new administrator?**
A: An existing admin can create new users with the Administrator role.

**Q: How do I update the JWT secret?**
A: Update `JWT_SECRET` environment variable and restart the API. All existing tokens will be invalidated.

## Related Documentation

- [Administrator User Guide](../07-Administrator-Guide/README.md)
- [Database Guide](../13-Database/README.md)
- [Backup and Recovery Guide](../14-Backup-and-Recovery/README.md)
- [Security Guide](../12-Security/README.md)
- [Maintenance Guide](../15-Maintenance/README.md)
- [Troubleshooting Guide](../16-Troubleshooting/README.md)
