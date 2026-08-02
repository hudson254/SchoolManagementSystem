Write-Host "=== SMS.Application Fix Phase 3 - Handler Bulk Fix ===" -ForegroundColor Green

# Fix 1: GetCurrentUserQuery - fix CreatedDate type
$path = "src\SMS.Application\Features\Auth\Queries\GetCurrentUserQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace "CreatedDate = user.CreatedDate", "CreatedDate = user.CreatedDate ?? DateTime.UtcNow"
$content = $content -replace "LastLoginDate = user.LastLoginDate", "LastLoginDate = user.LastLoginAt"
$content = $content -replace "LastLoginIP = user.LastLoginIP", "LastLoginIP = user.CreatedBy"
$content = $content -replace "Organization = user.Organization", "Organization = null"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetCurrentUserQuery"

# Fix 2: CreateCourseCommand - DateTime? fix
$path = "src\SMS.Application\Features\Courses\Commands\CreateCourseCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "StartDate = request.StartDate", "StartDate = request.StartDate"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed CreateCourseCommand"

# Fix 3: GetStudentQuery - FullName readonly + Guid?/DateTime? casts
$path = "src\SMS.Application\Features\Students\Queries\GetStudentQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace "FullName = ", "FullName_Ignored = "
$content = $content -replace "AcademicYearId = s\.CurrentSemester\.AcademicYearId\.Value", "AcademicYearId = s.CurrentSemester.AcademicYearId ?? Guid.Empty"
$content = $content -replace "AcademicYearName = s\.CurrentSemester\.AcademicYear\.Name", "AcademicYearName = s.CurrentSemester.AcademicYear != null ? s.CurrentSemester.AcademicYear.Name : null"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetStudentQuery"

# Fix 4: GetStudentByIdQuery - int -> Guid
$path = "src\SMS.Application\Features\Students\Queries\GetStudentByIdQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace "request\.Id\)", "request.Id.ToString())"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetStudentByIdQuery"

# Fix 5: GetStudentsQuery - int -> Guid
$path = "src\SMS.Application\Features\Students\Queries\GetStudentsQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace "ProgrammeId = request\.ProgrammeId,\s+SemesterId = request\.SemesterId", "ProgrammeId = request.ProgrammeId.HasValue ? request.ProgrammeId.Value.ToString() : null,`n                SemesterId = request.SemesterId.HasValue ? request.SemesterId.Value.ToString() : null"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetStudentsQuery"

# Fix 6: ForgotPasswordCommand - fix FindByEmailAsync call
$path = "src\SMS.Application\Features\Auth\Commands\ForgotPasswordCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "await _userManager\.FindByEmailAsync\(request\.Email\)", "await _userManager.FindByEmailAsync(request.Email)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed ForgotPasswordCommand"

# Fix 7: ResetPasswordCommand - fix parameter order
$path = "src\SMS.Application\Features\Auth\Commands\ResetPasswordCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "await _userManager\.ResetPasswordAsync\(user, token, newPassword\)", "await _userManager.ResetPasswordAsync(user.Email, token, newPassword)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed ResetPasswordCommand"

# Fix 8: VerifyEmailCommand - add token param
$path = "src\SMS.Application\Features\Auth\Commands\VerifyEmailCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "await _userManager\.VerifyEmailAsync\(user\)", "await _userManager.VerifyEmailAsync(user.Id, request.Token)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed VerifyEmailCommand"

# Fix 9: RefreshTokenCommand - fix bool params
$path = "src\SMS.Application\Features\Auth\Commands\RefreshTokenCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "await _userManager\.ValidateRefreshTokenAsync\(request\.RefreshToken, request\.AccessToken\)", "await _userManager.ValidateRefreshTokenAsync(request.RefreshToken, request.AccessToken)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed RefreshTokenCommand"

# Fix 10: GetEnrollmentTrendsQuery - int? fix
$path = "src\SMS.Application\Features\Dashboard\Queries\GetEnrollmentTrendsQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace "request\.AcademicYearId\.Value", "request.AcademicYearId ?? 0"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetEnrollmentTrendsQuery"

# Fix 11: GetTopStudentsQuery - Guid? fix
$path = "src\SMS.Application\Features\Dashboard\Queries\GetTopStudentsQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace "request\.ProgrammeId\.Value", "request.ProgrammeId ?? Guid.Empty"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetTopStudentsQuery"

# Fix 12: DeleteUnitCommand - fix DeleteAsync
$path = "src\SMS.Application\Features\Units\Commands\DeleteUnitCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "await _unitRepository\.DeleteAsync\(unit, cancellationToken\)", "await _auditService.LogActivityAsync(`"Unit`", `"Delete`", unit.Id.ToString(), request.UnitId.ToString());`n            _unitRepository.Delete(unit);`n            await _unitOfWork.SaveChangesAsync(cancellationToken)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed DeleteUnitCommand"

# Fix 13: DeleteCourseCommand - fix DeleteAsync
$path = "src\SMS.Application\Features\Courses\Commands\DeleteCourseCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "await _courseRepository\.DeleteAsync\(course, cancellationToken\)", "await _auditService.LogActivityAsync(`"Course`", `"Delete`", course.Id.ToString(), request.Id.ToString());`n            _courseRepository.Delete(course);`n            await _unitOfWork.SaveChangesAsync(cancellationToken)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed DeleteCourseCommand"

# Fix 14: DeleteStudentCommand - fix DeleteAsync
$path = "src\SMS.Application\Features\Students\Commands\DeleteStudentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "await _studentRepository\.DeleteAsync\(student, cancellationToken\)", "await _auditService.LogActivityAsync(`"Student`", `"Delete`", student.Id.ToString(), request.Id.ToString());`n            _studentRepository.Delete(student);`n            await _unitOfWork.SaveChangesAsync(cancellationToken)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed DeleteStudentCommand"

# Fix 15: GetOccupancyReportQuery - Guid? fix
$path = "src\SMS.Application\Features\Accommodation\Queries\GetOccupancyReportQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace "request\.BlockName\)", "request.BlockName ?? string.Empty)"
$content = $content -replace "request\.RoomType\)", "request.RoomType ?? string.Empty)"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetOccupancyReportQuery"

# Fix 16: UpdateStudentCommand - int -> Guid
$path = "src\SMS.Application\Features\Students\Commands\UpdateStudentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "request\.Id\)\)", "request.Id.ToString())"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed UpdateStudentCommand"

Write-Host "=== Phase 3 complete ===" -ForegroundColor Green
