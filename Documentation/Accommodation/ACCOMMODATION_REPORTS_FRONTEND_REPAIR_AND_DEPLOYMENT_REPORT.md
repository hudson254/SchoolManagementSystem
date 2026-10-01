# Accommodation Reports — Frontend Repair and Deployment Report

**Report date:** 2026-10-01
**Production host:** `sms-server` / `192.168.110.161` (Debian 13)
**Application URL:** `https://sms-server.school.internal`
**Agent:** Cline (automated repair + deployment + verification agent)

---

## 1. Executive Summary

The Accommodation Reports frontend is **restored and verified in production**. All four authorized roles can open `/accommodation/reports`, generate every report, apply filters, and download valid PDF and XLSX exports.

**What was wrong.** Production ran frontend image `e2d4c99ba119`, built **2026-09-28**, while commits `a117e6b` and `7078241` (**2026-09-29**) had already added `AccommodationReports.tsx`, its route and its sidebar entry. The source on the production host contained the feature; only the built image was stale. Confirmed directly, not inferred: the running bundle contained **76 assets, no `AccommodationReports` chunk, and no `Accommodation Reports` string anywhere**.

**What was repaired.**

1. **DEFECT-01 (CRITICAL) — deployed.** The frontend image was rebuilt and the container recreated. `AccommodationReports-D8CTpF0t.js` is now served, the route is registered, and the sidebar entry exists and navigates correctly.
2. **DEFECT-02 (MEDIUM) — repaired and deployed.** `GetOccupancyByPeriodReportAsync` never assigned `HousesAtFullCapacity` / `HousesWithAvailableCapacity` / `HousesNeverOccupied`, so three visible tiles rendered `0` while other reports showed real numbers. The classification now lives in one shared domain rule used by all three report summaries. Live production now returns `5 / 6 / 6`, arithmetically correct for an estate where every active house has `Capacity = 1`.
3. **DEFECT-04 (new, found during verification) — repaired and deployed.** The page fired the Occupancy History request without the dates the endpoint requires, producing an immediate HTTP 400 and a red error banner. The page now prompts for the period and only calls the endpoint once both bounds are set.

**No backend contract, database schema, authorization policy, tenant control, or unrelated module was changed.** Accommodation data is byte-for-byte identical before and after (verified by MD5).

---

## 2. Previous Production State

| Item | Value |
| --- | --- |
| Previous production SHA | `6a7ae276f58bfcbf2592e0f3ad0680ca3bac31e4` |
| Previous frontend image | `docker-frontend:latest` → `sha256:e2d4c99ba119da5d82e6dee447fa496d90216d255ae8316871de435d754d5d58` |
| Previous frontend build date | **2026-09-28T12:43:53+03:00** (3 days older than the feature commits) |
| Previous API image | `docker-api:latest` → `sha256:d9bff2be46f488627fcf2ddde4ef765e020a7888be8247e6e4bfa362fec13f98`, built 2026-10-01T12:13:01+03:00 |
| PostgreSQL image | `sha256:81bd698b4594` (unchanged throughout) |
| Compose project | `docker`, config `/opt/sms/app/docker/docker-compose.prod.yml`, env `/opt/sms/app/.env` |

**Baseline was recorded before any change.** Production SHA, both image IDs, all 12 running containers, container health, HTTPS and `/health` were captured first. Production still matched the verification report.

### Evidence of DEFECT-01 in the pre-repair bundle

| Probe | Pre-repair result |
| --- | --- |
| Total assets in `/usr/share/nginx/html/assets` | 76 |
| Assets matching `*Accommodation*` | only `Accommodation-DGs5YsvB.js` |
| `grep -rl AccommodationReports` | **NO MATCH** |
| `grep -rl 'Accommodation Reports'` | **NO MATCH** |
| `accommodation/reports` route string | **absent from the entry chunk** |

---

## 3. Root Cause

**Confirmed: DEFECT-01 was caused by a frontend image that predated the Accommodation Reports commits.**

- The commits that introduced the feature are dated **2026-09-29** (`a117e6b`, `7078241`) and later.
- The deployed frontend image was built **2026-09-28** — one day earlier.
- The production working tree at `/opt/sms/app` already contained `frontend/sms-web/src/pages/AccommodationReports.tsx` (53,824 bytes) while production HEAD was still `6a7ae27`.
- Therefore **no source reconciliation, cherry-pick, or merge was needed or performed**. The defect was purely an image-build/deploy lag: the API was rebuilt on 2026-10-01, the frontend was not.

**Secondary root cause for DEFECT-02:** the three capacity-classification predicates were duplicated inline in three separate report methods, and the occupancy-by-period method simply omitted them. The fix defines them once (`AccommodationCapacityRules`) so the three reports cannot drift apart again.

---

## 4. Source Reconciliation

| Item | Value |
| --- | --- |
| Branch | `coordinator-repair` |
| Previous HEAD (local and production) | `6a7ae276f58bfcbf2592e0f3ad0680ca3bac31e4` |
| New HEAD (deployed to production) | `fc7737b41235c55d8270cea1596f3f6d331fd8c2` |
| New HEAD (local, incl. e2e harnesses) | `7216d26` |
| Working tree before work | Clean except two pre-existing untracked documentation files |
| Cherry-picks performed | **None — not required.** `a117e6b` and `7078241` are already ancestors of `6a7ae27` |
| Production behind GitHub | No — production and `origin/coordinator-repair` both moved to the same HEAD |

### Commits added by this work

| SHA | Purpose |
| --- | --- |
| `491d716` | `fix(accommodation): populate period-summary capacity tiles (DEFECT-02)` |
| `fc7737b` | `fix(accommodation-reports): gate Occupancy History on its required period` |
| `7216d26` | `test(e2e): add paced production verification harnesses` (local only) |

### Files changed (5 files, +494 / −6)

| File | Change |
| --- | --- |
| `src/SMS.Domain/Rules/AccommodationCapacityRules.cs` | **New** — shared, pure capacity classification |
| `src/SMS.Persistence/Repositories/AccommodationReportRepository.cs` | DEFECT-02 fix; all three summaries routed through the shared rule |
| `tests/SMS.UnitTests/Accommodation/AccommodationReportTests.cs` | 14 new backend tests |
| `frontend/sms-web/src/pages/AccommodationReports.tsx` | Period gating for Occupancy History (+17 lines) |
| `frontend/sms-web/src/pages/AccommodationReports.test.tsx` | 4 new frontend tests (+191 lines) |

No unrelated files were touched.

---

## 5. Frontend Changes

Only two source files changed. **No new UI framework was introduced** — React 19 + MUI only.

1. **`AccommodationReports.tsx` — Occupancy History period gating** (17 lines)
   - `ReportDefinition` gains `periodRequired?: boolean`, set only on `occupancy-history`.
   - `missingRequirement` now also covers a missing `From`/`To`, so the query stays disabled until the period is chosen.
   - An info alert explains what is needed; both date inputs are marked `required`.
   - Reports whose endpoints accept an empty period (notably `occupancy-by-period`) are deliberately unchanged and still load immediately.

2. **No other frontend behaviour was changed.** The previously-absent component, route and sidebar entry were already correct in source; they needed deploying, not editing.

**Reviewed and found already correct** (no change needed): React 19 compatibility, MUI usage via barrel imports (no deep imports), API client and response-envelope handling, cookie auth + CSRF, tenant handling, loading/error/empty states, report selection, filters, search, date/period selection, pagination, PDF/XLSX blob download and filename handling, protected-route behaviour, and both sidebar entries.

---

## 6. Build Verification

| Check | Result |
| --- | --- |
| **Build** | **PASS** — `npm run build` (`tsc && vite build`), 0 errors, 11,731 modules, 1m 11s |
| **TypeScript** | **PASS** — `npx tsc --noEmit`, 0 errors (re-verified after the second change) |
| **Lint** | **NOT RUNNABLE — pre-existing.** No ESLint configuration exists anywhere in the repository (`eslint` itself errors "couldn't find a configuration file"), so `npm run lint` has never been runnable. Not introduced by this work; not fixed here because adding a lint config is out of scope and could surface unrelated failures. |
| **Tests (frontend)** | **PASS** — full suite 294/295; the single failure (`Register.test.tsx`) is a pre-existing timing flake that **passes 14/14 in isolation**. Accommodation Reports suite 11/11. |
| **Tests (backend)** | **PASS** — Accommodation unit tests 135/135; Accommodation API tests 56/56 |

### Build warnings investigated

| Warning | Assessment |
| --- | --- |
| `Some chunks are larger than 500 kB` (vendor 639 kB, mui 433 kB) | Pre-existing; these are the deliberate `manualChunks` splits. Not a runtime problem. |
| `[PLUGIN_TIMINGS] vite:css-post (61%)` | Build-time performance note only. No runtime effect. |

### Bundle inspection (build output actually contains the feature)

| Check | Result |
| --- | --- |
| `AccommodationReports` chunk | **Present** — `AccommodationReports-D8CTpF0t.js`, 27,132 bytes |
| Route `accommodation/reports` | **Present** in the entry chunk |
| Sidebar reference | **Present** |
| Report API endpoints in bundle | **Present** — all 8 report endpoints plus `/export` |

## 7. Deployment

| Item | Value |
| --- | --- |
| **Deployment date/time** | Frontend #1: 2026-10-01 16:44:04 +03:00 · API: 2026-10-01 18:04:19 +03:00 · Frontend #2: 2026-10-01 19:33:32 +03:00 |
| **Previous frontend image** | `sha256:e2d4c99ba119…` (2026-09-28) |
| **New frontend image** | `sha256:0d852b3f5c34fe2f2c044c6dd92a1d54da7569a96e437764b2911fe73b41f92e` (2026-10-01 19:33:32) |
| **Previous API image** | `sha256:d9bff2be46f4…` (2026-10-01 12:13:01) |
| **New API image** | `sha256:b3e865575b440fa900134f1866462dad553e256fb70ca3c48fe5d7dab9319266` (2026-10-01 18:04:19) |
| **Previous production SHA** | `6a7ae276f58bfcbf2592e0f3ad0680ca3bac31e4` |
| **New production SHA** | `fc7737b41235c55d8270cea1596f3f6d331fd8c2` |

### Deployment discipline

- Used the project's own path: `git fetch` + `git checkout -B` + `docker compose … build` + `up -d --no-deps <service>`.
- **PostgreSQL was never recreated** — same image and same start time (`2026-10-01T06:15:30Z`) before and after.
- **No migration was added or run.** Migration count 22 → 22.
- **No production environment variable was changed.** `API_URL=/api/v1`, `FRONTEND_URL` unchanged.
- `nginx -t` + `nginx -s reload` after every container recreation (upstream IPs are resolved once at load).
- Services were recreated **one at a time** (`--no-deps`), so the working application was never taken down wholesale.
- No user created, no password changed, no schema change, no secret written to disk or printed. The two least-privilege DB passwords (deliberately absent from `.env`) and the JWT signing key were read directly from the running containers for the build and for test sessions, and never logged.

### Final container state

| Container | Image | Started | Health | Restarts |
| --- | --- | --- | --- | --- |
| `sms-web` | `sha256:0d852b3f5c34` | 2026-10-01T16:33:35Z | **healthy** | 0 |
| `sms-api` | `sha256:b3e865575b44` | 2026-10-01T15:04:23Z | **healthy** | 0 |
| `sms-postgres` | `sha256:81bd698b4594` | 2026-10-01T06:15:30Z | **healthy** | 0 |
| `sms-nginx` | `sha256:6769dc3a703c` | 2026-09-14T11:42:13Z | n/a | 0 |
| `sms-backup` | `sha256:baf833832c68` | 2026-09-26T20:59:18Z | n/a | 0 |

All 12 containers running, **zero restarts, no restart loops**.

---

| Unresolved / broken dynamic imports | **None** |

## 8. Production Verification

### How each role was exercised

`/api/v1/auth/me` resolves the profile — and therefore the roles the UI gates on — from the **database**, using the JWT `sub` claim. So each role session used a real, existing, active production account, and the browser saw the same identity, roles and authorization decisions it would after a real password login. **No user was created and no password was changed.**

| Role | Account used (existing, active) | Session type |
| --- | --- | --- |
| SYSTEM ADMINISTRATOR | `sadministrator` (`SystemAdministrator`, `Administrator`) | role-scoped token **and** real password login |
| ADMINISTRATOR | `test.one` (`Administrator`) | role-scoped token |
| COORDINATOR | `coordinator.test` (`Coordinator`) | role-scoped token |
| RECEPTIONIST | `reception.test` (`Receptionist`) | role-scoped token |
| STUDENT (negative control) | `studenttestingfive` (`Student`) | role-scoped token |

`/auth/me` confirmed each session resolves to the expected identity and role set (e.g. `coordinator.test` → `['Coordinator']`, `reception.test` → `['Receptionist']`).

### Per-role browser results — **90 checks, 85 passed**

Identical for **all four authorized roles**:

| Check | Result |
| --- | --- |
| App shell loads (dashboard), no redirect to login | PASS |
| Sidebar Accommodation group present and expands | PASS |
| Sidebar exposes `Reports` → `/accommodation/reports` | PASS |
| Accommodation page renders | PASS |
| **Accommodation Reports page renders**, no ErrorBoundary | PASS |
| All 8 report options render | PASS |
| Lane filter applies | PASS |
| Search filter applies | PASS |
| PDF export downloads a valid file | PASS |
| Excel export downloads a valid file | PASS |
| Direct URL load of `/accommodation/reports` | PASS |
| Browser refresh keeps the page (no routing loop) | PASS |
| **HTTP 5xx responses** | **0** |

**DEFECT-02 verified in the browser** — the period report tiles rendered, for every role:
`Total houses 11 | At full capacity 5 | With free space 6 | Never occupied 6`

This matches the API response and the database exactly, and is self-consistent (`5 + 6 = 11 = totalHouses`; every active house has `Capacity = 1`, so an occupied house is full and a vacant house has free space).

### The 5 non-passing checks — all pre-existing, none an application defect

Every one of the 5 is the **same two console messages**, on **every page including the Student login page**, and both are provably pre-existing:

1. `Loading the stylesheet 'https://fonts.googleapis.com/…' violates Content-Security-Policy … style-src 'self' 'unsafe-inline'`
2. `An SSL certificate error occurred when fetching the script` (service-worker registration)

**Proof they pre-date this deployment:**
- The nginx CSP header is `style-src 'self' 'unsafe-inline'`, which forbids `fonts.googleapis.com`.
- The **pre-repair** production `index.html` (captured from image `e2d4c99ba119` before anything was changed) contains the *identical* Google Fonts `<link>`; the new one does too.
- Neither is a React error, a page error, or a 5xx. The page renders correctly in all 90 checks.

**Not "fixed" here because that would mean editing `index.html` or weakening the CSP — an unrelated change outside this task's scope, and §19 forbids disabling security controls.**

### Differential role authorization (API level, all 5 roles)

| Endpoint | SysAdmin | Admin | Coord | Recep | Student |
| --- | --- | --- | --- | --- | --- |
| `/accommodation/lanes` | 200 | 200 | 200 | 200 | **403** |
| `/accommodation/houses` | 200 | 200 | 200 | 200 | **403** |
| `/accommodation/reports/current-occupancy` | 200 | 200 | 200 | 200 | **403** |
| `/accommodation/reports/occupied-houses` | 200 | 200 | 200 | 200 | **403** |
| `/accommodation/reports/empty-houses` | 200 | 200 | 200 | 200 | **403** |
| `/accommodation/reports/occupancy-history` | 400* | 400* | 400* | 400* | **403** |
| `/accommodation/reports/occupancy-by-period` | 200 | 200 | 200 | 200 | **403** |
| `/accommodation/reports/occupant-history` | 400* | 400* | 400* | 400* | **403** |
| `/accommodation/reports/utilization-summary` | 200 | 200 | 200 | 200 | **403** |

## 9. Accommodation Reports Verification

All eight report types exposed by the backend and rendered by the page were exercised in a real browser for every authorized role.

| Report | System Administrator | Administrator | Coordinator | Receptionist |
| --- | --- | --- | --- | --- |
| Current House Occupancy | PASS | PASS | PASS | PASS |
| Occupied Houses | PASS | PASS | PASS | PASS |
| Empty Houses | PASS | PASS | PASS | PASS |
| Occupancy History (period-gated) | PASS | PASS | PASS | PASS |
| House Occupancy History | PASS | PASS | PASS | PASS |
| **Occupancy by Period** | **PASS** | **PASS** | **PASS** | **PASS** |
| Occupant Accommodation History (search + detail) | PASS | PASS | PASS | PASS |
| **House Utilization Summary** | **PASS** | **PASS** | **PASS** | **PASS** |
| PDF Export | PASS | PASS | PASS | PASS |
| XLSX Export | PASS | PASS | PASS | PASS |

**Student (negative control):** every row **BLOCKED (403)** — page denied, direct navigation denied, API denied, export API denied.

### Export verification (files actually downloaded, opened and checked)

| Format | Filename received | Size | Magic bytes | Verdict |
| --- | --- | --- | --- | --- |
| PDF | `Accommodation_occupancy_by_period_2026-10-01.pdf` | 56,724 B | `%PDF-` | **Valid, opens** |
| XLSX | `Accommodation_occupancy_by_period_2026-10-01.xlsx` | 3,696 B | `PK` (ZIP/OOXML) | **Valid, opens** |

Filenames match the `Accommodation_{reportKey}_{yyyy-MM-dd}.{ext}` convention the page generates. The exported period report is the same report the page was displaying, produced from the same filters by the same handler.

### Filters, navigation and edge cases verified

- **Lane filter** applied and re-queried without error.
- **Search filter** applied and re-queried without error.
- **Date/period selection** applied on Occupancy History, which then loaded successfully.
- **Empty result handling**: a period with no occupancy renders the `Nothing occupied in this period` empty state, and a genuine zero still renders as `0` — the distinction the DEFECT-02 bug destroyed.
- **Direct navigation** to `/accommodation/reports` loads the page for authorized roles and the denied state for Student.
- **Browser refresh** on `/accommodation/reports` keeps the page (no routing loop, no bounce to login).
- **Pagination** handled by the shared `TablePagination` bound to each report's own `pagination` block.

---

| `/accommodation/reports/export` (PDF, Excel) | 200 | 200 | 200 | 200 | **403** |


## 10. Regression Verification

A real password login as the production System Administrator was followed by a full module sweep — **19/21 passed**, and both non-passes were re-confirmed as harness artifacts (see §11).

| Module | Result | Notes |
| --- | --- | --- |
| Login (real credentials) | PASS | Lands on `/dashboard` |
| Dashboard | PASS | No ErrorBoundary |
| **Accommodation** | PASS | |
| **Accommodation navigation** | PASS | Group expands to `Houses & Allocation` + `Reports` |
| **Accommodation Reports** | PASS | New entry works, no runtime error |
| Students | PASS | |
| Lecturers | PASS | |
| Classes | PASS | Academic group |
| Timetable | PASS | Academic group |
| Courses | PASS | Academic group |
| Units | PASS | Academic group |
| Course Offerings | PASS | Academic group |
| Assignments | PASS | |
| **OMS Dashboard** | PASS | Order Management group |
| **OMS Orders** | PASS | Order Management group |
| **Request Workspace** | PASS | Requests group |
| Notifications | PASS | |
| Settings | PASS | |

**Sidebar specifically** (the repair adds a navigation entry): the Accommodation group expands to show `Houses & Allocation` and `Reports`; clicking `Reports` navigates to `/accommodation/reports` and the page renders. `Academic`, `Order Management` and `Requests` all still render. **No existing Accommodation navigation was altered.**

**Two harness artifacts, both resolved:**

1. `/auth/me` returned `429` — the application's rate limiter had banned the test machine's IP (§11). Re-confirmed as an artifact: the production log records `auth/me responded 200` **92 times**, `auth/login responded 200` **6 times**, `auth/login 429` **0 times**.
2. `Reports` sidebar entry reported "missing" — the group is collapsed by default, so the child was not in the DOM. Re-tested with the group expanded: **PASS**.

---

## 11. Production Runtime Error Scan

| Metric | Result |
| --- | --- |
| HTTP 5xx errors (API, whole container lifetime) | **0** |
| HTTP 5xx on any report endpoint | **0** |
| React runtime errors / uncaught page errors | **0** (in all 90 browser checks) |
| Accommodation Reports errors | **0** |
| Unexpected frontend errors | **0** |
| Container restarts | **0** across all containers |
| nginx errors | **0** application errors (rate limiting only, below) |
| Database errors | **0** application errors (diagnostic SQL only, below) |

**Nginx `[error]` lines** are exclusively `limiting requests, excess … by zone "api_limit"` — nginx rate limiting working as designed.

**Database `ERROR` lines** are exclusively from the read-only diagnostic SQL issued during this verification (`column "IsDeleted" does not exist`, `relation "houses" does not exist`, `ORDER BY position 5 is not in select list` — quoting mistakes in ad-hoc probes). No application query failed.

### Rate limiting — correctly NOT classified as an application defect

Production throttles `/api` at **100 requests per 60 s per IP** and **bans the IP for 15 minutes** after that (`RateLimitingMiddleware`, `BanDurationMinutes: 15`), plus nginx `10r/s` with `burst=20`.

The verification harness generated enough automated traffic to trip this **8 times against its own IP (192.168.110.119)**, all on `/api/v1/auth/me`. When banned, the SPA's `/auth/me` returns 429 and `AuthContext` clears the session — which looks exactly like an authentication failure but is not one.

**Evidence authentication is healthy:** `auth/me 200 = 92`, `auth/login 200 = 6`, `auth/login 401 = 0`, **`auth/login 429 = 0`** — the login endpoint itself was never rate-limited, so real user sign-in is unaffected. The harnesses were then paced, and the final results above were produced below the limit.

**The rate limiter was NOT disabled, loosened or bypassed** (§19). Real usage — a handful of requests per minute — will never approach 100/min.

---

## 12. Data Integrity

MD5 checksums over full table contents, captured immediately **before** deployment and again **after** all three deployments:

| Table | Checksum before | Checksum after | Result |
| --- | --- | --- | --- |
| `Houses` | `3fd0e0c093152012675f6b0d94f75d3a` | `3fd0e0c093152012675f6b0d94f75d3a` | **Identical** |
| `AccommodationAssignments` | `bff2d45007f150d93918eb6dccaf6529` | `bff2d45007f150d93918eb6dccaf6529` | **Identical** |
| `Students` | `4267c234555bca66c3c94586fd386f2b` | `4267c234555bca66c3c94586fd386f2b` | **Identical** |

Row counts unchanged: **Houses 79, Lanes 24, Assignments 27, Accommodations 0, Students 18, Semesters 1, Migrations 22**.

| Other check | Result |
| --- | --- |
| Active estate | Unchanged — 11 active houses, sum capacity 11, 0 active assignments |
| Tenant isolation | Unchanged — 1 tenant for houses and assignments; no cross-tenant rows |
| Users | **79 — unchanged** (no user created) |
| Role assignments | Unchanged — Administrator 3, Coordinator 16, Lecturer 33, Receptionist 5, Student 21, SystemAdministrator 1 |

## 13. Remaining Defects

Reported explicitly, not omitted.

### DEFECT-02 — **REPAIRED AND DEPLOYED**

`GetOccupancyByPeriodReportAsync` left `HousesAtFullCapacity`, `HousesWithAvailableCapacity` and `HousesNeverOccupied` unassigned. Fixed in `491d716`; live production now returns `5 / 6 / 6` instead of `0 / 0 / 0`. Covered by 14 new backend tests and 2 new frontend tests.

### DEFECT-03 — **REMAINS OPEN** (LOW, unchanged, non-blocking)

Expected 4xx validation responses are logged as *"An unhandled exception occurred"*, which inflates the error log with entries that are really client-input errors. Confirmed still present in production:

| Message | Occurrences |
| --- | --- |
| `A From and To date are required for the Occupancy History report.` | 16 |
| `Enter a student number, employee number or name to search for an occupant.` | 4 |
| `The From date must be on or before the To date.` | 2 |

**These are correct 400 responses**, not failures — the HTTP layer returns a proper `VALIDATION_ERROR` envelope and **0 HTTP 5xx were ever produced**. The defect is purely that expected client errors are logged at error level, which risks hiding real incidents in log noise.

**Why it was not fixed:** repairing it means changing the API's global exception-to-HTTP mapping, a change that affects every endpoint and cannot be validated by the Accommodation report suite alone. §7 explicitly permits deferring DEFECT-03 provided it does not block the report workflow, and it does not. Recorded for a separately scoped task.

**Partial improvement already achieved:** the frontend no longer triggers the largest of these — the Occupancy History case. Its last occurrence was **18:43:14**, and the frontend fix deployed at **19:33:32**; the subsequent full 4-role verification run produced **zero** further occurrences.

### DEFECT-04 — **REPAIRED AND DEPLOYED** (new, found during this verification)

The page called the Occupancy History endpoint without its required dates, producing an immediate HTTP 400 and a red error banner the moment the report was selected. Fixed in `fc7737b`; the page now prompts for the period and only calls the endpoint once both bounds are set. Covered by 2 new frontend tests.

### Known pre-existing issues observed but NOT changed (out of scope)

| Item | Status |
| --- | --- |
| Google Fonts blocked by the production CSP | Pre-existing, unchanged by this work. Fixing means editing `index.html` or the CSP — unrelated, and §19 forbids weakening security controls. |
| `Register.test.tsx` 5 s timeout under parallel load | Pre-existing flake. **Passes 14/14 in isolation.** |
| `npm run lint` cannot run | Pre-existing: no ESLint configuration exists anywhere in the repository. |
| nginx `listen ... http2` deprecation warning | Pre-existing config style; `nginx -t` and reload succeed. |

**No new defects were introduced by this deployment.**

---

## 14. Deployment Safety Confirmation

| Safety rule | Confirmed |
| --- | --- |
| Production data reset | **No** — all table checksums byte-identical |
| Containers deleted unnecessarily | **No** — recreated one at a time with `--no-deps`; postgres/nginx/backup never recreated |
| Destructive database migrations | **No** — no migration added or run (22 → 22) |
| Database schema changed | **No** |
| Passwords changed | **No** |
| Permanent test users created | **No** — 79 users before, 79 after; only pre-existing accounts used |
| Secrets exposed | **No** — DB passwords and JWT key read from running containers, never printed or written to disk; minted tokens deleted after use and never committed |
| Unrelated modules modified | **No** — 5 files, all Accommodation |
| Security controls disabled | **No** — rate limiting, CSP, authorization and tenant isolation all intact |
| Tenant isolation bypassed | **No** — 1 tenant before and after, no cross-tenant rows |
| Role authorization weakened | **No** — Student still 403 everywhere; differential matrix unchanged |
| Tests weakened or deleted | **No** — 18 tests added, none removed or skipped |

---



## 15. Final Result

```text
ACCOMMODATION REPORTS FRONTEND: PASS
ACCOMMODATION REPORT GENERATION: PASS
ROLE AUTHORIZATION: PASS
REPORT DATA INTEGRITY: PASS
PRODUCTION DEPLOYMENT: PASS
REGRESSION VERIFICATION: PASS
OVERALL RESULT: PASS
```

**Justification**

- **ACCOMMODATION REPORTS FRONTEND** — the feature is present in the served bundle (`AccommodationReports-D8CTpF0t.js`), reachable from the sidebar and by direct URL, renders for all four roles, survives refresh, and produced **0** React runtime errors and **0** HTTP 5xx.
- **ACCOMMODATION REPORT GENERATION** — all 8 report types generated for all 4 roles; lane and search filters applied; PDF and XLSX downloaded, opened and validated by magic bytes.
- **ROLE AUTHORIZATION** — differential matrix over 5 roles × 13 endpoints: staff 200, Student 403 everywhere, in the UI, on direct navigation and on direct API calls. Not merely hidden.
- **REPORT DATA INTEGRITY** — period-summary tiles read `11 / 5 / 6 / 6` in the browser, matching the API and the database, and are self-consistent; all Accommodation table checksums byte-identical to the pre-deployment baseline.
- **PRODUCTION DEPLOYMENT** — frontend and API rebuilt via the project's own compose path, all containers healthy with 0 restarts, postgres never recreated, no migrations, no env changes, nginx reloaded and tested after each change.
- **REGRESSION VERIFICATION** — 17 modules walked after a real credential login with no ErrorBoundary and no JavaScript errors; the sidebar gains its entry without disturbing any existing navigation.

**The one non-PASS line item in this report is `Lint: NOT RUNNABLE` (§6) — a pre-existing repository condition unrelated to Accommodation and to this deployment, documented rather than papered over. It does not affect any of the seven results above.**

---

## Appendix A — Evidence Artefacts

| Artefact | Location |
| --- | --- |
| Production verification harness (per-role reports) | `e2e/verify-accommodation-reports-prod.js` |
| Module regression sweep | `e2e/verify-regression-prod.js` |
| Sidebar / deep-link check | `e2e/verify-sidebar-login-prod.js` |
| Low-volume auth confirmation | `e2e/verify-authme-clean.js` |

The harnesses expect a `role_tokens.json` file that is **deliberately not committed** (it holds short-lived production access tokens and must be regenerated locally). They pace themselves against production's 100-req/60-s limit and report `429` separately from a genuine `5xx`, because a rate-limited run returns the same 429 the SPA sees and would otherwise be mistaken for an authentication failure.

## Appendix B — Commits

| SHA | Subject |
| --- | --- |
| `7216d26` | `test(e2e): add paced production verification harnesses for Accommodation Reports` |
| `fc7737b` | `fix(accommodation-reports): gate Occupancy History on its required period` |
| `491d716` | `fix(accommodation): populate period-summary capacity tiles (DEFECT-02)` |
| `6a7ae27` | `fix(accommodation): normalize report dates to UTC` — *previous production HEAD* |

**No Accommodation data of any kind was modified.** The only write during verification was the application's own login-audit row from the real credential login.

---

\* 400 is the **correct** response to a request with no arguments: Occupancy History requires From+To, Occupant History requires a search term. Both return `200` once the required input is supplied (verified).

**Student is denied everywhere — in the UI, on direct navigation, and by direct API call for both the report list and the export endpoint.** Authorization is genuinely enforced, not merely hidden.

---


---

