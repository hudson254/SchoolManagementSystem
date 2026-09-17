# Production deployment validation — final report

Date: 2026-09-16. Checkout: coordinator-repair, HEAD ec9e06a.
Resumed validation: 2026-09-17. No commit created; working tree left uncommitted.

## Decision

**NOT PRODUCTION READY** for the requested OMS deployment.
This report supersedes the unsupported completion/root-cause claims in
PRODUCTION_OMS_INTEGRATION_COMPLETE.md and the provisional integration guide.

## Updates in the final validation pass (2026-09-17)

All previously failing API tests are now resolved; production host is
unreachable (network outage) so the production smoke test could not be rerun.

Two real backend/fixture defects were found and fixed (tests were not weakened):

1. **Upload metadata stored a path that never existed on disk.**
   `FileStorageService.UploadFileAsync` prefixes the stored file with a GUID and
   returns the real container-relative path, but `UploadService.UploadAsync`
   created the `UploadFile` metadata record with the *pre-GUID logical path*
   before uploading, so every download/delete of a freshly uploaded file
   targeted a nonexistent path (`FileNotFoundException` → HTTP 500).
   Fix (`src/SMS.Infrastructure/Services/UploadService.cs`): upload first,
   then create the metadata record using the actual returned storage path, and
   persist metadata only after storage succeeds (no orphaned DB rows).

2. **Test fixture invented a teaching-assignment status that production never
   writes.** The fixture seeded `CourseOfferingLecturer.Status = "Confirmed"`,
   but the real lifecycle is `PendingConfirmation` →
   (`ConfirmTeachingAssignmentCommand`) → `Status = "Active"`, and the lecturer
   dashboard (`GetActiveByLecturerAsync`) filters `Status == "Active"`. The
   lecturer dashboard therefore returned an empty Courses list.
   Fix (`tests/SMS.ApiTests/Controllers/DocumentUploadApiTests.cs`): seed
   `Status = "Active"` with `ConfirmationStatus = Confirmed`, matching the
   production state machine.

## Updated actual results

| Validation | Result |
|---|---|
| Release build (SchoolManagementSystem.sln) | 0 errors, 0 warnings, exit 0 |
| Release unit suite | 408 passed, 0 failed, 0 skipped; exit 0 |
| Release API suite | 111 passed, 0 failed, 0 skipped; exit 0 |
| DocumentUploadApiTests (upload→download→delete, dashboards) | 9 passed, 0 failed |
| `docker compose -f docker/docker-compose.oms.yml config --quiet` | exit 0 |
| OMS Compose stack (oms-test-api, oms-test-postgres) | Up 22h, both healthy |
| OMS container storage write probes (/app/data, /app/data/wal-recovery, /app/data/order-manifests, /app/uploads) | ALL_WRITABLE, all on named volumes (not container layer) |
| `npm run build` (frontend/sms-web) | exit 0, built in 1m10s (chunk-size warnings only) |
| Production smoke test | NOT RERUN: 192.168.110.161 unreachable (ping and ports 22/443 time out) |
| Named 01 → 02 → 03 workflow sequence | Not executed: workflows absent from this checkout |

## Root cause / repository mismatch

The exact sms-new startup failure cannot be established: production docker ps -a
and inspect show no sms-new container. The running application is sms-api,
healthy, started 2026-09-15T12:46:00.108934275Z. Its health does not prove that
local uncommitted changes were deployed.

A filesystem search of 1,229 text files, including untracked files, excluding
.git/dependencies/build outputs, found no implementation of OrderCreationApiTests,
OrderManifestStorageTests, ProductionStorageLayoutTests, RoadAccounting,
OrderFormPage or ProdStorage. Documentation mentions are not implementations.
The requested 01/02/03 workflows and frontend-form-bug-discovery.md are absent.
Only .github/workflows/ci-cd.yml exists. Missing OMS configuration is NOT a
proven startup root cause. The correct checkout/deployment identity is required.

## Actual results

| Validation | Result |
|---|---|
| Debug build | 0 errors, 67 warnings |
| Release restore/build | Incremental log: 0 errors, 0 warnings; runner reported terminal-close error after output |
| Debug unit suite | 408 passed, 0 failed, 0 skipped |
| Release unit suite | 408 passed, 0 failed, 0 skipped; exit 0 |
| UsernameGen subset | 55 passed, 0 failed (included in unit totals) |
| API suite via oms_test | 106 passed, 5 failed, 0 skipped; total 111; exit 1 |
| Requested OMS/storage suites | Not executable: code/tests absent |
| SMS.IntegrationTests | Not rerun; fixture may select InMemory and contains EnsureDeleted; no real-PG claim |
| npm ci --no-audit --no-fund; npm run build | Bundle built in 1m15s with warnings; runner reported terminal-close error after output |
| Root and canonical Compose config | Both exit 0 |
| bash -n ./oms_env.sh | Passed |
| Local Compose API /health | Healthy, PostgreSQL reachable |
| Production HTTPS / | HTTP 200 with curl -k; certificate trust unverified |
| Production HTTPS /health | Healthy, PostgreSQL reachable with curl -k |
| Production log inspection | 0 exception/[ERR]/[FTL] lines in last 300 at inspection |
| Full OMS authenticated smoke / restart | Not completed |
| Named 01 → 02 → 03 sequence | Not executed: workflows absent |

API failures: three document-download requests returned HTTP 500; two dashboard
tests failed deserializing OccupantType. Assertions were not weakened.

## Compose and environment

Both files target the isolated sms-oms-test project. Root file uses Compose
include, preserving relative paths of the canonical file in docker/.
Actual validation commands (expanded secrets not printed):

```bash
docker compose -f docker-compose.oms.yml config > /dev/null
docker compose -f docker/docker-compose.oms.yml config --quiet
source ./oms_env.sh
oms_test
```

oms_test now explicitly selects Compose PostgreSQL at localhost:5434 and
propagates test failures. It runs SMS WebApplicationFactory tests, NOT OMS HTTP
requests through the running application container (localhost:5080).
No full OMS end-to-end success is claimed.

Effective production environment presence, without values:

```text
ASPNETCORE_ENVIRONMENT = PRESENT
ASPNETCORE_URLS = PRESENT
ConnectionStrings__DefaultConnection = PRESENT
JWT_SECRET = PRESENT
JwtSettings__Secret = MISSING
Frontend__Url = PRESENT
RoadsDb__File = MISSING
WALRecovery__Target = MISSING
WALRecovery__RecoveryPath = MISSING
OrderManifestStorage__BasePath = MISSING
FileStorage__Path = PRESENT
```

Alternative JWT keys need not both exist. Environment absence does not prove
absence from JSON. DSNs, JWT_SECRET, DB_PASSWORD and ADMIN_PASSWORD are
secret-bearing. Supply production values through the protected deployment
environment, never the test helper. appsettings.Production.json is gitignored;
local edits are not a durable committed deployment fix.

## Production state and data safety

```text
PostgreSQL: HEALTHY (API connectivity check)
sms-new: FAILED acceptance (absent)
Frontend: HEALTHY (root responds; interactive flow unverified)
API: HEALTHY at /health (feature validation incomplete)
Storage: FAILED acceptance (OMS persistence/restart unverified)
OMS integration: FAIL (mandatory path not executed)
```

Production and local test API mounts are persistent writable volumes at
/app/data, /app/uploads, /app/logs and /app/dataprotection-keys. This alone does
not prove actual order-manifest writes. The described legacy /app/lib/WAL data
cannot be identified from available evidence.
No volume deletion/prune, database/schema drop or production test-record
creation was performed in this resumed validation. Visible production volumes
were left intact. API tests targeted only local sms-oms-test PostgreSQL.

## Working tree file inventory (repository-relative)

- .env.example: provisional storage environment names.
- docker/Dockerfile.api: writable directories created before ownership assignment.
- docker/docker-compose.prod.yml and docker/docker-compose.yml: provisional storage wiring.
- src/SMS.API/Program.cs: upload/access DI registrations and directory provisioning.
- docker/docker-compose.oms.yml: isolated SMS application/PostgreSQL test stack.
- docker-compose.oms.yml: root include entry point; config validated.
- oms_env.sh: corrected database selection and health/test failure propagation.
- tests/SMS.ApiTests/Controllers/DocumentUploadApiTests.cs: untracked regression tests, five failures.
- tests/SMS.UnitTests/Configuration/AppSettingsJsonValidityTests.cs: optional-file JSON guards.
- Documentation/ProductionReadiness/OMS_INTEGRATION.md: provisional guide, marked superseded where conflicting.
- PRODUCTION_OMS_INTEGRATION_COMPLETE.md: earlier report marked unsupported/superseded.
- PRODUCTION_DEPLOYMENT_FINAL_REPORT.md: this evidence-based report.

Additional read-only checks: /swagger/v1/swagger.json and /openapi/v1.json returned
404 from both production sms-api and local Compose API. No OpenAPI order
contract was obtained; a disabled documentation endpoint does not prove absence
of server routes. No order request was invented.

## Remaining work

Obtain the correct OMS checkout and sms-new deployment identity. ~~Resolve five
API failures~~ (RESOLVED 2026-09-17: all 111 API tests pass; see "Updates").
Remaining: rerun the production smoke test once 192.168.110.161 is reachable
(earlier verification: api healthy, HTTPS 200, storage writable, no startup
exceptions; this must be re-confirmed post-outage), and rerun full OMS
container/restart validation. No frontend routing changes were made;
OrderFormPage is absent. No commit created: production is unreachable so the
smoke-test evidence cannot be refreshed, and the deployment identity question
remains open. No production secrets, dumps or generated binaries were added by
this resumption.
