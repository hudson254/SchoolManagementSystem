# Study Materials — Scan, Repair, Validation and Deployment Report

**Report date:** 2026-10-02
**Production host:** `sms-server` / `192.168.110.161` (Debian 13, Docker, LAN-only)
**Application URL:** `https://sms-server.school.internal` (unchanged)
**Approved commits:** `a7e9de7`, `e43a5c1`
**Final production SHA:** `e43a5c199b3f68db6995fb97d40e5e74a50ab578`

---

## 1. Baseline

| Item | Value |
| --- | --- |
| Previous repository SHA | `7fb5dee51fc5f311a85ac51d4a907674c13801cc` |
| Previous production SHA | `fc7737b41235c55d8270cea1596f3f6d331fd8c2` |
| Production branch / state | `coordinator-repair`, not detached, no tracked modifications |
| Production drift | 3 commits behind `origin/coordinator-repair` |
| Backend build (baseline) | PASS — 0 errors, 421 pre-existing warnings |
| Unit tests (baseline) | **808 passed, 0 failed, 0 skipped** |
| Containers | `sms-api`, `sms-web`, `sms-postgres` healthy; `sms-nginx`, Prometheus/Grafana stack healthy |
| Latest EF migration | `20260930120000_EnableTenantRowLevelSecurity` |

**Existing Study Materials implementation status: backend complete, frontend
present but unreachable.** The domain entity, repository, DTO, two commands, two
queries, the controller, `AcademicAccessService` and the `UploadFile` pipeline
were all already implemented and correctly authorized. The React service,
component, per-unit page and route existed too. What did not exist was any way
for a user to *find* or *enter* the feature from navigation.

## 2. Findings

| # | Severity | Location | Issue | Corrective action |
| --- | --- | --- | --- | --- |
| 1 | **HIGH** | `frontend/sms-web/src/components/Layout/Sidebar.tsx` | No Study Materials entry at all. Neither lecturer nor student could reach the feature; only a hand-typed URL or a dashboard unit chip opened it. | Added the **Academics → Study Materials** entry for `Lecturer` and `Student`. |
| 2 | **HIGH** | *(no endpoint existed)* | No way to choose a unit. `UnitStudyMaterialsPage` requires a unit id in the URL and nothing listed the caller's entitled units. | Added `GET /api/v1/study-materials/my-units` + `StudyMaterialsPage` selector. Deliberately **not** built from the dashboard payloads (see §4). |
| 3 | **MEDIUM** | `src/SMS.Application/Features/StudyMaterials/Commands/CreateStudyMaterialCommand.cs` | Upload stored bytes + `UploadFile` row *before* inserting `LectureNote`; a failed insert leaked an unreferenced file nothing reclaimed. | Wrapped the save; the orphaned upload is retired via `IUploadService.DeleteAsync` and its id logged. |
| 4 | **MEDIUM** | `.../Queries/DownloadStudyMaterialQuery.cs` | A material whose file was gone returned **HTTP 500** (`FileNotFoundException` derives from `IOException`). | Now returns **404**, and the storage path stays out of the response. |
| 5 | **MEDIUM** | `.../Commands/CreateStudyMaterialCommand.cs:120` | **Found during production verification.** An Administrator/Coordinator without a Lecturer profile resolved a null lecturer and the handler dereferenced `lecturer.Id` → `NullReferenceException` → **HTTP 500** instead of 403. Only the lecturer branch was null-guarded. | Single null guard after the role branches → `ForbiddenException` (403). |
| 6 | LOW | `frontend/sms-web/src/services/studyMaterial.service.ts` | Frontend allow-list had 7 extensions; the backend `LecturerNotes` category accepts 13, so a lecturer could not select a legitimate `.txt`/`.rtf`. | Aligned to the backend list. |

**Not defects (verified as pre-existing / by design):**
`ExceptionHandlingMiddleware` logs 403/404 outcomes at `ERR` level
application-wide; `GET /course-offerings` 404s because `CourseOfferingController`
carries no explicit `[Route]`; a single `/health` 502 occurred in the 1-second
window while the API container was recreated.

## 3. Changes made

**Backend**
* `StudyMaterialController` — new `GET my-units`.
* `Features/StudyMaterials/Queries/GetMyStudyMaterialUnitsQuery.cs` — **new** query + handler.
* `DTOs/StudyMaterialDto.cs` — new `StudyMaterialUnitDto`.
* `Domain/Interfaces/IUnitRepository.cs` + `Persistence/Repositories/UnitRepository.cs` — **additive only**: `GetUnitsByIdsAsync`.
* `DownloadStudyMaterialQuery.cs` — missing file → 404.
* `CreateStudyMaterialCommand.cs` — orphaned-upload compensation + null-lecturer 403.

**Frontend** (React 19 + MUI only; no new framework, no new dependency)
* `pages/StudyMaterialsPage.tsx` — **new** unit selector.
* `components/Layout/Sidebar.tsx` — navigation entry.
* `utils/roles.ts` — `STUDY_MATERIALS_ROLES`.
* `App.tsx` — `/study-materials` route, lazy-loaded like its siblings.
* `services/studyMaterial.service.ts` — `getMyUnits()`, `StudyMaterialUnit`, aligned extensions.

**Database:** none. `LectureNotes` already exists in `20260728195330_InitialMigration`;
the new query is read-only, so **no migration was created or required**.

**Authorization:** no policy was weakened. One path changed from 500 to 403.

**Tests added:** 8 backend unit, 4 API, 11 frontend.

## 4. Design note — why the dashboard was not reused

`GET /dashboard/lecturer-me` and `/student-me` already return units, so reusing
## 5. Security verification

| Control | Result |
| --- | --- |
| Lecturer unit authorization | Enforced server-side via `LecturerTeachesUnitAsync(lecturerId, unitId)`; client `unitId` never trusted. Lecturer→unassigned unit = **403** (unit + API test). |
| Student enrollment authorization | Enforced via `StudentEnrolledInUnitAsync`; unenrolled unit = **403** (unit + API test). |
| Tenant isolation | `ITenantAwareEntity` global query filter + PostgreSQL RLS; foreign ids resolve to nothing. |
| File access protection | Downloads streamed through the authenticated endpoint; material id re-authorized against its **own** `UnitId` (no IDOR). |
| Upload validation | Extension, double-extension, blocked list, MIME-from-magic-bytes, magic-byte signature, 50 MB cap + `[RequestSizeLimit]`. Executable = 400 (API test), 403 in production (authorization precedes validation). |
| Download authorization | Unknown material = **404**; malformed id = **400**; anonymous = **401**. |
| Path traversal | `FileStorageService.ResolveSafePath` rejects `../` and absolute escapes; `../../../etc/passwd.pdf` upload → **403**; no server path in any response body (**verified: `no` leak**). |
| Cross-tenant | Covered by the EF global filter, RLS and the existing isolation suites (120/120 integration tests pass). |
| CSRF | State-changing calls require the double-submit token; without it → **403**. |

## 6. Test results

| Check | Result |
| --- | --- |
| Backend build (Release) | PASS — **0 errors** |
| Frontend typecheck (`tsc --noEmit`) | PASS — **0 errors** |
| Frontend production build (`vite build`) | PASS — emits `StudyMaterialsPage-DVvKHaJ1.js` (only the pre-existing chunk-size warning) |
| Unit tests | **816 passed, 0 failed, 0 skipped** (baseline 808 + 8 new) |
| Integration tests | **120 passed, 0 failed, 0 skipped** |
| API tests — Study Materials | **13 passed, 0 failed** (9 existing + 4 new) |
| API tests — full suite | **222 passed, 1 failed of 223** |
| Frontend vitest | **293 passed, 4 failed of 297** |

**The two failures are pre-existing and unrelated.**

* `AccommodationReportDateApiTests.OccupancyHistory_BoundaryDates_AroundSeededStay_ShouldBeInclusive` — reproduced **identically (1 failed / 31 passed)** on a pristine `git worktree` at `7fb5dee`, before any change. It is an Accommodation test (`Nullable.Value` null-ref at line 233 of the test itself) and this work touches no Accommodation file.
* The 4 `Register.test.tsx` timeouts — pass **14/14 in isolation**; a load-related flake on this machine (suite import time 3262s), not related to Study Materials.

No test was weakened, skipped or deleted.

## 7. Deployment

| Item | Value |
| --- | --- |
| Previous production SHA | `fc7737b41235c55d8270cea1596f3f6d331fd8c2` |
| New production SHA | `e43a5c199b3f68db6995fb97d40e5e74a50ab578` |
| GitHub SHA | `e43a5c199b3f68db6995fb97d40e5e74a50ab578` (matches) |
| Branch | `coordinator-repair` (not detached, no tracked modifications) |
| Migration applied | **None required** — no schema change |
| Backups | `predeploy_studymaterials_20261002T073358Z.dump` (723,780 B) and `predeploy_sm403fix_20261002T075750Z.dump` (724,263 B), both verified with `pg_restore --list` |
| Containers rebuilt | `docker-api` and `docker-frontend`; second pass rebuilt **API only** (backend-only fix) |
| Containers recreated | `sms-api`, `sms-web` — no other container touched |
| Deployment time | 2026-10-02 ~10:33–11:00 EAT (two passes) |
| Post-deploy health | `{"status":"Healthy"}`, postgresql Healthy |

Unchanged as required: hostname, TLS, LAN DNS, authentication config, JWT
secrets, database credentials. No secret was printed, logged or committed —
admin credentials were read from `/opt/sms/app/.env` inside the remote script.
them looked tempting. They build their lists from a **narrower** set of tables
than the authorization path: `GetEnrolledUnitIdsAsync` also covers legacy
`Enrollment` rows and `StudentEnrollment` rows, which the dashboards omit. A
selector built on the dashboards could therefore hide a unit the API would allow
or show one it would reject. `my-units` derives from the **same**
`GetTaughtUnitIdsAsync` / `GetEnrolledUnitIdsAsync` calls the handlers authorize
with, so the list and the authorization decision cannot diverge.
## 8. Production verification

| Check | Result |
| --- | --- |
| HTTPS | 200 |
| API health | `{"status":"Healthy"}` |
| Frontend | 80 assets; `StudyMaterialsPage`, `UnitStudyMaterialsPage`, `studyMaterial.service` chunks present; `Study Materials` nav string present; `study-materials/my-units` present |
| Containers | `sms-api` healthy, `sms-web` healthy, `sms-postgres` healthy |
| Authentication | Login 200, `/auth/me` 200 |
| `my-units` | 200 |
| List a real unit | 200 |
| **Admin upload (the fix)** | **403** — was 500. Log: `Study material upload blocked: no lecturer profile resolves for user …` |
| Upload no title / no file | 400 / 400 |
| Upload executable / double extension / traversal filename | 403 / 403 / 403 |
| Upload without CSRF header | 403 |
| Download unknown id | 404 |
| Malformed ids | 400 |
| Delete without `unitId` | 400 |
| Anonymous (all three routes) | 401 |
| Data integrity | `LectureNotes` still **1** row — every probe upload was refused, none leaked |
| Path disclosure | none |
| Regression smoke | `units`, `courses`, `students`, `lecturers`, `oms/orders`, `assignments`, `grades`, `assessment/types`, `certificates`, `timetables`, `classes`, `notifications`, `dashboard/statistics` → all 200 |
| Logs | **0** `NullReferenceException`, **0** 500 responses, **0** nginx 5xx after the fix deploy; no Npgsql/DbUpdateException/migration errors |

## 9. Remaining issues

1. **Lecturer and student happy-path upload/download not exercised against live production with those specific roles.** Production seeds only the administrator, and no lecturer/student production credentials exist in the repository. Those paths are covered by the 13 API tests, which run the real auth pipeline, the real EF tenancy and a real PostgreSQL database — but a live browser session as a lecturer and as a student is still outstanding. Recommend a manual pass with real accounts.
2. **`AccommodationReportDateApiTests` failure** — pre-existing, proven at baseline, left untouched as out of scope.
3. **`Register.test.tsx` flake** — pre-existing, passes in isolation.
4. **Pre-existing, not addressed:** `ExceptionHandlingMiddleware` logs correct 403/404 outcomes at `ERR`, which inflates error monitoring app-wide; `CourseOfferingController` exposes no explicit `[Route]`; `UploadService.DeleteAsync` soft-deletes metadata without removing the physical file (so a compensated upload still occupies disk until manual cleanup).
5. **No lint run:** no ESLint config exists anywhere in the repository, so `npm run lint` has never been runnable. Not introduced here.

---

## Appendix — Commits

| SHA | Purpose |
| --- | --- |
| `a7e9de7` | `fix(study-materials): make the feature reachable and correct under Academics` — findings 1–4, 6 |
| `e43a5c1` | `fix(study-materials): return 403 not 500 when an admin has no lecturer profile` — finding 5 |

User documentation: `Documentation/StudyMaterials/README.md`.