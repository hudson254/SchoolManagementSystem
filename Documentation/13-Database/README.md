# Database Administration Guide

## Table of Contents
- [Database Overview](#database-overview)
- [Database Architecture](#database-architecture)
- [Schema Overview](#schema-overview)
- [Entity Relationships](#entity-relationships)
- [Migrations](#migrations)
- [Backups](#backups)
- [Restores](#restores)
- [Point-in-Time Recovery](#point-in-time-recovery)
- [Optimization](#optimization)
- [Maintenance](#maintenance)
- [Integrity Checks](#integrity-checks)
- [Security](#security)
- [Troubleshooting](#troubleshooting)
- [Related Documentation](#related-documentation)

---

## Database Overview

The School Management System uses **PostgreSQL 16** as its primary database with **Entity Framework Core 9.0** as the ORM. The database is named `SchoolManagementSystem` and uses a dedicated user `sms_user`.

### Key Database Configuration
| Setting | Value |
|---------|-------|
| Database Name | SchoolManagementSystem |
| Database User | sms_user |
| Default Port | 5432 (mapped to 5433 in development docker-compose; NOT exposed in production) |
| EF Core Provider | Npgsql (PostgreSQL) |
| Connection Pool | Min: 1, Max: 10 |
| Command Timeout | 60 seconds |
| Retry on Failure | 3 attempts |

---

## Database Architecture

### Multi-Tenancy with Row Level Security
The database implements **Row Level Security (RLS)** for tenant isolation:
- Every table has a `TenantId` column
- PostgreSQL RLS policies enforce tenant isolation at the database level
- Queries are automatically filtered by the current tenant context
- Prevents cross-tenant data access even if application logic fails

### Schema
The database uses the `public` schema by default. Tables are named using PascalCase matching the entity names.

---

## Schema Overview

### Core Tables

#### Users and Roles
| Table | Purpose |
|-------|---------|
| `Users` | System users (all roles) |
| `Roles` | User roles |
| `UserRoles` | User-role assignments |
| `UserClaims` | User claims |
| `RoleClaims` | Role claims |
| `LoginHistory` | User login attempts |

#### Academic Structure
| Table | Purpose |
|-------|---------|
| `Departments` | Academic departments |
| `Courses` | Academic courses |
| `Units` | Course units |
| `CourseUnits` | Course-to-unit mapping |
| `AcademicYears` | Academic calendar years |
| `Semesters` | Academic semesters |
| `Programmes` | Academic programmes |

#### People
| Table | Purpose |
|-------|---------|
| `Students` | Student profiles |
| `Lecturers` | Lecturer profiles |
| `Titles` | Academic titles (Dr., Prof., etc.) |

#### Academic Operations
| Table | Purpose |
|-------|---------|
| `Enrollments` | Student course enrollments |
| `CourseOfferings` | Course offerings per academic period |
| `CourseOfferingUnits` | Units in a course offering |
| `CourseOfferingEnrollments` | Student enrollment in offerings |
| `CourseOfferingLecturers` | Lecturer assignments to offerings |
| `UnitAllocations` | Unit-to-lecturer assignments |
| `Grades` | Student grades |
| `GradeChangeHistory` | Grade change tracking |
| `Assignments` | Assignment definitions |
| `Attendance` | Student attendance |
| `UnitResults` | Computed unit results |
| `Assessments` | Assessment definitions |
| `AssessmentTypes` | Assessment type configuration |
| `AssessmentTemplates` | Assessment templates |
| `GradingScales` | Grading scale configuration |
| `GradeBands` | Grade band definitions |
| `ModerationRecords` | Assessment moderation records |
| `AssessmentExemptions` | Student assessment exemptions |

#### Accommodation
| Table | Purpose |
|-------|---------|
| `Lanes` | Accommodation lanes |
| `Houses` | Accommodation houses |
| `Accommodations` | Accommodation units/rooms |
| `AccommodationAssignments` | Occupant assignments |

#### Certificates
| Table | Purpose |
|-------|---------|
| `Certificates` | Generated certificates |
| `CertificateTemplates` | Certificate templates |
| `CertificateAuditLogs` | Certificate audit trail |
| `DigitalSignatures` | Digital signature records |
| `CertificateRules` | Certificate eligibility rules |

#### System
| Table | Purpose |
|-------|---------|
| `AuditLogs` | System audit trail |
| `Notifications` | System notifications |
| `UploadFiles` | Uploaded file metadata |
| `ReportVerifications` | Report verification records |
| `PasswordResetRequests` | Password reset workflow |
| `Timetables` | Class schedules |
| `CalendarEvents` | Academic calendar events |

---

## Entity Relationships

### Key Relationships
```
Course 1—* CourseUnits *—1 Unit
Course 1—* CourseOfferings
CourseOffering 1—* CourseOfferingUnits *—1 Unit
CourseOffering 1—* CourseOfferingEnrollments *—1 Student
CourseOffering 1—* CourseOfferingLecturers *—1 Lecturer
Unit 1—* UnitAllocations *—1 Lecturer
Student 1—* Enrollments *—1 Course
Unit 1—* Grades *—1 Student
Department 1—* Units
Department 1—* Lecturers
```

---

## Migrations

### Applying Migrations
```bash
# Via Docker
docker compose exec api dotnet SMS.API.dll migrate-database

# Via .NET CLI
cd src/SMS.API
dotnet run -- migrate-database
```

### Creating a New Migration
```bash
cd src/SMS.Persistence
dotnet ef migrations add MigrationName
```

### Removing Last Migration
```bash
cd src/SMS.Persistence
dotnet ef migrations remove
```

### Viewing Migration Status
```bash
cd src/SMS.Persistence
dotnet ef migrations list
```

### Rolling Back
```bash
# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Rollback to empty database
dotnet ef database update 0
```

---

## Backups

### Manual Database Backup
```bash
# Custom format (recommended)
pg_dump -h localhost -U sms_user -d SchoolManagementSystem -F c -f backup.dump

# Compressed plain format
pg_dump -h localhost -U sms_user -d SchoolManagementSystem -F c -Z 9 -f backup.dump

# With Docker
docker compose exec postgres pg_dump -U sms_user -d SchoolManagementSystem -F c -f /backups/backup.dump
```

### Automated Backups
The system includes an automated backup service:
- Runs daily (interval configurable via `BACKUP_INTERVAL`)
- Retains backups for 30 days (`BACKUP_RETENTION_DAYS`)
- Backs up database and file storage
- Stores in the `backup_data` Docker volume

---DATE---

## Restores

### Restoring a Database
```bash
# Restore custom format backup
pg_restore -h localhost -U sms_user -d SchoolManagementSystem -c backup.dump

# Parallel restore (faster)
pg_restore -h localhost -U sms_user -d SchoolManagementSystem -j 4 -c backup.dump

# With Docker
docker compose exec -T postgres pg_restore -U sms_user -d SchoolManagementSystem < backup.dump
```

### Restore Checklist
1. Stop application to prevent writes during restore
2. Verify backup file integrity
3. Restore database
4. Run pending migrations if version differs
5. Verify data integrity
6. Restart application

---

## Point-in-Time Recovery

### Prerequisites
- PostgreSQL WAL archiving enabled
- Base backup available
- WAL segments since base backup

### Recovery Steps
1. Restore the base backup
2. Configure recovery settings (`recovery.conf` or `postgresql.conf`)
3. Specify recovery target time
4. Start PostgreSQL in recovery mode
5. Verify data at target time

---

## Optimization

### Index Strategy
- Index foreign key columns
- Index frequently queried columns
- Use composite indexes for multi-column queries
- Monitor index usage with `pg_stat_user_indexes`

### Performance Tuning

#### Server Settings (postgresql.conf)
```conf
# Shared buffers (25% of RAM typically)
shared_buffers = 1GB

# Work memory
work_mem = 32MB

# Connection pool settings
max_connections = 100

# WAL settings
wal_buffers = 16MB
```

#### Application Settings
- Connection pooling: Min 1, Max 10
- Command timeout: 60 seconds
- Retry on failure: 3 attempts

### Monitoring Slow Queries
```sql
-- Enable slow query logging
ALTER SYSTEM SET log_min_duration_statement = 500;

-- View recent slow queries
SELECT query, calls, total_time, mean_time
FROM pg_stat_statements
ORDER BY total_time DESC
LIMIT 10;
```

---

## Maintenance

### Routine Maintenance Tasks

#### VACUUM
```sql
-- Standard vacuum
VACUUM;

-- Vacuum with analyze
VACUUM (ANALYZE);

-- Vacuum all tables
VACUUM VERBOSE ANALYZE;
```

#### ANALYZE
```sql
-- Update statistics
ANALYZE;

-- Update statistics for specific table
ANALYZE students;
```

#### REINDEX
```sql
-- Reindex specific table
REINDEX TABLE students;

-- Reindex entire database
REINDEX DATABASE SchoolManagementSystem;
```

### Maintenance Schedule
| Frequency | Task |
|-----------|------|
| Daily | Backup verification |
| Weekly | VACUUM |
| Monthly | REINDEX, ANALYZE |
| Quarterly | Full integrity check |

---

## Integrity Checks

### Check Database Integrity
```bash
# PostgreSQL checksums
pg_checksums -c -D /var/lib/postgresql/data

# Check for corrupted indexes
REINDEX DATABASE SchoolManagementSystem;
```

### Data Consistency Checks
```sql
-- Check for orphaned records
SELECT COUNT(*) FROM enrollments e
WHERE NOT EXISTS (SELECT 1 FROM students s WHERE s.id = e.student_id);

-- Check for duplicate emails
SELECT email, COUNT(*) FROM users GROUP BY email HAVING COUNT(*) > 1;
```

---

## Security

### Database Security
- Use strong passwords (enforced by Docker Compose)
- Restrict network access to the database
- Use SSL connections in production
- Regular security updates

### Backup Security
- Encrypt sensitive backups
- Store offsite backups securely
- Limit backup access to authorized personnel

### Audit
- Enable PostgreSQL audit logging
- Monitor failed connection attempts
- Track schema changes

---

## Troubleshooting

### Common Database Issues
| Issue | Solution |
|-------|----------|
| Connection refused | Check PostgreSQL running, verify connection string |
| Connection pool exhausted | Increase max pool size, check for leaks |
| Slow queries | Run ANALYZE, add indexes, review execution plans |
| Disk full | Remove old backups, VACUUM FULL, increase storage |
| Migration failed | Check migration order, backup before migrating |

### Diagnostic Commands
```bash
# Check database size
SELECT pg_size_pretty(pg_database_size('SchoolManagementSystem'));

# Check active connections
SELECT * FROM pg_stat_activity WHERE datname = 'SchoolManagementSystem';

# Check table sizes
SELECT tablename, pg_size_pretty(pg_total_relation_size(tablename))
FROM pg_tables WHERE schemaname = 'public' ORDER BY pg_total_relation_size(tablename) DESC;
```

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Backup and Recovery](../14-Backup-and-Recovery/README.md) | Backup and restore procedures |
| [Maintenance Guide](../15-Maintenance/README.md) | Routine maintenance tasks |
| [System Administration](../06-System-Administration/README.md) | Database administration |
| [Installation Guide](../03-Installation/README.md) | Database setup |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | Database troubleshooting |
