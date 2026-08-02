# Administrator Guide

## Overview
This guide provides administrators with the information needed to manage the School Management System.

## Access Control

### Roles
| Role | Description |
|------|-------------|
| Administrator | Full system access |
| Moderator | Administrative access with some restrictions |
| Lecturer | Teaching staff access |
| Student | Student access |
| Receptionist | Front desk access |

### Authorization Policies
| Policy | Roles |
|--------|-------|
| AdministratorAccess | Administrator |
| ModeratorAccess | Administrator, Moderator |
| LecturerAccess | Administrator, Moderator, Lecturer |
| StudentAccess | All roles |
| ReceptionistAccess | Administrator, Moderator, Receptionist |

## Audit Logs

### Viewing Audit Logs
Access the audit viewer at: `GET /api/v1/audit`

### Filtering Options
- User ID
- Action type
- Entity name
- Date range
- Success/failure status
- Pagination

### Export Options
- CSV export: `GET /api/v1/audit/export/csv`
- JSON export: `GET /api/v1/audit/export/json`

### Audit Statistics
`GET /api/v1/audit/stats` provides:
- Total events
- Successful vs failed events
- Events by action
- Events by entity
- Events by user

## Error Handling

### Common Error Codes
| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| VALIDATION_ERROR | 400 | Input validation failed |
| NOT_FOUND | 404 | Resource not found |
| UNAUTHORIZED | 401 | Authentication required |
| FORBIDDEN | 403 | Insufficient permissions |
| TOKEN_EXPIRED | 401 | Session expired |
| DATABASE_UNAVAILABLE | 500 | Database connection failed |
| SERVICE_UNAVAILABLE | 503 | Service temporarily unavailable |

### Error Response Format
```json
{
  "statusCode": 400,
  "errorCode": "VALIDATION_ERROR",
  "message": "User-friendly message",
  "correlationId": "abc-123",
  "path": "/api/v1/resource"
}
```

## Health Monitoring

### Health Check Endpoint
`GET /health` - Returns application health status

### Logging
- Application logs: Configured in appsettings
- Audit logs: Database `AuditLogs` table
- Log levels configurable per environment

## Backup and Recovery

### Automated Backups
- Interval: Configurable (default 24 hours)
- Retention: Configurable (default 30 days)
- Location: Configurable backup path

### Manual Backup
Database backup can be triggered through the backup service.

## Troubleshooting

### Common Issues
1. **Application won't start**
   - Check JWT_SECRET environment variable
   - Verify database connection string
   - Check ASPNETCORE_ENVIRONMENT setting

2. **Database connection errors**
   - Verify PostgreSQL is running
   - Check connection string
   - Verify network connectivity

3. **Authentication failures**
   - Check JWT secret configuration
   - Verify user account status
   - Check account lockout status

4. **Performance issues**
   - Check database connection pooling
   - Review slow query logs
   - Monitor application metrics

### Support
For additional support, contact the system administrator or refer to the technical documentation.
