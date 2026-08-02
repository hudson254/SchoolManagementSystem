# Repair Roadmap - School Management System

**Timeline:** 12 days  
**Team:** 2 Backend, 1 Frontend, 1 DevOps (part-time), 1 QA (part-time)

---

## Week 1: Foundation Repair

### Day 1 - Build Stability (Phase A) ✅ Milestone M1

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-09:15 | T001: Fix 50 files with missing `using` | Backend Dev 1 | 50 files updated |
| 09:15-09:30 | T002: Fix DependencyInjection.cs | Backend Dev 1 | Compiles clean |
| 09:30-10:00 | T003-T005: Create interfaces (IBaseEntity, ISoftDelete) | Backend Dev 1 | Domain compiles |
| 10:00-10:30 | T006-T009: Create exception classes | Backend Dev 1 | Application compiles |
| 10:30-11:00 | T010-T014: Create/fix DTOs | Backend Dev 1 | Application compiles |
| 11:00-11:30 | T015-T016: Create MediatR behaviors | Backend Dev 1 | Application compiles |
| 11:30-12:30 | T017-T021: Create missing entities | Backend Dev 2 | Domain compiles |
| 13:00-14:00 | T022-T023: Fix ID types (int → Guid) | Backend Dev 2 | All entities compile |
| 14:00-15:00 | T024-T026: Fix entity properties | Backend Dev 2 | Domain compiles |
| 15:00-16:00 | **Build verification** | Both | ✅ Solution builds with 0 errors |

### Day 2 - Backend Repair (Phase B) ✅ Milestone M2

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-09:30 | T027-T028: Fix Program.cs imports and DI | Backend Dev 1 | API compiles |
| 09:30-10:00 | T029: Create LocalFileStorageService | Backend Dev 1 | Infrastructure compiles |
| 10:00-11:00 | T030: Create ExceptionHandlingMiddleware | Backend Dev 1 | API compiles |
| 11:00-11:30 | T031: Create TenantResolutionMiddleware | Backend Dev 1 | API compiles |
| 11:30-12:30 | T032: Implement OnModelCreating | Backend Dev 2 | Persistence compiles |
| 13:00-14:00 | T033: Implement SaveChangesAsync | Backend Dev 2 | Persistence compiles |
| 14:00-14:30 | T034: Add DbSet declarations | Backend Dev 2 | Persistence compiles |
| 14:30-16:00 | **Integration verification** | Both | ✅ All projects compile, DI chain verified |

### Day 3 - Database Repair (Phase C) ✅ Milestone M3

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-09:30 | T035: Delete old migration, create fresh | Backend Dev 2 | Migration generated |
| 09:30-10:00 | Add seed data for roles/permissions/admin | Backend Dev 2 | Seed script ready |
| 10:00-11:00 | Configure Row-Level Security | Backend Dev 2 | RLS policies created |
| 11:00-12:00 | Run migrations against local PostgreSQL | Backend Dev 2 | ✅ Database created |
| 13:00-14:00 | Verify seed data loaded | Both | ✅ Seed data verified |
| 14:00-16:00 | **Database verification** | Both | ✅ All tables, relationships, indexes verified |

---

## Week 1: API & Security

### Day 4 - API Repair Part 1 (Phase D) ✅ Milestone M4

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | T039: Create AuthController | Backend Dev 1 | Auth endpoints work |
| 10:00-11:30 | T040: Create StudentsController | Backend Dev 1 | Student endpoints work |
| 11:30-12:30 | T041: Create CoursesController | Backend Dev 2 | Course endpoints work |
| 13:00-14:00 | T042: Create UnitsController | Backend Dev 2 | Unit endpoints work |
| 14:00-15:00 | T046-T047: Create appsettings | Backend Dev 1 | Config loaded |
| 15:00-16:00 | T048-T049: Response envelope + versioning | Backend Dev 1 | API responses consistent |

### Day 5 - API Repair Part 2 + Security Start (Phase D + F)

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | T043: Create AccommodationController | Backend Dev 1 | Accommodation endpoints work |
| 10:00-11:00 | T044: Create AssignmentsController | Backend Dev 1 | Assignment endpoints work |
| 11:00-12:00 | T045: Create DashboardController | Backend Dev 1 | Dashboard endpoints work |
| 13:00-13:30 | T036: Fix CORS policy | Backend Dev 2 | CORS restricted |
| 13:30-14:00 | T037: Remove hardcoded password | Backend Dev 2 | Password generated |
| 14:00-14:30 | T038: Fix AutoMapper vulnerability | Backend Dev 2 | Package updated |
| 14:30-15:00 | T053: Add password policy validation | Backend Dev 2 | Passwords validated |
| 15:00-16:00 | **API verification** | Both | ✅ All endpoints tested |

### Day 6 - Security Hardening (Phase F)

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | T054: Add security headers middleware | Backend Dev 2 | Headers present |
| 10:00-10:30 | T055: Add rate limiting | Backend Dev 2 | Rate limiting active |
| 10:30-11:00 | T066: Add CSRF protection | Backend Dev 2 | CSRF configured |
| 11:00-11:30 | T067: JWT secret protection | Backend Dev 2 | Secret secured |
| 11:30-12:00 | T068: Add input sanitization | Backend Dev 2 | Sanitization active |
| 13:00-16:00 | **Security validation** | Both | ✅ OWASP scan passes |

---

## Week 2: Frontend, Testing & Deployment

### Day 7 - Frontend Repair (Phase E) ✅ Milestone M5

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | T050: Audit frontend project | Frontend Dev | Audit report |
| 10:00-12:00 | T051: Create API client library | Frontend Dev | API client generated |
| 13:00-16:00 | T052: Fix component structure | Frontend Dev | ✅ Frontend builds |

### Day 8 - Performance + Testing Start (Phase G + H)

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | T061-T065: Performance improvements | Backend Dev 2 | Performance improved |
| 10:00-12:00 | T056: Fix unit tests | Backend Dev 1 | Unit tests pass |
| 13:00-16:00 | T056 continued + T057: Fix integration tests | Backend Dev 1 | Integration tests pass |

### Day 9 - Testing Continued (Phase H)

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-12:00 | T058: Fix API tests | QA Engineer | API tests pass |
| 13:00-15:00 | T073: Add missing test coverage | QA Engineer | Coverage >70% |
| 15:00-16:00 | **Test verification** | All | ✅ All test suites pass |

### Day 10 - Deployment + Documentation Start (Phase I + J) ✅ Milestone M6, M7

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | T059: Fix Dockerfiles | DevOps | Docker builds succeed |
| 10:00-11:00 | T060: Fix Docker Compose | DevOps | Compose starts |
| 11:00-11:30 | T074: Create .env.example | DevOps | Env vars documented |
| 11:30-12:30 | T075: Configure SSL in Nginx | DevOps | SSL configured |
| 13:00-14:00 | T076: Verify deployment scripts | DevOps | Scripts verified |
| 14:00-15:00 | T069: Create Installation Guide | Backend Dev 1 | Guide complete |
| 15:00-16:00 | T070: Create Administrator Guide | Backend Dev 2 | Guide complete |

### Day 11 - Documentation Completion (Phase J) ✅ Milestone M8

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | T071: Create Deployment Guide | DevOps | Guide complete |
| 10:00-11:00 | T072: Create Database Schema Documentation | Backend Dev 2 | Guide complete |
| 11:00-12:00 | T077: Create Troubleshooting Guide | Both | Guide complete |
| 13:00-14:00 | T078: Update Swagger annotations | Backend Dev 1 | Swagger complete |
| 14:00-16:00 | **Documentation review** | All | ✅ All docs complete |

### Day 12 - Final Verification & Production Readiness ✅ Milestone M9, M10

| Time | Task | Owner | Verification |
|------|------|-------|-------------|
| 09:00-10:00 | Full solution rebuild | Both | 0 errors, 0 warnings |
| 10:00-11:00 | Full database migration test | Backend Dev 2 | Migration clean |
| 11:00-12:00 | Full API endpoint test | QA Engineer | All endpoints pass |
| 13:00-14:00 | Security scan | Both | All findings resolved |
| 14:00-15:00 | Docker deployment test (dev) | DevOps | Dev compose works |
| 15:00-16:00 | Docker deployment test (prod) | DevOps | Prod compose works |
| 16:00 | **Production Readiness Sign-off** | All | ✅ System ready |

---

## Parallel Work Streams

```
Week 1:  Backend Dev 1 ──▶ Phase A ──▶ Phase B ──▶ Phase D ──▶ Phase F
         Backend Dev 2 ──▶ Phase A ──▶ Phase B ──▶ Phase C ──▶ Phase D

Week 2:  Frontend Dev ───▶ Phase E
         Backend Dev 1 ──▶ Phase H ──▶ Phase J
         Backend Dev 2 ──▶ Phase G ──▶ Phase H
         DevOps ──────────▶ Phase I ──▶ Phase J
         QA ──────────────▶ Phase H
```

## Critical Path Duration

| Phase | Hours | Calendar Days |
|-------|-------|---------------|
| A - Build Stability | 6 | 1 |
| B - Backend Repair | 8 | 1 |
| C - Database Repair | 6 | 1 |
| D - API Repair | 8 | 2 |
| E - Frontend Repair | 8 | 1 |
| F - Security Hardening | 6 | 2 |
| G - Performance | 4 | 1 |
| H - Testing | 8 | 3 |
| I - Deployment | 6 | 2 |
| J - Documentation | 6 | 2 |
| **Total** | **66** | **12** |

## Buffer & Contingency

- Add 20% buffer for unexpected issues: ~13 hours
- Total with buffer: **79 hours**
- Worst-case timeline: **14 days**
