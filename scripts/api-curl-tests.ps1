# ============================================================
# SMS API Manual Endpoint Testing via curl
# Tests: Assign Lecturer to House, Get Lecturer Assignment, Vacate House
#
# Prerequisites:
#   - API running at http://localhost:5000 (Development) or http://localhost:5001
#   - Valid admin credentials (default: admin@school.com / Admin123!)
#   - Valid house, lecturer, and semester IDs in the database
# ============================================================

$ErrorActionPreference = "Continue"

# Configuration
$baseUrl = "http://localhost:5000"
$cookieJar = "$env:TEMP\sms-cookies.txt"
$tenantHeader = "X-Tenant-Id: default"
$adminEmail = "admin@school.com"
$adminPassword = "Admin123!"

# Clean up old cookie files
Remove-Item -Path $cookieJar -ErrorAction SilentlyContinue

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " SMS API Manual Testing - Accommodation Endpoints" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# Helper: Extract access token from Set-Cookie header
# ============================================================
function Extract-AccessToken {
    param([string]$ResponseHeaders)
    $cookieLine = $ResponseHeaders | Select-String 'access_token=([^;]+)' | Select-Object -First 1
    if ($cookieLine -and $cookieLine.Matches.Count -gt 0) {
        return $cookieLine.Matches[0].Groups[1].Value
    }
    return ""
}

# ============================================================
# Step 1: Health Check
# ============================================================
Write-Host "--- Step 1: API Health Check ---" -ForegroundColor Yellow
$healthStatus = curl.exe -s -o NUL -w "%{http_code}" "$baseUrl/health"
Write-Host "  GET $baseUrl/health"
Write-Host "  Response: HTTP $healthStatus"
Write-Host ""

# ============================================================
# Step 2: Login & obtain access token
# ============================================================
Write-Host "--- Step 2: Login as Admin ---" -ForegroundColor Yellow
$loginBody = @{ email = $adminEmail; password = $adminPassword; rememberMe = $true } | ConvertTo-Json
$loginResponse = curl.exe -s -i -X POST `
    -H "Content-Type: application/json" `
    -H $tenantHeader `
    -d $loginBody `
    "$baseUrl/api/v1/auth/login" 2>&1

Write-Host "  POST $baseUrl/api/v1/auth/login"
Write-Host "  Body: $loginBody"

$accessToken = Extract-AccessToken -ResponseHeaders $loginResponse
if ([string]::IsNullOrEmpty($accessToken)) {
    Write-Host "  ERROR: Login failed. Full response:" -ForegroundColor Red
    Write-Host "  $loginResponse"
    Write-Host ""
    Write-Host "  NOTE: The database connection may be unavailable or" -ForegroundColor Yellow
    Write-Host "  credentials may have changed. Update adminEmail/adminPassword." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
Write-Host "  Login successful! Access token obtained (${$accessToken.Length} chars)."
Write-Host ""

# ============================================================
# Step 3: Test 1 - Assign Lecturer to House
# ============================================================
Write-Host "--- Step 3: TEST 1 - Assign Lecturer to House ---" -ForegroundColor Yellow
Write-Host "  POST $baseUrl/api/v1/accommodation/houses/{houseId}/assign"
Write-Host ""

# Discover available houses
Write-Host "  Discovering available houses..."
$housesResponse = curl.exe -s -H "Authorization: Bearer $accessToken" -H $tenantHeader "$baseUrl/api/v1/accommodation/houses/available"
$houses = $housesResponse | ConvertFrom-Json

if ($houses -and $houses.Count -gt 0) {
    $house = $houses | Select-Object -First 1
    $houseId = $house.id
    Write-Host "  Using house: $($house.houseNumber) (ID: $houseId)"
}
else {
    Write-Host "  WARNING: No available houses found. Response:" -ForegroundColor Yellow
    Write-Host "  $housesResponse"
    $houseId = Read-Host "  Enter a house ID to use (or leave blank to use placeholder)"
    if ([string]::IsNullOrEmpty($houseId)) { $houseId = "11111111-1111-1111-1111-111111111111" }
}

# Discover lecturers
Write-Host "  Discovering lecturers..."
$lecturersResponse = curl.exe -s -H "Authorization: Bearer $accessToken" -H $tenantHeader "$baseUrl/api/v1/lecturers"
$lecturers = $lecturersResponse | ConvertFrom-Json

if ($lecturers -and $lecturers.Count -gt 0) {
    $lecturer = $lecturers | Select-Object -First 1
    $lecturerId = $lecturer.id
    Write-Host "  Using lecturer: $($lecturer.firstName) $($lecturer.lastName) (ID: $lecturerId)"
}
else {
    Write-Host "  WARNING: No lecturers found. Response:" -ForegroundColor Yellow
    Write-Host "  $lecturersResponse"
    $lecturerId = Read-Host "  Enter a lecturer ID to use (or leave blank to use placeholder)"
    if ([string]::IsNullOrEmpty($lecturerId)) { $lecturerId = "22222222-2222-2222-2222-222222222222" }
}

# Discover semesters
Write-Host "  Discovering semesters..."
$semestersResponse = curl.exe -s -H "Authorization: Bearer $accessToken" -H $tenantHeader "$baseUrl/api/v1/semesters"
$semesters = $semestersResponse | ConvertFrom-Json

if ($semesters -and $semesters.Count -gt 0) {
    $semester = $semesters | Select-Object -First 1
    $semesterId = $semester.id
    Write-Host "  Using semester: $($semester.name) (ID: $semesterId)"
}
else {
    Write-Host "  WARNING: No semesters found. Response:" -ForegroundColor Yellow
    Write-Host "  $semestersResponse"
    $semesterId = Read-Host "  Enter a semester ID to use (or leave blank to use placeholder)"
    if ([string]::IsNullOrEmpty($semesterId)) { $semesterId = "33333333-3333-3333-3333-333333333333" }
}

# Build assign house request
$assignBody = @{
    occupantType = "Lecturer"
    lecturerId   = $lecturerId
    semesterId   = $semesterId
    moveInDate   = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    remarks      = "Manual API test via curl"
} | ConvertTo-Json

Write-Host ""
Write-Host "  Assign Request:"
Write-Host "  $assignBody"
Write-Host ""
Write-Host "  Executing curl command..."
$assignResponse = curl.exe -s -w "`nHTTP_STATUS:%{http_code}" -X POST `
    -H "Authorization: Bearer $accessToken" `
    -H $tenantHeader `
    -H "Content-Type: application/json" `
    -d $assignBody `
    "$baseUrl/api/v1/accommodation/houses/$houseId/assign"

$assignStatus = if ($assignResponse -match "HTTP_STATUS:(\d+)") { $Matches[1] } else { "N/A" }
$assignBody2 = $assignResponse -replace "`nHTTP_STATUS:\d+", ""

Write-Host "  POST /api/v1/accommodation/houses/$houseId/assign"
Write-Host "  HTTP Status: $assignStatus"
Write-Host "  Response Body: $assignBody2"
Write-Host ""

if ($assignStatus -eq "200") {
    Write-Host "  [PASS] Lecturer successfully assigned to house!" -ForegroundColor Green
}
else {
    Write-Host "  [INFO] See response above. If the house is occupied or lecturer already assigned," -ForegroundColor Yellow
    Write-Host "  try a different house/lecturer from the discovery responses above." -ForegroundColor Yellow
}
Write-Host ""

# ============================================================
# Step 4: Test 2 - Get Lecturer Assignment
# ============================================================
Write-Host "--- Step 4: TEST 2 - Get Lecturer Assignment ---" -ForegroundColor Yellow
Write-Host "  GET $baseUrl/api/v1/accommodation/assignments/lecturer/{lecturerId}"
Write-Host ""

$getAssignmentResponse = curl.exe -s -w "`nHTTP_STATUS:%{http_code}" `
    -H "Authorization: Bearer $accessToken" `
    -H $tenantHeader `
    "$baseUrl/api/v1/accommodation/assignments/lecturer/$lecturerId"

$getStatus = if ($getAssignmentResponse -match "HTTP_STATUS:(\d+)") { $Matches[1] } else { "N/A" }
$getBody = $getAssignmentResponse -replace "`nHTTP_STATUS:\d+", ""

Write-Host "  GET /api/v1/accommodation/assignments/lecturer/$lecturerId"
Write-Host "  HTTP Status: $getStatus"
Write-Host "  Response Body: $getBody"
Write-Host ""

if ($getStatus -eq "200") {
    Write-Host "  [PASS] Lecturer assignment retrieved successfully!" -ForegroundColor Green
}
elseif ($getStatus -eq "404") {
    Write-Host "  [FAIL] No assignment found for this lecturer." -ForegroundColor Red
    Write-Host "  This may indicate the assign step failed or the lecturer ID is incorrect." -ForegroundColor Yellow
}
else {
    Write-Host "  [INFO] See response above." -ForegroundColor Yellow
}
Write-Host ""

# ============================================================
# Step 5: Test 3 - Vacate House
# ============================================================
Write-Host "--- Step 5: TEST 3 - Vacate House ---" -ForegroundColor Yellow
Write-Host "  POST $baseUrl/api/v1/accommodation/houses/{houseId}/vacate"
Write-Host ""

$vacateResponse = curl.exe -s -w "`nHTTP_STATUS:%{http_code}" -X POST `
    -H "Authorization: Bearer $accessToken" `
    -H $tenantHeader `
    "$baseUrl/api/v1/accommodation/houses/$houseId/vacate"

$vacateStatus = if ($vacateResponse -match "HTTP_STATUS:(\d+)") { $Matches[1] } else { "N/A" }
$vacateBody = $vacateResponse -replace "`nHTTP_STATUS:\d+", ""

Write-Host "  POST /api/v1/accommodation/houses/$houseId/vacate"
Write-Host "  HTTP Status: $vacateStatus"
Write-Host "  Response Body: $vacateBody"
Write-Host ""

if ($vacateStatus -eq "204") {
    Write-Host "  [PASS] House successfully vacated!" -ForegroundColor Green
}
else {
    Write-Host "  [INFO] See response above." -ForegroundColor Yellow
}
Write-Host ""

# ============================================================
# Summary
# ============================================================
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1. Assign Lecturer to House:    HTTP $assignStatus"
Write-Host "  2. Get Lecturer Assignment:      HTTP $getStatus"
Write-Host "  3. Vacate House:                 HTTP $vacateStatus"
Write-Host ""
Write-Host "  Test IDs Used:"
Write-Host "    House ID:    $houseId"
Write-Host "    Lecturer ID: $lecturerId"
Write-Host "    Semester ID: $semesterId"
Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Testing Complete" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
