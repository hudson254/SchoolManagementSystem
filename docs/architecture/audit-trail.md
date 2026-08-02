# Audit Trail Architecture

## Overview
The School Management System implements comprehensive immutable audit trails for all sensitive user actions. Audit records are append-only and cannot be modified or deleted.

## Architecture
```
User Action → Handler → AuditHelper → IAuditService → AuditLog Entity → Database
                                                          ↓
                                                  Structured Logging
```

## Audit Event Categories

### Authentication Events
| Event | Description | Data Captured |
|-------|-------------|---------------|
| Login | User login | UserId, Username, IP, Success/Failure |
| Logout | User logout | UserId, Username, IP |
| FailedLogin | Failed login attempt | Username, IP, FailureReason |
| PasswordReset | Password reset | UserId, Username, Success/Failure |
| PasswordChange | Password change | UserId, Username, Success/Failure |

### User Management Events
| Event | Description | Data Captured |
|-------|-------------|---------------|
| UserCreated | New user created | UserId, Username, CreatedBy |
| UserModified | User profile updated | UserId, Changes |
| UserDeleted | User deleted | UserId, Username, DeletedBy |
| RoleAssigned | Role assigned to user | UserId, Role, AssignedBy |
| PermissionChanged | Permission modified | RoleId, Permission, ChangedBy |

### Academic Events
| Event | Description | Data Captured |
|-------|-------------|---------------|
| StudentRegistered | New student registered | StudentId, Name, RegisteredBy |
| StudentUpdated | Student profile updated | StudentId, Changes |
| MarksEntered | Marks entered | GradeId, Student, Unit, EnteredBy |
| MarksModified | Marks modified | GradeId, OldScore, NewScore |
| GradePublished | Grades published | UnitId, UnitName, PublishedBy |

### Enrollment Events
| Event | Description | Data Captured |
|-------|-------------|---------------|
| EnrollmentCreated | Student enrolled | EnrollmentId, Student, Course |
| EnrollmentStatusChanged | Status changed | EnrollmentId, OldStatus, NewStatus |

### Administrative Events
| Event | Description | Data Captured |
|-------|-------------|---------------|
| ConfigurationChanged | Config updated | ConfigKey, OldValue, NewValue |
| ReportGenerated | Report generated | ReportType, GeneratedBy, Parameters |
| DataExported | Data exported | ExportType, ExportedBy, Details |
| DataImported | Data imported | ImportType, ImportedBy, RecordCount |
| BackupInitiated | Backup started | BackupType, InitiatedBy |
| RestorePerformed | Restore done | BackupId, PerformedBy |

## Audit Record Structure
```json
{
  "id": "guid",
  "timestamp": "2026-07-28T12:00:00Z",
  "userId": "user-id",
  "username": "john.doe",
  "userRole": "Administrator",
  "action": "UserCreated",
  "entityName": "User",
  "entityId": "entity-id",
  "oldValues": null,
  "newValues": "{\"email\":\"john@example.com\"}",
  "ipAddress": "192.168.1.1",
  "userAgent": "Mozilla/5.0...",
  "sessionId": "session-id",
  "correlationId": "correlation-id",
  "success": true,
  "failureReason": null,
  "details": "User 'john.doe' was created by admin"
}
```

## Audit Service Interface
The `IAuditService` interface provides methods for all audit event types:
- `LogAsync()` - Generic audit event
- `LogActivityAsync()` - Activity audit with entity ID
- `LogDataChangeAsync()` - Data change with old/new values
- `LogLoginAsync()` - Login event
- `LogLogoutAsync()` - Logout event
- `LogFailedLoginAsync()` - Failed login
- `LogPasswordResetAsync()` - Password reset
- `LogPasswordChangeAsync()` - Password change
- `LogSecurityEventAsync()` - Security event
- `LogPerformanceAsync()` - Performance event
- `LogErrorAsync()` - Error event

## AuditHelper Service
The `AuditHelper` class provides convenience methods for common audit scenarios:
- 30+ specialized methods for different event types
- Automatic context enrichment (IP, UserAgent, CorrelationId)
- Structured logging integration

## Audit Viewer
The `AuditController` provides administrative access to audit records:
- Paginated listing with filtering
- Search by user, action, entity, date range
- Export to CSV and JSON
- Audit statistics dashboard
- Role-based access control (Administrator only)

## Immutability
Audit records are append-only:
- No update operations allowed
- No delete operations allowed
- Database-level restrictions
- Application-level enforcement
