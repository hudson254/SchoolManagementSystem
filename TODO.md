# Repair TODO — Current Session

## Progress Tracking

### Completed this session
- [x] RISK-16: React beta -> stable (package.json)
- [x] RISK-17: Source maps disabled in production (vite.config.ts)
- [x] RISK-20: Grafana default admin password replaced (docker-compose.prod.yml)
- [x] RISK-22: FileStorageService path traversal hardened

### Remaining items
- [ ] RISK-15: Replace deprecated Mvc.Versioning with ApiExplorer package
- [ ] RISK-18: Add DB indexes for AuditLogs, Enrollments, Grades, Notifications, LoginHistory
- [ ] RISK-19: Frontend test missing (add at least one vitest test)
- [ ] RISK-23: Remove unused AutoMapper
- [ ] RISK-24: Remove controller stubs + duplicate BaseApiController
- [ ] RISK-26: Nginx missing /uploads/ location
- [ ] RISK-27: LoginHistory not persisted on login
- [ ] RISK-13/14: SMS/Email stubs (partially examined - SmsService already improved)
- [ ] RISK-21: Admin password recovery (HIGH)

### Verification
- [ ] dotnet build (0 errors)
- [ ] dotnet test all projects
- [ ] npm run build frontend
- [ ] Update REPAIR_PROGRESS.md Live Status Table

