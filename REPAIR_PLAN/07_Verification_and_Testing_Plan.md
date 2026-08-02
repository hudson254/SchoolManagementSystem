# Verification and Testing Plan - School Management System

---

## 1. Verification Strategy

### Verification Levels
| Level | Scope | Executed By | Frequency |
|-------|-------|-------------|-----------|
| L1 - Build | Compilation success | Developer | After every change |
| L2 - Unit | Individual components | Developer | After each phase |
| L3 - Integration | Component interaction | Developer | After Phase C, D |
| L4 - API | Endpoint functionality | QA | After Phase D |
| L5 - Security | OWASP compliance | Security | After Phase F |
| L6 - Performance | Response times | DevOps | After Phase G |
| L7 - E2E | Full workflows | QA | After Phase H |
| L8 - Deployment | Production readiness | DevOps | After Phase I |

---

## 2. Phase A Verification - Build Stability

### Build Verification Checklist

| # | Check | Command/Procedure | Expected Result |
|---|-------|------------------|-----------------|
| A1 | Solution restore | `dotnet restore SchoolManagementSystem.sln` | All packages restored, 0 errors |
| A2 | Domain build | `dotnet build src/SMS.Domain/SMS.Domain.csproj` | 0 errors, 0 warnings |
| A3 | Application build | `dotnet build src/SMS.Application/SMS.Application.csproj` | 0 errors, 0 warnings |
| A4 | Infrastructure build | `dotnet build src/SMS.Infrastructure/SMS.Infrastructure.csproj` | 0 errors, 0 warnings |
| A5 | Persistence build | `dotnet build src/SMS.Persistence/SMS.Persistence.csproj` | 0 errors, 0 warnings |
| A6 | Identity build | `dotnet build src/SMS.Identity/SMS.Identity.csproj` | 0 errors, 0 warnings |
| A7 | Shared build | `dotnet build src/SMS.Shared/SMS.Shared.csproj` | 0 errors, 0 warnings |
| A8 | Multitenancy build | `dotnet build src/SMS.Multitenancy/SMS.Multitenancy.csproj` | 0 errors, 0 warnings |
| A9 | Notifications build | `dotnet build src/SMS.Notifications/SMS.Notifications.csproj` | 0 errors, 0 warnings |
| A10 | Reporting build | `dotnet build src/SMS.Reporting/SMS.Reporting.csproj` | 0 errors, 0 warnings |
| A11 | API build | `dotnet build src/SMS.API/SMS.API.csproj` | 0 errors, 0 warnings |
| A12 | Full solution build | `dotnet build SchoolManagementSystem.sln` | 0 errors, <5 warnings |
| A13 | NuGet vulnerability check | `dotnet list package --vulnerable` | 0 vulnerable packages |

### Success Criteria
- [ ] All 11 projects compile with 0 errors
- [ ] No NU1603 (version conflict) warnings
- [ ] No NU1903 (vulnerability) warnings
- [ ] No CS8802 (top-level statements) errors
- [ ] No CS0246 (type not found) errors

---

## 3. Phase B Verification - Backend

### Unit Verification

| # | Check | Procedure | Expected Result |
|---|-------|-----------|-----------------|
| B1 | Entity model consistency | Verify all entity properties match repository expectations | No missing properties |
| B2 | Interface implementation | Verify all interfaces have concrete implementations | No missing implementations |
| B3 | DI registration completeness | Verify all services are registered in DI container | No resolution failures |
| B4 | Middleware chain | Verify middleware ordering is correct | Pipeline processes requests correctly |
| B5 | Namespace correctness | Verify all using statements resolve correctly | No CS0246 errors |

### Success Criteria
- [ ] All entity properties aligned with repository expectations
- [ ] All DI registrations resolve at runtime
- [ ] Middleware pipeline configured correctly
- [ ] SoftDelete pattern implemented on all entities

---

## 4. Phase C Verification - Database

### Database Verification Checklist

| # | Check | Procedure | Expected Result |
|---|-------|-----------|-----------------|
| C1 | Migration generation | `dotnet ef migrations add InitialCreate` | Migration created successfully |
| C2 | Migration application | `dotnet ef database update` | Database created, all tables present |
| C3 | Table count | Query PostgreSQL for table count | All expected tables present |
| C4 | Foreign keys | Query PostgreSQL for FK constraints | All FKs present |
| C5 | Indexes | Query PostgreSQL for indexes | All expected indexes present |
| C6 | Seed data | Query roles, admin user | Default data present |
| C7 | RLS policies | Query PostgreSQL for RLS policies | Tenant isolation policies active |
| C8 | Connection pooling | Verify Npgsql connection string | Pooling enabled |
| C9 | Migration rollback | `dotnet ef migrations remove` | Migration can be rolled back |

### Expected Tables
- [ ] AspNetUsers
- [ ] AspNetRoles
- [ ] AspNetUserRoles
- [ ] AspNetRoleClaims
- [ ] AspNetUserClaims
- [ ] AspNetUserLogins
- [ ] AspNetUserTokens
- [ ] Students
- [ ] Courses
- [ ] Units
- [ ] Enrollments
- [ ] Departments
- [ ] Programmes
- [ ] AcademicYears
- [ ] Semesters
- [ ] Grades
- [ ] Attendances
- [ ] Assignments
- [ ] Timetables
- [ ] Lecturers
- [ ] Rooms
- [ ] Accommodations
- [ ] AccommodationAssignments
- [ ] Tenants
- [ ] AuditLogs
- [ ] Notifications
- [ ] LoginHistory
- [ ] __EFMigrationsHistory

### Success Criteria
- [ ] All expected tables created
- [ ] Foreign key relationships established
- [ ] Indexes on FK columns and tenant_id
- [ ] Seed data loaded for roles and admin
- [ ] RLS policies implemented
- [ ] Migration can be rolled back

---

## 5. Phase D Verification - API

### Endpoint Verification

| # | Endpoint | Method | Auth Required | Expected Status |
|---|----------|--------|---------------|-----------------|
| D1 | /api/v1/auth/login | POST | No | 200 |
| D2 | /api/v1/auth/register | POST | No | 201 |
| D3 | /api/v1/auth/refresh-token | POST | Yes | 200 |
| D4 | /api/v1/auth/forgot-password | POST | No | 200 |
| D5 | /api/v1/auth/reset-password | POST | No | 200 |
| D6 | /api/v1/auth/verify-email | POST | No | 200 |
| D7 | /api/v1/auth/logout | POST | Yes | 200 |
| D8 | /api/v1/students | GET | Yes | 200 |
| D9 | /api/v1/students/{id} | GET | Yes | 200 |
| D10 | /api/v1/students | POST | Yes | 201 |
| D11 | /api/v1/students/{id} | PUT | Yes | 200 |
| D12 | /api/v1/students/{id} | DELETE | Yes | 204 |
| D13 | /api/v1/students/search | GET | Yes | 200 |
| D14 | /api/v1/courses | GET | Yes | 200 |
| D15 | /api/v1/courses/{id} | GET | Yes | 200 |
| D16 | /api/v1/courses | POST | Yes | 201 |
| D17 | /api/v1/units | GET | Yes | 200 |
| D18 | /api/v1/units/{id} | GET | Yes | 200 |
| D19 | /api/v1/units | POST | Yes | 201 |
| D20 | /api/v1/accommodation/rooms | GET | Yes | 200 |
| D21 | /api/v1/accommodation/assign | POST | Yes | 201 |
| D22 | /api/v1/assignments | GET | Yes | 200 |
| D23 | /api/v1/assignments | POST | Yes | 201 |
| D24 | /api/v1/dashboard/statistics | GET | Yes | 200 |
| D25 | /api/v1/dashboard/trends | GET | Yes | 200 |
| D26 | /health | GET | No | 200 |
| D27 | /swagger | GET | No | 200 |

### API Response Format Verification

```json
{
    "success": true,
    "message": "Operation completed successfully",
    "data": { ... },
    "errors": []
}
```

### Error Response Verification

| Scenario | Expected Status | Expected Response |
|----------|-----------------|-------------------|
| Invalid input | 400 | Validation errors listed |
| Unauthenticated | 401 | Error message |
| Forbidden | 403 | Authorization error |
| Not found | 404 | Resource not found |
| Conflict | 409 | Conflict details |
| Server error | 500 | Generic error (no stack trace) |

### Success Criteria
- [ ] All endpoints return correct status codes
- [ ] All endpoints return consistent response format
- [ ] Authentication works (valid/invalid tokens)
- [ ] Authorization enforced correctly
- [ ] Swagger UI loads and displays all endpoints
- [ ] Health check returns healthy
- [ ] Error handling returns ProblemDetails format

---

## 6. Phase E Verification - Frontend

### Frontend Verification Checklist

| # | Check | Procedure | Expected Result |
|---|-------|-----------|-----------------|
| E1 | npm install | `npm install` | All packages installed |
| E2 | npm audit | `npm audit` | 0 critical vulnerabilities |
| E3 | TypeScript compile | `npx tsc --noEmit` | 0 errors |
| E4 | Lint | `npm run lint` | 0 errors |
| E5 | Build | `npm run build` | Build succeeds |
| E6 | API connectivity | Verify API calls reach backend | Responses received |
| E7 | Auth flow | Login → token storage → API calls | Auth works end-to-end |
| E8 | Responsive layout | Test at 375px, 768px, 1440px | Layout adapts |

### Success Criteria
- [ ] Frontend builds with 0 errors
- [ ] All API integrations working
- [ ] Authentication flow complete
- [ ] Responsive design functional

---

## 7. Phase F Verification - Security

### Security Testing Checklist

| # | Check | Tool/Method | Expected Result |
|---|-------|-------------|-----------------|
| F1 | CORS validation | curl with different origins | Only allowed origins succeed |
| F2 | JWT validation | tamper with token | 401 Unauthorized |
| F3 | SQL Injection | OWASP ZAP scan | No vulnerabilities |
| F4 | XSS | OWASP ZAP scan | No vulnerabilities |
| F5 | CSRF | Test state-changing requests | CSRF protection active |
| F6 | Security headers | curl -I | All required headers present |
| F7 | Rate limiting | Rapid requests | 429 Too Many Requests |
| F8 | Password policy | Test weak passwords | Validation errors |
| F9 | JWT secret strength | Check entropy | Strong secret |
| F10 | Dependency scan | `dotnet list package --vulnerable` | 0 vulnerable packages |

### Required Security Headers
- [ ] X-Content-Type-Options: nosniff
- [ ] X-Frame-Options: DENY
- [ ] X-XSS-Protection: 1; mode=block
- [ ] Content-Security-Policy
- [ ] Strict-Transport-Security: max-age=31536000
- [ ] Referrer-Policy: strict-origin-when-cross-origin
- [ ] Permissions-Policy

### Success Criteria
- [ ] OWASP ZAP scan passes with 0 high/critical findings
- [ ] All security headers present
- [ ] Rate limiting functional
- [ ] No vulnerable NuGet packages
- [ ] Password policy enforced
- [ ] CORS restricted to allowed origins

---

## 8. Phase G Verification - Performance

### Performance Benchmarks

| # | Metric | Target | Tool |
|---|--------|--------|------|
| G1 | API response time (p50) | <200ms | k6/Postman |
| G2 | API response time (p95) | <500ms | k6/Postman |
| G3 | API response time (p99) | <1000ms | k6/Postman |
| G4 | Concurrent users | 100 | k6 |
| G5 | Requests per second | 50 | k6 |
| G6 | DbContext pool hit rate | >90% | App metrics |
| G7 | Memory usage | <512MB | docker stats |
| G8 | Startup time | <10s | Manual |
| G9 | Frontend bundle size | <500KB | webpack-bundle-analyzer |
| G10 | Frontend Lighthouse score | >80 | Lighthouse |

### Success Criteria
- [ ] API responds within 200ms for p50
- [ ] Handles 50 concurrent requests
- [ ] Memory usage under 512MB
- [ ] Frontend Lighthouse score >80

---

## 9. Phase H Verification - Testing

### Test Execution Checklist

| # | Test Suite | Command | Expected Result |
|---|------------|---------|-----------------|
| H1 | Unit Tests | `dotnet test tests/SMS.UnitTests/` | All pass |
| H2 | Integration Tests | `dotnet test tests/SMS.IntegrationTests/` | All pass |
| H3 | API Tests | `dotnet test tests/SMS.ApiTests/` | All pass |
| H4 | All Tests | `dotnet test SchoolManagementSystem.sln` | All pass |
| H5 | Coverage report | `dotnet test --collect:"XPlat Code Coverage"` | >70% coverage |

### Minimum Coverage Targets

| Layer | Target Coverage |
|-------|-----------------|
| Domain | 90%+ |
| Application | 80%+ |
| Infrastructure | 70%+ |
| Persistence | 70%+ |
| API | 80%+ |

### Success Criteria
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All API tests pass
- [ ] Code coverage >70%
- [ ] No flaky tests

---

## 10. Phase I Verification - Deployment

### Deployment Verification Checklist

| # | Check | Procedure | Expected Result |
|---|-------|-----------|-----------------|
| I1 | Docker build (API) | `docker build -f docker/Dockerfile.api` | Build succeeds |
| I2 | Docker build (Frontend) | `docker build -f docker/Dockerfile.frontend` | Build succeeds |
| I3 | Docker Compose (Dev) | `docker-compose -f docker/docker-compose.dev.yml up` | All services start |
| I4 | Docker Compose (Prod) | `docker-compose -f docker/docker-compose.prod.yml up` | All services start |
| I5 | Health check | curl localhost:5000/health | Healthy |
| I6 | Database connection | Verify API can connect to PostgreSQL | Connected |
| I7 | SSL termination | curl https://localhost | HTTPS works |
| I8 | Logging | Check Serilog output | Logs written |
| I9 | Backup script | Run backup.sh | Backup created |
| I10 | Restore script | Run restore.sh | Restore succeeds |

### Success Criteria
- [ ] Docker images build successfully
- [ ] Docker Compose starts all services
- [ ] Health check returns healthy
- [ ] SSL configured and working
- [ ] Logging functional
- [ ] Backup and restore scripts work

---

## 11. Phase J Verification - Documentation

### Documentation Review Checklist

| # | Document | Key Sections | Complete |
|---|----------|--------------|----------|
| J1 | Installation Guide | Prerequisites, Setup steps, Configuration | [ ] |
| J2 | Administrator Guide | User management, System config, Monitoring | [ ] |
| J3 | Deployment Guide | Prerequisites, Docker setup, SSL, Environment vars | [ ] |
| J4 | API Documentation | All endpoints, Auth, Examples, Error codes | [ ] |
| J5 | Database Schema | ERD, Table descriptions, Migrations | [ ] |
| J6 | Troubleshooting Guide | Common issues, Solutions, Support | [ ] |

### Success Criteria
- [ ] All 6 documents created/updated
- [ ] Documents reflect actual system state
- [ ] API documentation matches implementation
- [ ] Deployment guide verified with actual deployment

---

## 12. Regression Test Suite

### Critical Regression Scenarios

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| R1 | Full auth flow | Register → Verify → Login → Refresh → Logout | All steps succeed |
| R2 | Student CRUD | Create → Read → Update → Delete | All operations work |
| R3 | Course management | Create course → Add units → Enroll students | All operations work |
| R4 | Multi-tenant isolation | Tenant A creates data → Tenant B cannot see it | Data isolated |
| R5 | Role-based access | Admin vs Lecturer vs Student permissions | Proper access control |
| R6 | Error handling | Invalid input → Unauthorized → Not found | Proper error responses |
| R7 | Pagination | List with page/size params | Correct pagination |
| R8 | Search | Search with various terms | Correct results |

### Success Criteria
- [ ] All regression scenarios pass
- [ ] Multi-tenant isolation verified
- [ ] Role-based access control verified
- [ ] Error handling consistent

---

## 13. Final Acceptance Test

### Production Readiness Verification

| # | Criterion | Verification Method | Status |
|---|-----------|---------------------|--------|
| 1 | Solution builds | `dotnet build` | [ ] |
| 2 | All tests pass | `dotnet test` | [ ] |
| 3 | Database migrated | `dotnet ef database update` | [ ] |
| 4 | API responds | curl /health | [ ] |
| 5 | Auth works | Login test | [ ] |
| 6 | Frontend builds | `npm run build` | [ ] |
| 7 | Security scan | OWASP ZAP | [ ] |
| 8 | Docker deploy | docker-compose up | [ ] |
| 9 | SSL working | curl https:// | [ ] |
| 10 | Documentation complete | Review | [ ] |

### Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Tech Lead | | | |
| QA Lead | | | |
| Security Lead | | | |
| DevOps Lead | | | |
| Product Owner | | | |
