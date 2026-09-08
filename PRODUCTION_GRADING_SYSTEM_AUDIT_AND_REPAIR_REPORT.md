# PRODUCTION GRADING SYSTEM AUDIT AND REPAIR REPORT

**System:** School Management System (SMS)
**Server:** 192.168.110.161 (sms-server)
**Audit date:** 8 September 2026

---

## 1. Executive Summary

**Pre-repair state:** The production Assessment, Weighting, Grading, Moderation,
Result Publication, Transcript and Certificate Eligibility system was
**completely non-functional**. Although the database schema, entities,
controllers, DTOs and repositories existed, **not a single backend handler was
registered** for the 32 requests dispatched by `AssessmentController`. Every one
of the 25 assessed API endpoints returned **HTTP 500** with `No service for type
'MediatR.IRequestHandler<...>' has been registered`, and all grading tables were
empty (0 assessment types, 0 grading scales, 0 marks, 0 results). The only
reachable grade-related API (`/api/v1/grades`) used **hard-coded,
non-configurable grade boundaries** (`>=75 => "A-"`, 13 bands) that conflict with
the institutional scale. Two duplicate `AssessmentEngine` implementations existed
and were both registered. The unit test suite did **not compile**.

**Repair performed (commit `b96c50c`, branch `grading-system-repair`):**
- Implemented 33 MediatR handlers wiring every assessment/grading endpoint to a
  single centralized engine.
- Removed the duplicate engine; `IAssessmentEngine` (Infrastructure) is now the
  only grading authority.
- Added grade-change persistence with immutable history, recalculation of
  result/grade/eligibility, and a mandatory reason for published changes.
- Removed hard-coded grade switches; grade letters now come from configurable
  `GradeBands`.
- Added EF Core migration `20260908120000_SeedAssessmentGradingData`
  (moderation metadata columns + idempotent, RLS-aware seed of the 13 assessment
  types, the default grading scale and the default certificate rule).
- Repaired the test build; **346 unit tests pass**, including new weight
  validation, mark entry and post-publication change tests.
- `SMS.API` builds Release with **0 errors / 0 warnings**.

**Deployment status:** At the time of writing the production host became
unreachable from this workstation (SSH port 22 and HTTPS port 443 both time out).
The repair branch is committed and ready; **deployment, migration application and
production re-verification are BLOCKED pending connectivity** (exact commands in
Section 14). Production verification requirements are therefore reported as
**NOT TESTABLE (deployment pending)**, not as PASS.

**Verdict (pre-repair):** NOT PRODUCTION READY — the grading system was inert.
**Verdict (post-repair, code-level):** structurally compliant and build/test
clean; final production readiness requires completing the blocked deployment and
acceptance run.

---

## 2. Production Environment (verified pre-repair)

| Item | Value |
|---|---|
| Hostname | `sms-server` |
| OS | Debian GNU/Linux 13 (trixie) |
| Docker | 29.7.2 (Compose v5.5.0) |
| Deploy directory | `/opt/sms/app` |
| Git commit (pre-repair) | `438331a1f16df84e31b3a6da79ef85d81bd42ca2` |
| Git state pre-repair | 2 commits AHEAD of local `main` (`25d2d33`); working tree DIRTY |
| Snapshot branch | `prod-snapshot-20260908_092825` -> `0ce766b` (exact working tree) |
| Repair branch | `grading-system-repair` -> `b96c50c` |
| Containers (11) | sms-web, sms-api, sms-postgres, sms-nginx, sms-prometheus, sms-grafana, sms-alertmanager, sms-node-exporter, sms-cadvisor, sms-backup, sms-postgres-exporter |
| API image | `docker-api` `16ed8011729d` |
| Frontend image | `docker-frontend` `c032ef8c472a` |
| DB | PostgreSQL 16-alpine, database `SchoolManagementSystem`, user `sms_user` |
| Migrations applied | 8, latest `20260907160951_AddAcademicYearNameSemesterName` |
| Health | `GET /health` -> 200 |
| Pre-repair backup | `/opt/sms/backups/pre_grading_repair_20260908_092631.dump` (275 KB) |
## 3. Requirements Compliance Matrix

Status legend: **PASS** (verified in production), **FAIL** (missing/non-functional
at audit), **PARTIAL** (incomplete/defective), **CODE** (implemented and
unit-tested in the repair branch, deployment pending), **NOT TESTABLE** (could not
be proven in production).

| # | Requirement | Pre-repair | Post-repair (pending deploy) |
|---|---|---|---|
| R1 | Centralized Assessment Engine (single grading authority) | FAIL (two engines, both unreachable) | CODE (one engine; duplicate removed) |
| R2 | No independent module calculates final grades | FAIL (hard-coded switches + duplicate engine) | CODE (switches removed; Grade commands delegate to AssignGradeAsync) |
| R3 | Assessment types (13 standard + admin-configurable) | FAIL (500; table empty) | CODE (seeded + CRUD handlers) |
| R4 | Weighting configuration & persistence | FAIL (500) | CODE |
| R5 | Weight total equals exactly 100% | FAIL | CODE (unit-tested: 99.99/100/100.01/0/negative) |
| R6 | Invalid totals cannot be published | FAIL | CODE (SubmitForReviewHandler rejects) |
| R7 | Weight locks after grading; admin unlock | FAIL | CODE |
| R8 | Manual mark entry (single/edit/draft) | FAIL (500) | CODE |
| R9 | Bulk mark import (validation, duplicate protection) | FAIL | CODE |
| R10 | Mark range restricted | FAIL | CODE (0..MaxScore; unit-tested) |
| R11 | Online assessment integration (no double count) | FAIL | PARTIAL (engine duplicate guard; online bridge is follow-up) |
| R12 | Automatic final score (79.00 case) | FAIL | CODE |
| R13 | Calculation precision & rounding | NOT TESTABLE | CODE (decimal math; unit-tested) |
| R14 | Grading scale A/B/C/F with institutional boundaries | FAIL (empty tables; legacy switch wrong) | CODE (seeded GradeBands) |
| R15 | Boundary values (49.99..100) | NOT TESTABLE | CODE (band membership unit-tested) |
| R16 | Scale versioning; historical results preserved | FAIL | CODE (Version + GradingScaleVersionId) |
| R17 | Certificate eligibility (configurable rules) | FAIL (500) | CODE |
| R18 | Publication Draft->Pending->Approved->Published | FAIL (500) | CODE |
| R19 | Draft/pending results invisible to students | FAIL | CODE (published-only student query) |
| R20 | Moderation (review/return/approve/comments/history) | FAIL (500) | CODE |
| R21 | Post-publication change (history/reason/recalc/eligibility/audit) | FAIL | CODE (unit-tested) |
| R22 | Student portal results | FAIL (no UI; API 500) | CODE (backend only; UI in later stage) |
| R23 | Lecturer dashboard & permissions | FAIL (500) | CODE |
| R24 | Administrative controls | FAIL | CODE |
| R25 | RBAC on grading endpoints | FAIL | CODE (controller Role annotations; 403 for unauthorized) |
| R26 | Security (IDOR, direct API, range) | NOT TESTABLE | CODE (tenant repos + engine range checks); prod re-test pending |
| R27 | Audit logging of grading events | PARTIAL (legacy logs only) | CODE (audit in all write paths) |
| R28 | Reporting (distribution/pass-fail/summary/exports) | FAIL (500) | CODE (aggregation from engine data; PDF/Excel export UI in later stage) |
| R29 | Transcripts use centralized results | FAIL (legacy module reads legacy Grades) | PARTIAL (transcript migration is follow-up) |
| R30 | Automated tests | FAIL (did not compile) | CODE (346 pass; API 0 errors) |
## 4. Architecture Assessment

**Existing assessment architecture (at audit):** `AssessmentController`
(32 endpoints, RBAC-annotated) dispatched MediatR requests that had **no
handlers**. DTOs, entities, repositories and EF tables were present but inert.
Two engines: `SMS.Application.Services.AssessmentEngine` (registered concrete;
orchestrator-style, invoked `mediator.Send` for its own commands) and
`SMS.Infrastructure.Services.AssessmentEngine` (registered as `IAssessmentEngine`;
contained the real calculation logic). The legacy `Grades` module (`Grade`,
`GradeController`, `CreateGradeCommand`/`UpdateGradeCommand`) ran independently
with hard-coded boundaries.

**Repaired architecture:** MediatR handlers (`SMS.Application.Features.Assessments.
Handlers`, 33 classes) are the service layer. They perform CRUD/mapping/auditing and
**delegate every calculation** (weighted score, final score, grade assignment,
eligibility, publication transitions, post-change recalculation) to the single
`IAssessmentEngine` implementation in `SMS.Infrastructure`. The Application engine
was deleted. Legacy `Grade` commands now derive the letter from `AssignGradeAsync`
(GradeBands), removing duplicated grade math.

**Data flow:** repository -> handler -> `IAssessmentEngine` -> PostgreSQL
(tenant-scoped via RLS). Results are persisted as `UnitResult` (the authoritative
final-score store) and surfaced through publish-gated queries.

**API flow:** `POST /api/v1/Assessment/...` (JWT bearer cookie + RBAC) ->
`AssessmentController` -> MediatR handler -> engine -> DB.

**Frontend flow:** Currently absent for assessments (the frontend stage is not part
of this repair). The legacy `Grades.tsx` page targets `/api/v1/grades`.

## 5. Database Assessment

**Existing schema:** all required tables exist under RLS (AssessmentTypes,
Assessments, StudentAssessmentMarks, AssessmentExemptions, GradingScales,
GradeBands, CertificateRules, StudentCertificateEligibilities, GradeChangeHistories,
UnitResults, ModerationRecords, AuditLogs). 8 migrations applied atomically at
startup via `dbContext.Database.MigrateAsync()`.

**Changes made in this repair:**
- New migration `20260908120000_SeedAssessmentGradingData`:
  - Adds `ModerationRecords` columns `StudentId`, `MarkId`, `OriginalScore`,
    `RevisedScore`, `ReviewerComments` (nullable, additive, backwards-compatible).
  - Seeds (idempotent `NOT EXISTS` guards, tenant-aligned with
    `set_config('app.tenant_id', ...)` to satisfy RLS): the 13 AssessmentTypes
    (Assignment..Participation); the Default GradingScale v1 with 4 GradeBands
    (A 75.00-100.00 Distinction; B 65.00-74.99 Credit; C 50.00-64.99 Pass;
    F 0.00-49.99 Fail); and a Default CertificateRule (min 50.00, no outstanding
    incompletes, all required units).

**Data preservation:** no existing rows are modified or deleted; seeds are
insert-only. Pre-repair backup exists (`pre_grading_repair_20260908_092631.dump`).
Historical result handling: `UnitResult.GradingScaleVersionId` snapshots the scale
in force at calculation time; scale changes create new versions rather than
rewriting history.

**Orphaned records:** none found in the grading tables (all empty pre-repair).
## 6. Security Assessment

- **RBAC:** endpoints carry `@Authorize(Roles = ...)`; the repair implements the
  handlers those annotations gate (Lecturer/Administrator/Coordinator for mark
  entry and submission; Administrator/Coordinator for approval/publication and
  moderation; Administrator for scaling/locks/audit). After deploy, unauthorized
  roles should receive 403, not the previous 500.
- **Tenant isolation:** every repository query filters through
  `app.current_tenant_id()` RLS; the seed inserts set the session variable to the
  resolved tenant so seeded config is visible to that tenant.
- **Input validation:** mark range (0..assessment MaxScore) enforced in handlers
  and the engine; weight range 0..100; negative/absent weights rejected.
- **Direct API access:** all assessment endpoints require authentication; student
  results handler intentionally returns only published results.
- **Audit protection:** audit rows are append-only (AuditLog entity documented as
  immutable; no update/delete paths).
- **Identified but not exercised in production:** IDOR probes and role-based
  negative tests are pending the deployment (Section 14).

## 7. Calculation Verification

The engine computes, per mark: percentage = mark / MaxScore * 100;
weighted score = percentage * weight / 100 (rounded to 2 dp,
MidpointRounding.AwayFromZero); final = sum(weighted) / sum(weight) * 100
(normalised so missing/exempt assessments do not distort the 0-100 band).

Acceptance case (Phase 33):

| Assessment | Mark | Weight | Contribution |
|---|---|---|---|
| Assignment 1 | 85 | 10% | 8.50 |
| Assignment 2 | 70 | 15% | 10.50 |
| CAT | 80 | 15% | 12.00 |
| Project | 90 | 20% | 18.00 |
| Final Examination | 75 | 40% | 30.00 |
| **Final score** | | | **79.00** |

Expected final score = **79.00%** -> band A (75.00-100.00) -> **A / Distinction**.
Unit tests cover weight boundaries 99.99/100.00/100.01/0/negative and mark-range
rejection; the end-to-end 79.00 assertion executes against production in the
deployment phase.

## 8. Grading Scale Verification

Seeded default scale (configurable, versioned):

| Boundary | Letter | Description | GPA | Colour |
|---|---|---|---|---|
| 75.00-100.00 | A | Distinction | 4.0 | #00AA00 |
| 65.00-74.99 | B | Credit | 3.0 | #0000FF |
| 50.00-64.99 | C | Pass | 2.0 | #FFA500 |
| 0.00-49.99 | F | Fail | 0.0 | #FF0000 |

Band membership uses inclusive lower bound and inclusive upper bound
(`p >= min && p <= max`). Boundary points 49.99/50.00/64.99/65.00/74.99/75.00/
100.00ão will be asserted against the live API after deployment; the band-mapping
logic is unit-tested.

## 9. Certificate Eligibility Verification

Engine evaluates published unit results against the active CertificateRule
(min overall 50.00; no outstanding incompletes; all required units passed).
`StudentCertificateEligibilities` is persisted and re-evaluated after every
legitimate grade change (`RecalculateAfterGradeChangeAsync`). Scenarios to be
executed post-deploy: pass (eligible), one fail (not eligible), unpublished unit
(not eligible), grade correction flips eligibility.

## 10. Publication and Moderation Verification

Workflow: Calculate -> Submit (requires total weight = 100%) -> PendingReview ->
Approve (Coordinator/Administrator) -> Approved -> Publish -> Published.
Students see results only at Published (student query filters
`GetPublishedByStudentAsync`). Moderation: Review creates a ModerationRecord
(original/revised score, comments, moderator identity); Return marks the
assessment ReturnedForCorrection; Approve finalises; full history retained.
Post-publication changes require a reason, record previous/new values, and
recalculate result/grade/eligibility (unit-tested at handler level; DB-level
verification pending deployment).

## 11. Transcript Verification

Legacy transcript endpoints read the legacy `Grades` table and remain unchanged
in this stage. Backend support for engine-authoritative results is in place
(StudentResultDto / UnitResult), but the transcript module migration to the
centralized source is a documented follow-up. **Not claimed as complete.**

## 12. Reporting Verification

Three handlers (grade distribution, pass/fail rates with per-assessment
breakdown, assessment summary) aggregate exclusively from persisted UnitResult and
mark data with thresholds from the configured CertificateRule. PDF/Excel/CSV export
UI is a later-stage item. Live report verification pending deployment.

## 13. Automated Test Results

Baseline at audit: the unit test project did **not compile**
(12 errors: `CreateCourseOfferingCommand` still referenced removed
`AcademicYearId/SemesterId` fields). After the repair:

- `dotnet build src/SMS.API/SMS.API.csproj -c Release` -> **0 warnings, 0 errors**
- `dotnet build tests/SMS.UnitTests/...` -> **0 errors**
- `dotnet build tests/SMS.IntegrationTests/...` -> **0 errors**
- `dotnet build tests/SMS.ApiTests/...` -> **0 errors**
- `dotnet test tests/SMS.UnitTests/...` -> **346 passed, 0 failed, 0 skipped**

New/extended unit tests added in this stage:
- `ValidateWeightsHandlerTests` (5): 100% exact, 99.99, 100.01, negative, empty.
- `EnterMarkHandlerTests` (3): out-of-range rejection, final-mark engine
  delegation, draft-mark engine delegation.
- `AssessmentChangeMarkTests` (2): reason required for published changes;
  correct engine invocation with actor identity.
- `CourseOfferingCommandTests` repaired to the current command contract.

Coverage tooling is not configured in this repository; statement coverage is
therefore reported as not measured.

## 14. Production Deployment Verification

**BLOCKED - host unreachable.** At the end of the audit the production server
stopped responding from this workstation (TCP 22 and 443 time out; ICMP also
failing). The repair branch `grading-system-repair` (commit `b96c50c`) is fully
committed locally. The exact operator checklist to complete deployment:

```bash
# 0) Preconditions
ssh sms_admin@192.168.110.161
cd /opt/sms/app
git fetch origin grading-system-repair || git fetch ssh://sms_admin@192.168.110.161/opt/sms/app grading-system-repair
git reset --hard origin/grading-system-repair   # == b96c50c; snapshot 0ce766b remains tagged
ls -la /opt/sms/backups/   # confirm pre_grading_repair_20260908_092631.dump exists

# 1) Build the API image (migration auto-applies at startup)
docker compose -f docker/docker-compose.prod.yml build sms-api

# 2) Restart the API
docker compose -f docker/docker-compose.prod.yml up -d sms-api
docker compose -f docker/docker-compose.prod.yml ps

# 3) Verify migration + health
docker logs sms-api --tail 200 | grep -i migration
curl -sk https://192.168.110.161/health

# 4) Seed visibility check (expect the 13 stored types after login)
curl -sk -X POST https://192.168.110.161/api/v1/auth/login -H 'Content-Type: application/json' \
  -d '{"Identifier":"hwainaina@kws.go.ke","Password":"<from /opt/sms/app/.env>"}' -c /tmp/ck
curl -sk -b /tmp/ck https://192.168.110.161/api/v1/assessment/types

# 5) Acceptance (Phase 33): create a controlled unit + 5 assessments
#    (10/15/15/20/40%), enter marks 85/70/80/90/75, calculate -> expect 79.00 -> A ->
#    Distinction; submit -> approve -> publish; verify student visibility only after
#    publish; change one mark with a reason; verify history + recalc + eligibility.
```

If the seeded config is not visible to the resolved tenant, run a corrective,
backup-guarded UPDATE at that point (align `tenant_id` of the seeded rows with the
tenant the app resolves, then re-run the checklist from step 4).
## 15. Repairs Made

| # | Problem | Root cause | Files changed | DB changes | Tests |
|---|---|---|---|---|---|
| 1 | All 32 assessment requests had no handler (HTTP 500) | MediatR registered 193 handlers but zero for assessment/grading | 33 new handler classes under `src/SMS.Application/Features/Assessments/Handlers/` (+ GradingScales etc. wired there) | none | 346 unit tests pass (incl. new) |
| 2 | Duplicate AssessmentEngine (both registered) | Two implementations, one registered via `AddScoped<AssessmentEngine>()` in Application DI | Deleted `src/SMS.Application/Services/AssessmentEngine.cs`; removed registration in `SMS.Application/DependencyInjection.cs` | none | API build 0 errors |
| 3 | Hard-coded grade boundaries (`>=75=>"A-"` etc.) in legacy Grade commands | Independent grade math not driven by GradeBands | `Features/Grades/Commands/CreateGradeCommand.cs`, `UpdateGradeCommand.cs` now call `IAssessmentEngine.AssignGradeAsync` | none | API build 0 errors |
| 4 | No mark-update/change persistence with history | Engine lacked update path | Added `SaveDraftMarkAsync`/`UpdateMarkAsync` to `IAssessmentEngine` + `SMS.Infrastructure/Services/AssessmentEngine.cs` (writes GradeChangeHistory, reasons, recalcs, audits) | none (tables existed) | ChangeMark unit tests |
| 5 | Moderation records lacked student/mark/scores | Entity/model gap | `SMS.Domain/Entities/ModerationRecord.cs` + migration AddColumns | `ModerationRecords`: StudentId, MarkId, OriginalScore, RevisedScore, ReviewerComments (nullable) | API build 0 errors |
| 6 | No institutional grading/type/rule config in DB | Seeds missing / seed script was SQL Server flavoured | Migration `20260908120000_SeedAssessmentGradingData`; `scripts/seed-assessment-data.sql` deprecated | Seeds: 13 AssessmentTypes, GradingScales+GradeBands (A/B/C/F), CertificateRule (idempotent, RLS-aware) | run via `migrate-database` at deploy |
| 7 | Test suite did not compile | Tests referenced removed command fields | `tests/SMS.UnitTests/CourseOfferings/CourseOfferingCommandTests.cs` updated; `AssessmentEngineTests.cs` rewritten as handler tests; added ValidateWeights/EnterMark/ChangeMark tests | none | 346 passed |

## 16. Remaining Issues

1. **Production deployment & acceptance not executed** (host unreachable) -
   the single blocking item. All production verification rows (R13/R15/R26 and the
   Phase 33 end-to-end scenario) remain NOT TESTABLE until the Section 14 checklist
   completes.
2. **Transcript module** still reads the legacy `Grades` table; migrating it to the
   centralized UnitResult source is a follow-up (R29, PARTIAL).
3. **Online assessment bridge** (Assignment/Submission -> StudentAssessmentMark) is
   not built; the engine's duplicate guard prevents double counting, but the sync
   path itself is future work (R11, PARTIAL).
4. **Frontend assessment module** (weight editor, mark entry grid, moderation
   queue, publication, student results views, exports) is a separate stage by
   agreement; the API is ready to consume.
5. Legacy `GetStudentEnrollmentReportQuery` still uses a hard-coded `Score >= 40`
   pass threshold for the legacy grade module; it does not affect the centralized
   engine path but is flagged for consolidation.
6. `accessToken` in the login JSON body is empty by design (JWT is delivered via
   HttpOnly cookies); non-browser API clients must use the cookie flow. Documented
   here so no future client depends on the empty field.

## 17. Final Compliance Score

Mandatory requirements (R1-R30, pre-repair vs code-level post-repair):
- Pre-repair: PASS 0 | PARTIAL 1 (R27) | FAIL 22 | NOT TESTABLE 7.
- Post-repair (code): PASS/CODE 25 | PARTIAL 3 (R11, R29 + frontend R22 UI) |
  NOT TESTABLE in production until deployed: R13, R15, R26, Phase 33 acceptance.

Overall percentage compliance (code-level, stage-1 backend): the 30 mandatory
requirements are structurally satisfied by the repair branch with 3 documented
partials; production-functional compliance cannot be scored until deployment
completes (approximately 0% verifiable in production today).

## 18. Final Production Recommendation

**Pre-repair: NOT PRODUCTION READY** - the grading system was non-functional
(every endpoint 500, no handlers, no seeded config).

**Post-repair (pending deployment):** the backend is build- and unit-test-clean
with a single centralized engine, configurable scaling, auditable grade changes,
and seeded institutional configuration. Deploy the committed branch per Section 14,
run the acceptance scenario, and re-run the security negative tests before
declaring the production grading system ready for real academic use.---

## Rollback Instructions (database and application)

1. Database: restore the pre-repair custom dump.
   docker exec sms-postgres pg_restore -U sms_user -d SchoolManagementSystem -F c /tmp/pre_grading_repair_20260908_092631.dump
   (or restore into a fresh database and re-run `dotnet run --project src/SMS.API -- migrate-database`).
2. Application: return to the pre-repair image and commit.
   git reset --hard 438331a        # pre-repair production HEAD
   docker compose -f docker/docker-compose.prod.yml build sms-api
   docker compose -f docker/docker-compose.prod.yml up -d sms-api
3. Migration rollback if strictly required (not recommended - seeds are idempotent
   and insert-only): remove the seed rows and the 20260908120000 history row, then
   run the migration Down (drops the five additive moderation columns), or simply
   leave the additive columns in place.
4. Never delete the pre-repair backup or the AuditLogs / GradeChangeHistories rows.
---

## Deployment Attempt Log (2026-09-08, post-audit)

A deployment run was attempted immediately after the audit report was produced.
The production host was not reachable:

| Check | Result |
|---|---|
| SSH `sms_admin@192.168.110.161:22` (15 s / 20 s / key auth) | Connection timed out (all attempts) |
| HTTPS `https://192.168.110.161/` and `/health` | No response (`HTTPS:000`) |
| TCP ports 22, 2222, 443, 8443, 5000, 5433 on `.161` | All closed / no response |
| `tracert -h 6 -w 2000 192.168.110.161` | `Hop 1 (NCAICTLT49741.KWS.local [192.168.110.60]) -> Destination host unreachable` |
| ARP table for subnet 192.168.110.0/24 | **No entry** for `.161` (host not answering ARP) |
| Alternate IP from `~/.ssh/known_hosts` (`192.168.110.42`) | Also unreachable (TCP 22/443 no response) |
| DNS name `sms-server` (Omada LAN DNS) | Does not resolve from this workstation |

**Conclusion:** the production server was temporarily absent from the LAN at the
time of the first attempt (no ARP response, unreachable at layer 2/3).

---

## Deployment Execution Log (server back online)

The server came back online and the full deployment was executed successfully.

### Deployed artifacts

| Item | Value |
|---|---|
| Branch | `grading-system-repair` (server reset to repair commits; deploy via `deploy-temp` ref) |
| Final deployed API image | `docker-api:latest` `8e5132729096` (2026-09-08 17:58) |
| Migration applied | `20260908120000_SeedAssessmentGradingData` ("Applying 1 pending migration(s)" -> "Database migrations applied successfully") |
| DB seed state | 13 AssessmentTypes, 1 GradingScale, 4 GradeBands, 1 CertificateRule (tenant `11111111-...`) |
| Moderation columns | StudentId, MarkId, OriginalScore, RevisedScore, ReviewerComments added |
| Container health | `sms-api` Up (healthy); `sms-nginx` proxies; public `/health` 200 |

### Bugs found & fixed during deployment (all committed)

1. **Migration not discovered at runtime** — EF Core required `[DbContext]` and
   `[Migration]` attributes on the migration class; the generator template lacked
   them. Fixed generator + seed-migration SQL (correct `"id"` / `"created_date"`
   column names for the BaseEntity-mapped tables).
2. **`IAssessmentEngine` never registered in Program.cs** — `ServiceExtensions`
   is dead code; added `AddScoped<IAssessmentEngine, AssessmentEngine>()` to the
   runtime DI graph.
3. **`CreateAssessmentHandler` missing** — dropped during an earlier file edit;
   restored.
4. **`UnitResult` / eligibility always issued UPDATE** — code used
   `Id == Guid.Empty` to detect new entities, but `BaseEntity.Id` defaults to
   `Guid.NewGuid()`; replaced with explicit null checks on repository lookups.
5. **Duplicate mark returned HTTP 500** — EnterMarkHandler now throws
   `ConflictException` -> **409**.
6. **Eligibility always NotEligible** — C# precedence bug: `??` binds looser than
   `||`, so `!rule?.RequireAllRequiredUnits ?? true || X` evaluated as
   `(!rule.X) ?? (true || X)` = `false`; parenthesized correctly.

### Production verification results (live)

`GET /api/v1/assessment/types` -> **200** (13 types)
`GET /api/v1/assessment/grading-scales` -> **200** (Default Grading Scale, 4 bands)
`GET /api/v1/assessment/audit-log` -> **200**
`POST /api/v1/Assessment/marks` duplicate -> **409** (ConflictException)

### Phase 33 final acceptance (controlled unit CTU101, weights 10/15/15/20/40)

| Step | Result |
|---|---|
| Weight validation | `isValid: true, total: 100` |
| Marks 85/70/80/90/75 entered | 200 (all 5) |
| Final score | **79.0** |
| Grade / description | **A / Distinction** (colour #00AA00, passed true) |
| Pre-publish student visibility | 0 (draft/pending/approved hidden) |
| submit -> approve -> publish | 200 / 200 / 200 |
| Post-publish student visibility | 1 |
| Certificate eligibility | **Eligible** (missingRequirements []) |

### Phase 14 post-publication change (live)

| Step | Result |
|---|---|
| Change Final Examination 75 -> 50 (reason recorded) | 200 |
| Recalculated final | **69.0** (79.0 - (75-50)*0.40) |
| Recalculated grade | **B / Credit** |
| Eligibility re-evaluated | True |
| GradeChangeHistories row | PreviousScore=75, NewScore=50, PreviousGradeLetter=A, NewGradeLetter=C, ChangeReason=Correction, reason text recorded |
| Audit trail | MarkChanged, GradeRecalculated, EligibilityUpdated entries present |
| Unit test suite | **347 passed / 0 failed** |
| API Release build | 0 errors / 0 warnings |

### Actions still recommended
- Remove the controlled test unit `CTU101` (and its assessments/marks/results)
  once the institution is satisfied with the evidence (the unit is clearly
  namespaced "Phase 33 Controlled Test Unit"). It is isolated test data only.
- Point the frontend assessment module at these endpoints (later stage).

