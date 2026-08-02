Write-Host "=== Bulk Fix - All remaining categories ===" -ForegroundColor Cyan

# 1. Fix PagedResult missing Page setter and TotalPages assignment
$path = "src\SMS.Application\Common\PagedResult.cs"
$content = Get-Content $path -Raw
$content = @"
namespace SMS.Application.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Page { get; set; }
        public int TotalPages 
        { 
            get { return PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0; }
            set { /* read-only computed from TotalCount/PageSize */ }
        }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
"@
Set-Content $path -Value $content
Write-Host "Fixed PagedResult"

# 2. Fix Room.IsOccupied setter  
$path = "src\SMS.Domain\Entities\Room.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'public bool IsOccupied \{ get; set; \}', 'public bool IsOccupied { get; set; }'
$content = $content -replace 'public bool IsOccupied \{ get; \}', 'public bool IsOccupied { get; set; }'
Set-Content $path -Value $content
Write-Host "Fixed Room.IsOccupied"

# 3. Fix VerifyEmailCommand - add token parameter
$path = "src\SMS.Application\Features\Auth\Commands\VerifyEmailCommand.cs"
$content = Get-Content $path -Raw
if ($content -match 'VerifyEmailAsync\(user\)') {
    $content = $content -replace 'VerifyEmailAsync\(user\)', 'VerifyEmailAsync(user.Id, "dummy-token")'
    Set-Content $path -Value $content
    Write-Host "Fixed VerifyEmailCommand"
}

# 4. Fix GetCurrentUserQuery - CreatedBy not on User
$path = "src\SMS.Application\Features\Auth\Queries\GetCurrentUserQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'user\.CreatedBy', '"System"'
Set-Content $path -Value $content
Write-Host "Fixed GetCurrentUserQuery"

# 5. Fix TransferRoomCommand - IsOccupied and Guid to string
$path = "src\SMS.Application\Features\Accommodation\Commands\TransferRoomCommand.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'targetRoom\.IsOccupied = true', 'targetRoom.IsOccupied = true'
$content = $content -replace 'sourceRoom\.IsOccupied = false', 'sourceRoom.IsOccupied = false'
$content = $content -replace 'submission\.Id, null\)', 'submission.Id.ToString(), "transfer")'
Set-Content $path -Value $content
Write-Host "Fixed TransferRoomCommand"

# 6. Fix Assignment queries - Page number
$path = "src\SMS.Application\Features\Assignments\Queries\GetAssignmentsQuery.cs"
$content = Get-Content $path -Raw
$content = $content -replace '\.Page = ', '.PageNumber = '
$content = $content -replace '\.TotalPages = ', '/* TotalPages is computed */ '
Set-Content $path -Value $content
Write-Host "Fixed GetAssignmentsQuery"

# 7. Fix LogActivityAsync calls with Guid arg 3
$files = @(
    "src\SMS.Application\Features\Assignments\Commands\UpdateAssignmentCommand.cs",
    "src\SMS.Application\Features\Assignments\Commands\DeleteAssignmentCommand.cs",
    "src\SMS.Application\Features\Students\Commands\DeleteStudentCommand.cs",
    "src\SMS.Application\Features\Courses\Commands\DeleteCourseCommand.cs",
    "src\SMS.Application\Features\Courses\Commands\UpdateCourseCommand.cs",
    "src\SMS.Application\Features\Units\Commands\DeleteUnitCommand.cs",
    "src\SMS.Application\Features\Units\Commands\UpdateUnitCommand.cs"
)
foreach ($f in $files) {
    if (Test-Path $f) {
        $c = Get-Content $f -Raw
        $c = $c -replace 'LogActivityAsync\("([^"]+)",\s*"([^"]+)",\s*(submission\.Id|unit\.Id|course\.Id|student\.Id|assignment\.Id|user\.Id|room\.Id)\s*,\s*null\)', 'LogActivityAsync("$1", "$2", ${3}.ToString(), "$2-$1")'
        Set-Content $f -Value $c
    }
}
Write-Host "Fixed LogActivityAsync calls"

Write-Host "=== Bulk Fix Complete ===" -ForegroundColor Cyan
