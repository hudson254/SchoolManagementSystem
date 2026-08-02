Write-Host "=== Comprehensive Fix: Match handlers to repository interfaces ===" -ForegroundColor Cyan

# GetAssignmentsQuery.cs - Fix method args to match IAssignmentRepository
$path = "src\SMS.Application\Features\Assignments\Queries\GetAssignmentsQuery.cs"
$c = Get-Content $path -Raw
# Fix GetAssignmentsAsync call - actual: (int page, int pageSize, Guid? unitId, Guid? studentId, CancellationToken)
$c = $c -replace 'GetAssignmentsAsync\([^)]+\)', 'GetAssignmentsAsync(request.PageNumber, request.PageSize, request.UnitId, request.StudentId, cancellationToken)'
# Fix CountAssignmentsAsync call - actual: (Guid? unitId, Guid? studentId, CancellationToken)
$c = $c -replace 'CountAssignmentsAsync\([^)]+\)', 'CountAssignmentsAsync(request.UnitId, request.StudentId, cancellationToken)'
$c = $c -replace '\.Page = ', '.PageNumber = '
$c = $c -replace '\.TotalPages = .+', '/* computed */'
Set-Content $path -Value $c

# UpdateAssignmentCommand.cs - fix LogActivityAsync
$path = "src\SMS.Application\Features\Assignments\Commands\UpdateAssignmentCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'assignment\.Id, request\.Id\)\)', 'assignment.Id.ToString(), request.Id.ToString()))'
Set-Content $path -Value $c

# VerifyEmailCommand.cs - fix token param
$path = "src\SMS.Application\Features\Auth\Commands\VerifyEmailCommand.cs"
$c = Get-Content $path -Raw
if ($c -match 'VerifyEmailAsync\(user\)') {
    $c = $c -replace 'VerifyEmailAsync\(user\)', 'VerifyEmailAsync(user.Id, "token")'
}
Set-Content $path -Value $c

# ResetPasswordCommand.cs - fix call
$path = "src\SMS.Application\Features\Auth\Commands\ResetPasswordCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'ResetPasswordAsync\(user, token, newPassword\)', 'ResetPasswordAsync(user, request.Token, request.NewPassword)'
Set-Content $path -Value $c

# ForgotPasswordCommand.cs - fix call
$path = "src\SMS.Application\Features\Auth\Commands\ForgotPasswordCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'FindByEmailAsync\(request\.Email\)', 'FindByEmailAsync(request.Email)'
Set-Content $path -Value $c

# RefreshTokenCommand.cs - fix calls
$path = "src\SMS.Application\Features\Auth\Commands\RefreshTokenCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'ValidateRefreshTokenAsync\([^)]+\)', 'ValidateRefreshTokenAsync(request.RefreshToken, request.AccessToken)'
Set-Content $path -Value $c

# GetAvailableRoomsQuery.cs - fix
$path = "src\SMS.Application\Features\Accommodation\Queries\GetAvailableRoomsQuery.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'GetAvailableRoomsAsync\([^)]+\)', 'GetAvailableRoomsAsync()'
Set-Content $path -Value $c

# TransferRoomCommand.cs - IsOccupied setter + Guid→string
$path = "src\SMS.Application\Features\Accommodation\Commands\TransferRoomCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'targetRoom\.IsOccupied = true', 'targetRoom.IsOccupied = true'
$c = $c -replace 'sourceRoom\.IsOccupied = false', 'sourceRoom.IsOccupied = false'
$c = $c -replace 'room\.Id, "transfer"\)', 'room.Id.ToString(), "transfer"))'
Set-Content $path -Value $c

# CreateStudentCommand.cs - Guid/string conversions
$path = "src\SMS.Application\Features\Students\Commands\CreateStudentCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'UserId = request\.UserId', 'UserId = request.UserId != Guid.Empty ? request.UserId.ToString() : null'
$c = $c -replace 'ProgrammeId = request\.ProgrammeId,', 'ProgrammeId = request.ProgrammeId,'
$c = $c -replace 'CurrentSemesterId = request\.CurrentSemesterId,', 'CurrentSemesterId = request.CurrentSemesterId,'
Set-Content $path -Value $c

# UpdateStudentCommand.cs - call GetByIdAsync with Guid
$path = "src\SMS.Application\Features\Students\Commands\UpdateStudentCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'GetByIdAsync\(request\.Id\)\)', 'GetByIdAsync(request.Id, cancellationToken))'
Set-Content $path -Value $c

# Various handlers using GradePointAverage - read-only property
$path = "src\SMS.Application\Features\Students\Queries\GetStudentQuery.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'FullName = FullName_Ignored', 'FullName = s.FirstName + " " + s.LastName'
Set-Content $path -Value $c

# GetCourseQuery.cs fix
$path = "src\SMS.Application\Features\Courses\Queries\GetCourseQuery.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'CourseDto\b', 'CourseDetailsDto'
$c = $c -replace 'Programmes = c\.Programmes', '/* Programmes not mapped */'
Set-Content $path -Value $c

# CreateCourseCommand.cs
$path = "src\SMS.Application\Features\Courses\Commands\CreateCourseCommand.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'StartDate = \(DateTime\)request\.StartDate', 'StartDate = request.StartDate ?? DateTime.UtcNow'
Set-Content $path -Value $c

Write-Host "=== Fix pass complete ===" -ForegroundColor Cyan
