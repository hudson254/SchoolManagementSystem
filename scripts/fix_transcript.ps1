$target = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem\src\SMS.Application\Features\Students\Queries\GetStudentTranscriptQuery.cs'
$c = [System.IO.File]::ReadAllText($target)
$c = [System.Text.RegularExpressions.Regex]::Replace($c, '\.Where\(r => !r\.IsDeleted\.ToList\(\);\)\s*\.ToList\(\);', '.Where(r => !r.IsDeleted)`n                .ToList();')
$c = [System.Text.RegularExpressions.Regex]::Replace($c, 'Select\(r => r\.UnitId\.Distinct\(\)\)', 'Select(r => r.UnitId).Distinct())')
$c = [System.Text.RegularExpressions.Regex]::Replace($c, 'BuildSummary\(r, unitMap;', 'BuildSummary(r, unitMap);')
$c = [System.Text.RegularExpressions.Regex]::Replace($c, 'string\.Empty\.Trim\(\)', 'string.Empty).Trim()')
[System.IO.File]::WriteAllText($target, $c, (New-Object System.Text.UTF8Encoding $false))
Write-Host 'transcript fixes applied'