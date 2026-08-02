$ErrorActionPreference = 'SilentlyContinue'
$root = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem'
$out = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem\usage_results.txt'

$results = @()

$results += "=== .AccommodationAssignment (singular nav) usages ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -SimpleMatch '.AccommodationAssignment ' | ForEach-Object { $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

$results += ""
$results += "=== .AccommodationAssignments (collection) usages ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -SimpleMatch 'AccommodationAssignments' | ForEach-Object { $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

$results += ""
$results += "=== Programme.Students nav usages ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -SimpleMatch '.Students' | ForEach-Object { $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

$results += ""
$results += "=== IEmailService / SendEmail usages ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern 'IEmailService|SendEmail|EmailService' | ForEach-Object { $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

$results += ""
$results += "=== SMTP config in json ==="
Get-ChildItem -Path $root -Recurse -Include *.json -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -SimpleMatch '"SMTP"' | ForEach-Object { $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

$results += ""
$results += "=== UserRole usages ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern 'UserRole|userRole|\.UserRoles' | ForEach-Object { $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

$results | Set-Content -Path $out -Encoding UTF8
Write-Output "Done. Results written to $out"

