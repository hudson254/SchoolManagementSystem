$ErrorActionPreference = 'SilentlyContinue'
$root = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem'

Write-Output "=== UserRole usages ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern '\.UserRoles|UserRole>|new UserRole|userRole\.Role|userRole\.User|\.Role\b.*UserRole' | ForEach-Object { $_.Path.Replace($root,'') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

Write-Output ""
Write-Output "=== Student.AccommodationAssignments collection usage ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern '\.AccommodationAssignments' | ForEach-Object { $_.Path.Replace($root,'') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

Write-Output ""
Write-Output "=== Programme.Students usage ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern '\.Students' | ForEach-Object { $_.Path.Replace($root,'') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

Write-Output ""
Write-Output "=== IEmailService usage ==="
Get-ChildItem -Path $root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern 'IEmailService|SendEmail' | ForEach-Object { $_.Path.Replace($root,'') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

Write-Output ""
Write-Output "=== SMTP/EmailOptions references ==="
Get-ChildItem -Path $root -Recurse -Include *.json,*.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern 'Smtp|SMTP|EmailOptions|"SMTP"' | ForEach-Object { $_.Path.Replace($root,'') + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }

