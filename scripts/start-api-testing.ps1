# Start the SMS API in Testing environment
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=SMS_Test;Username=postgres;Password=postgres;Minimum Pool Size=1;Maximum Pool Size=10;"

# Kill any existing dotnet processes
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/SMS.API --no-build --no-launch-profile --urls http://localhost:5001" `
    -WorkingDirectory "c:\Users\hwainaina\Desktop\my dev project\SchoolManagementSystem\SchoolManagementSystem" `
    -WindowStyle Hidden `
    -RedirectStandardOutput "api-testing3.log" `
    -RedirectStandardError "api-testing3-err.log"

Write-Host "API starting on http://localhost:5001 with Testing environment..."
