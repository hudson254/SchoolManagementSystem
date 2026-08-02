$ErrorActionPreference = 'SilentlyContinue'
$root = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem'
$out = 'c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem\stub_scan_results.txt'

$results = @()

$patterns = @(
    'NotImplementedException',
    'TODO',
    'FIXME',
    'HACK',
    'throw new Exception',
    'throw new ApplicationException',
    'return default',
    'return null',
    'Task.CompletedTask',
    'Task.FromResult(default',
    '=> null',
    'PLACEHOLDER',
    'Demo',
    'dummy',
    'mock business',
    'NotSupportedException'
)

foreach ($p in $patterns) {
    $results += "==================== PATTERN: $p ===================="
    Get-ChildItem -Path $root\src -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern ([regex]::Escape($p)) | ForEach-Object {
        $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim()
    }
}

$results += ""
$results += "==================== TESTS TODO/FIXME ===================="
foreach ($p in @('NotImplementedException', 'TODO', 'FIXME')) {
    Get-ChildItem -Path $root\tests -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern ([regex]::Escape($p)) | ForEach-Object {
        $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim()
    }
}

$results += ""
$results += "==================== FRONTEND TODO/PLACEHOLDER ===================="
Get-ChildItem -Path $root\frontend -Recurse -Include *.ts, *.tsx, *.js, *.jsx, *.css -File | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|dist|build)\\' } | Select-String -Pattern 'TODO|FIXME|NotImplemented|placeholder|coming soon|dummy|lorem' | ForEach-Object {
    $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim()
}

$results += ""
$results += "==================== SCRIPTS/CI TODO ===================="
Get-ChildItem -Path $root\scripts, $root\docker -Recurse -Include *.sh, *.ps1, *.yml, *.yaml, *.conf -File | Select-String -Pattern 'TODO|FIXME|NotImplemented|placeholder' | ForEach-Object {
    $results += $_.Path.Replace($root, '') + ':' + $_.LineNumber + ': ' + $_.Line.Trim()
}

$results | Set-Content -Path $out -Encoding UTF8
Write-Output "Total lines: $($results.Count)"

