# SMS.Application Repair Plan — Remaining Errors

## Root Cause Analysis

All remaining ~126 errors in SMS.Application fall into 7 systematic categories:

### Category 1: DTO Missing Properties (~15 errors)
**Files**: `GetUnitQuery.cs`, `GetCurrentUserQuery.cs`, `GetCourseQuery.cs`, `GetStudentQuery.cs`
- `UnitDetailsDto` missing `AssessmentMethods`, `RecommendedTextbooks`, `UpdatedDate`
- `UserProfileDto` missing `Organization`, `LastLoginDate`, `LastLoginIP`, `CreatedDate`, `TenantId`
- `ProgrammeSummaryDto` missing `Duration`, `TotalCredits`
- `StudentDto.FullName` is read-only (computed) but handler tries to set it
- `CourseDto.StartDate` is `DateTime?` but handler assigns `DateTime`

**Fix**: Add missing DTO properties OR update handlers to use existing properties

### Category 2: Repository Method Signature Mismatches (~25 errors)
**Files**: `GetAssignmentsQuery.cs`, `GetCoursesQuery.cs`, `GetStudentsQuery.cs`, `GradeAssignmentCommand.cs`, `SubmitAssignmentCommand.cs`, various dashboard queries
- Repository methods being called with wrong number of parameters (e.g. `GetAssignmentsAsync` called with 11 args but interface has 7)
- `Update(T entity)` method missing from some repository interfaces
- `LogAsync` called with 5 args but defined with 4

**Fix**: Realign handler calls to match repository interface signatures

### Category 3: int→Guid Conversion (~12 errors)
**Files**: `CreateStudentCommand.cs`, `UpdateStudentCommand.cs`, `GetStudentByIdQuery.cs`, `GetStudentsQuery.cs`, `GetUpcomingEventsQuery.cs`
- Handlers still passing `int` values where entities now expect `Guid`

**Fix**: Use `Guid.NewGuid()` or mapping where `int` was used

### Category 4: DateTime?→DateTime Casting (~8 errors)
**Files**: `CreateCourseCommand.cs`, `UpdateCourseCommand.cs`, `GetCoursesQuery.cs`, `GetCourseQuery.cs`, `GetStudentQuery.cs`
- Handlers assign `DateTime?` values (from entity) to `DateTime` DTO properties

**Fix**: Use null-coalescing `?? default` or make DTO properties `DateTime?`

### Category 5: Service Method Signatures (~8 errors)
**Files**: `ForgotPasswordCommand.cs`, `ResetPasswordCommand.cs`, `RefreshTokenCommand.cs`, `VerifyEmailCommand.cs`
- `ForgotPasswordAsync` expects `User` not `string`
- `ResetPasswordAsync` missing `newPassword` param
- `RefreshTokenCommand` passing `bool` instead of `string`
- `VerifyEmailAsync` missing `token` param

**Fix**: Update calls to match `IUserManagerService` interface

### Category 6: PagedResult Property Names (~6 errors)
**Files**: `GetAssignmentsQuery.cs`, `GetCoursesQuery.cs`
- `PagedResult<T>.Page` doesn't exist (use `.PageNumber` or `.CurrentPage`)
- `PagedResult<T>.TotalPages` is read-only

**Fix**: Use correct property names

### Category 7: Miscellaneous (~10 errors)
- `ValidationBehavior.cs` ambiguous `ValidationException` reference
- `EnrollStudentCommand.cs` references `StudentEnrollment` entity (doesn't exist)
- `GetCourseQuery.cs` references `Course.Programmes` (doesn't exist on entity)
- `Dashboard query` referencing `object.ProgrammeName`
- `Room.IsOccupied` is read-only
- `TransferRoomCommand` trying to set it
- `GradeAssignmentCommand` assigning `DateTime` to `string` and `int?` to `decimal`

---

## Repair Strategy

**Proceed in order:**

1. Fix DTOs → add missing properties (fixes ~15 errors)
2. Fix repository interfaces → add missing methods (fixes ~25 errors)
3. Fix all handlers in a batch → realign method calls (fixes ~70 errors)
4. Fix remaining edge cases (~16 errors)

### Estimated effort: 30-45 minutes of code edits across ~35 files

---

## Do you want me to proceed with this repair plan?

If yes, I will systematically fix all remaining errors in SMS.Application and deliver a 0-error build.
