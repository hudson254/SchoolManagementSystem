# Repair TODO — Remaining Risk Items

## Backend (MEDIUM security/config)
- [x] RISK-22: Harden FileStorageService against path traversal
- [x] RISK-20: Remove Grafana admin123 default in docker-compose.prod.yml
- [x] RISK-15: Replace deprecated Microsoft.AspNetCore.Mvc.Versioning
- [x] RISK-18: Add EF indexes (AuditLogs, Enrollments, Grades, Notifications, LoginHistory)

## Frontend (MEDIUM)
- [x] RISK-16: Change react/react-dom from beta to stable
- [x] RISK-17: Disable sourcemaps in production build
- [x] RISK-19: Add at least one frontend test

## Low / Cleanup
- [x] RISK-23: Remove unused AutoMapper package reference
- [x] RISK-24: Delete _ControllerStubs.cs + duplicate root BaseApiController.cs
- [x] RISK-26: Add /uploads/ proxy to nginx-frontend.conf
- [x] RISK-27: Persist LoginHistory on login

## HIGH
- [x] RISK-21: Document admin-mediated password recovery (verify works on LAN)

## Deferred (separate session)
- [x] BRANDING: logo.png as unified branding
- [x] PWA: Progressive Web App support

## Verification
- [x] dotnet build SchoolManagementSystem.sln → 0 errors
- [x] dotnet test tests\SMS.UnitTests (94/94)
- [x] dotnet test tests\SMS.ApiTests (35/35)
- [x] dotnet test tests\SMS.IntegrationTests (21/21)
- [x] frontend npm test -- --run (7/7)
- [x] Update REPAIR_PROGRESS.md
