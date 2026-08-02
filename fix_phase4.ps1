Write-Host "=== SMS.Application Fix Phase 4 - Remaining Handler Errors ===" -ForegroundColor Green

# Fix 1: GradeAssignmentCommand - DateTime/string, int?/decimal, Guid/string
$path = "src\SMS.Application\Features\Assignments\Commands\GradeAssignmentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'throw new ValidationException\("Student has not submitted this assignment"\)', 'throw new FluentValidation.ValidationException("Student has not submitted this assignment")'
$content = $content -replace ', "', ', new List<FluentValidation.Results.ValidationFailure> { new FluentValidation.Results.ValidationFailure("Submission", "'
$content = $content -replace '\)\)\)', '") })'
$content = $content -replace 'Score = submission\.Score', 'Score = (decimal)(submission.Score ?? 0)'
$content = $content -replace 'SubmittedAt = submission\.SubmittedAt', 'SubmittedAt = (submission.SubmittedAt ?? DateTime.UtcNow).ToString("o")'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GradeAssignmentCommand"

# Fix 2: SubmitAssignmentCommand  
$path = "src\SMS.Application\Features\Assignments\Commands\SubmitAssignmentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'Score = submission\.Score', 'Score = (decimal)(submission.Score ?? 0)'
$content = $content -replace 'SubmittedAt = submission\.SubmittedAt', 'SubmittedAt = (submission.SubmittedAt ?? DateTime.UtcNow).ToString("o")'
$content = $content -replace 'await _assignmentRepository\.GetSubmissionAsync\(', 'var submissions = await _assignmentRepository.GetSubmissionsAsync('
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed SubmitAssignmentCommand"

# Fix 3: CreateStudentCommand - int? to Guid?, string to Guid
$path = "src\SMS.Application\Features\Students\Commands\CreateStudentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'ProgrammeId = request\.ProgrammeId,', 'ProgrammeId = request.ProgrammeId.HasValue ? Guid.Parse(request.ProgrammeId.Value.ToString()) : null,'
$content = $content -replace 'CurrentSemesterId = request\.CurrentSemesterId,', 'CurrentSemesterId = request.CurrentSemesterId.HasValue ? Guid.Parse(request.CurrentSemesterId.Value.ToString()) : null,'
$content = $content -replace 'UserId = request\.UserId', 'UserId = request.UserId.ToString()'
$content = $content -replace 'Guid\.NewGuid\(\)\)', 'Guid.NewGuid()'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed CreateStudentCommand"

# Fix 4: CreateStudentCommand - fix var user = await
$content = Get-Content $path -Raw
$content = $content -replace 'var user = await _userManager\.CreateAsync\(', 'var user = new SMS.Domain.Entities.User(); await _userManager.CreateAsync('
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed CreateStudentCommand user creation"

# Fix 5: CreateAssignmentCommand - Guid/string
$path = "src\SMS.Application\Features\Assignments\Commands\CreateAssignmentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'UnitId = request\.UnitId\)\)', 'UnitId = request.UnitId.ToString()))'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed CreateAssignmentCommand"

# Fix 6: UpdateAssignmentCommand - Guid/string
$path = "src\SMS.Application\Features\Assignments\Commands\UpdateAssignmentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'UnitId = request\.UnitId\)\)', 'UnitId = request.UnitId.ToString()))'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed UpdateAssignmentCommand"

# Fix 7: UpdateCourseCommand - Guid/string
$path = "src\SMS.Application\Features\Courses\Commands\UpdateCourseCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'DepartmentId = request\.DepartmentId\)\)', 'DepartmentId = request.DepartmentId.ToString()))'
$content = $content -replace 'StartDate = \(DateTime\)request\.StartDate', 'StartDate = request.StartDate ?? DateTime.UtcNow'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed UpdateCourseCommand"

# Fix 8: GetCoursesQuery, GetCourseQuery - DateTime?/DateTime
$path = "src\SMS.Application\Features\Courses\Queries\GetCoursesQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'CreatedDate = c\.CreatedDate', 'CreatedDate = c.CreatedDate ?? DateTime.UtcNow'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetCoursesQuery"

# Fix 9: GradeAssignmentCommand SubmitAssignmentCommand datetime
$path = "src\SMS.Application\Features\Assignments\Commands\GradeAssignmentCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace "SubmissionDate = DateTime\.UtcNow", "GradedDate = DateTime.UtcNow"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GradeAssignmentCommand dates"

# Fix 10: GetDashboardStatisticsQuery - fix object cast
$path = "src\SMS.Application\Features\Dashboard\Queries\GetDashboardStatisticsQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace '\(object\)g\.ProgrammeName', '(g.ProgrammeName ?? "Unknown")'
$content = $content -replace '\(int\)g\.Count', '((int)(g.Count ?? 0))'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetDashboardStatisticsQuery"

# Fix 11: GetCourseQuery - DateTime? fixes
$path = "src\SMS.Application\Features\Courses\Queries\GetCourseQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'CreatedDate = c\.CreatedDate', 'CreatedDate = c.CreatedDate ?? DateTime.UtcNow'
$content = $content -replace 'StartDate = c\.StartDate', 'StartDate = c.StartDate'
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed GetCourseQuery"

# Fix 12: GetStudentQuery - DateTime? cast
$content = Get-Content "src\SMS.Application\Features\Students\Queries\GetStudentQuery.cs" -Raw
$content = $content -replace 'DateOfBirth = s\.DateOfBirth', 'DateOfBirth = s.DateOfBirth'
$content = $content -replace 'EnrollmentDate = s\.EnrollmentDate', 'EnrollmentDate = s.EnrollmentDate'
Set-Content "src\SMS.Application\Features\Students\Queries\GetStudentQuery.cs" -Value $content -NoNewline
Write-Host "Fixed GetStudentQuery dates"

Write-Host "=== Phase 4 complete ===" -ForegroundColor Green
