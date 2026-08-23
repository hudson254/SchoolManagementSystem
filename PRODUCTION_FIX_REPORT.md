# Production Fix Report

## 1. Executive Summary

The School Management System had critical EF Core model migration failures causing:
- `PendingModelChangesWarning` - model/schema mismatch
- `relation "course_offerings" does not exist` - missing table
- Shadow foreign keys: `UserId1`, `RoleId1`, `ProgrammeId1`, `StudentId1`, `LecturerId1`
- Rate limiting blocking health checks
- Migration silently continuing on failure

## 2. Root Causes Identified

| Issue | Root Cause |
|-------|-----------|
| UserRole.UserId1/RoleId1 | `UserRole` extends `IdentityUserRole<string>` with nav properties but no explicit FK configuration |
| Student.ProgrammeId1 | `Student.Programme` relationship used `WithMany()` without specifying inverse navigation |
| AccommodationAssignment.StudentId1/LecturerId1 | Both `Student` and `Lecturer` had competing one-to-one + one-to-many relationships using same FK |
| course_offerings missing | No explicit `CourseOffering` entity configuration in `OnModelCreating` |
| Migration failure | `Program.cs` caught and swallowed migration exceptions |
| Health endpoint 429 | Rate limiting middleware applied to all paths including `/health` |
| Data Protection warning | No persistent key storage volume configured |

## 3. Files Changed

| File | Change |
|------|--------|
| `src/SMS.Persistence/Data/ApplicationDbContext.cs` | Complete rewrite of relationship configurations |
| `src/SMS.Domain/Entities/AccommodationAssignment.cs` | Made nav properties nullable, removed conflicting relationships |
| `src/SMS.Domain/Entities/Class.cs` | Added `ITenantAwareEntity` interface |
| `src/SMS.API/Middleware/RateLimitingMiddleware.cs` | Added exempt paths for health/metrics endpoints |
| `src/SMS.API/Program.cs` | Changed migration failure from `Log.Warning` to `Log.Fatal` + throw |
| `src/SMS.Persistence/Migrations/20260823113500_FixProductionModel.cs` | New migration |

## 4. EF Core Relationship Fixes

### UserRole
- Removed explicit `DbSet<UserRole>` (inherited from `IdentityDbContext`)
- Removed `HasKey()` on derived type (causes EF Core error)
- Explicitly mapped `User` and `Role` nav properties to `UserId`/`RoleId`

### Student.Programme
- Changed from `WithMany()` to `WithMany(p => p.Students)` matching Programme navigation

### AccommodationAssignment
- Removed duplicate one-to-one + one-to-many relationships
- Only one-to-many with `Student` and `Lecturer` remains

### House
- `Occupant` and `LecturerOccupant` both use `OccupantId` FK with `SetNull`
- `OccupantType` discriminator determines which nav is valid

### CourseOffering
- Added explicit configuration with `[Table("course_offerings")]`
- Configured Course, AcademicYear, Semester relationships
- Added Units, Enrollments, Lecturers collections

## 5. Migration Changes

The new migration `FixProductionModel`:
- Drops all shadow FK relationships
- Drops `LecturerId1`, `StudentId1`, `ProgrammeId1`, `UserId1`, `RoleId1` columns
- Re-adds proper FK relationships
- Preserves all existing data

## 6. Remaining Issues/Warnings
- Pre-existing CS8618 warnings (non-nullable nav properties) - cosmetic only
- Pre-existing CS0108 hiding warnings - cosmetic only

## 7. Deployment Instructions
```bash
# Build
dotnet build src/SMS.API/SMS.API.csproj -c Release

# Apply migration
dotnet run --project src/SMS.API -- migrate-database

# Or via Docker
docker compose -f docker/docker-compose.prod.yml up -d
