$g = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem\src\SMS.Application\Features\Grades\Queries\GetGradesQuery.cs'
$t = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem\src\SMS.Application\Features\Students\Queries\GetStudentTranscriptQuery.cs'

$gl = [System.IO.File]::ReadAllLines($g)
$gl[61] = '.Select(g => AuthoritativeGradeMapper.MapToGradeDto(g, g.Unit, g.Student)))'
$gl[62] = '.ToList();'
[System.IO.File]::WriteAllLines($g, $gl, (New-Object System.Text.UTF8Encoding $false))

$tl = [System.IO.File]::ReadAllLines($t)
$tl[60] = '                .Where(r => !r.IsDeleted)'
$tl[61] = '                .ToList();'
$tl[107] = '                    Grades = g.OrderBy(x => x.CreatedDate).ToList();'
[System.IO.File]::WriteAllLines($t, $tl, (New-Object System.Text.UTF8Encoding $false))
Write-Host 'line fixes applied'