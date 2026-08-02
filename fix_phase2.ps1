Write-Host "=== SMS.Application Bulk Fix Phase 2 ===" -ForegroundColor Green

# Fix 1: Room.IsOccupied - add setter
$path = "src\SMS.Domain\Entities\Room.cs"
$content = Get-Content $path -Raw
$content = $content -replace "public bool IsOccupied \{ get; \}", "public bool IsOccupied { get; set; }"
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed Room.IsOccupied"

# Fix 2: Course.Programmes - add collection nav property
$path = "src\SMS.Domain\Entities\Course.cs"
$content = Get-Content $path -Raw
if ($content -notmatch "public ICollection<Programme> Programmes") {
    $content = $content -replace "(public virtual ICollection<Enrollment> Enrollments)", 'public virtual ICollection<Programme> Programmes { get; set; } = new List<Programme>();`n        public virtual ICollection<Enrollment> Enrollments'
}
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed Course.Programmes"

# Fix 3: ProgrammeSummaryDto with Duration/TotalCredits
$path = "src\SMS.Application\DTOs\LecturerDto.cs"
$content = Get-Content $path -Raw
if ($content -notmatch "Duration") {
    $content = $content -replace "public class ProgrammeSummaryDto", "public class ProgrammeSummaryDto`n    {`n        public Guid Id { get; set; }`n        public string Name { get; set; } = string.Empty;`n        public string Code { get; set; } = string.Empty;`n        public string? Description { get; set; }`n        public int Duration { get; set; }`n        public int TotalCredits { get; set; }`n        public DateTime StartDate { get; set; }`n        public DateTime EndDate { get; set; }`n        public bool IsActive { get; set; }"
}
Set-Content $path -Value $content -NoNewline
Write-Host "Fixed ProgrammeSummaryDto"

Write-Host "=== Phase 2 complete ===" -ForegroundColor Green
