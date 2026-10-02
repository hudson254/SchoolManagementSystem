# Notifications and Progressive Web App - Independent Validation Report

**Date:** 2026-10-02
**Branch:** `coordinator-repair`
**Validated baseline SHA:** `82072f560fe6ffa8a13ba8606208039b028e2120`
**Outcome:** PASS on validated gates / **2 BLOCKERS FOUND - production deployment NOT performed**

---

## 1. Scope

Independent re-validation of the Notifications + PWA work before production deployment.
Two mandatory success criteria were **not met** (see section 10). Per the task's own
instruction - "If any mandatory gate fails, stop deployment and report it" - the production
database was **not** migrated and the application was **not** deployed.

Everything else was validated against a real clone of the production database and a live
API instance, not by inspection alone.

---

## 2. Starting state (verified directly, not assumed)

| Item | Value |
| --- | --- |
| Local branch / SHA | `coordinator-repair` @ `82072f5` |
| `origin` (GitHub) `coordinator-repair` | `82072f5` - identical to local |
| Production `/opt/sms/app` SHA | `82072f5` - identical to local |
| Working tree | 28 modified, 1 staged-add, 1 deleted, 20 untracked |
| Production containers | `sms-api` healthy, `sms-web` healthy, `sms-postgres` healthy, `sms-nginx` up |
| Production `/health` | `200 {"status":"Healthy"}` |
| Production latest EF migration | `20260930120000_EnableTenantRowLevelSecurity` (22 applied) |
| Production `Notifications` rows | 585 |

**Critical observation:** local HEAD, GitHub and production were all the *same* SHA, and
the Notifications/PWA work existed **only as an uncommitted working-tree diff**. Nothing
had been committed, pushed, or deployed. The reported SHA was the *pre-change* baseline,
not the implemented change.

---

## 3. Build and test results

| Gate | Result |
| --- | --- |
| Solution build (`Release`) | **0 errors**, 416 warnings |
| Unit tests (`SMS.UnitTests`) | **898 / 898 passed** |
| API tests (`SMS.ApiTests`) | **222 / 223 passed**, 1 pre-existing failure |
| Integration tests (`SMS.IntegrationTests`) | **120 / 120 passed** (RLS, cross-tenant, least-privilege) |
| Frontend build | **succeeded** (~62 s) |
| Frontend tests (`vitest`) | **339 / 340 passed**, 1 pre-existing failure |
| New PWA/notification frontend tests | **32 passed** (Notifications 9, utils 12, InstallPrompt 11) |

### Pre-existing failures - independently re-confirmed

| Test | Full suite | In isolation | Verdict |
| --- | --- | --- | --- |
| `AccommodationReportDateApiTests.OccupancyHistory_BoundaryDates_AroundSeededStay_ShouldBeInclusive` | FAIL | FAIL (1/32 in class) | **Pre-existing**, unrelated to this work |
| `Register.test.tsx > disables Next until a strong password is entered` | FAIL (5371 ms > 5000 ms) | **PASS 14/14** (2392 ms) | **Pre-existing** 5 s timeout under parallel load |

No new failures were introduced.

### Note on the warning count

The brief claimed "0 errors, 0 warnings". The build is error-free but emits **416
warnings**. These are pre-existing and project-wide (test-project nullability warnings,
xUnit analyzer suggestions). Only **2 distinct** warnings touch notification code, both
pre-existing navigation-property nullability (`Notification.User`, `User.Notifications`).
**No warning was introduced by this work.** The "0 warnings" claim is inaccurate.

---

## 4. Database clone validation PASS

A **fully isolated** clone was built - a separate `postgres:16-alpine` container
(`sms-notif-clone`) with its own volume, restored from a `pg_dump` of production. The
production database and its volume were never written to.

Clone source: `/opt/sms/clones/sms_prod_clone_20261002_150911.dump`
(md5 `adaeb1cfd47bd0fcca25b7d0758a74a8`, 724735 bytes)

Clone content: **585 notifications, 79 users, 1 tenant, 22 migrations** - representative
production data.

### Migration under test

`20261002094649_AddNotificationPriorityActionUrlExpiry`

Generated SQL is minimal and purely additive:

```sql
ALTER TABLE "Notifications" ADD "ActionUrl"  character varying(512);
ALTER TABLE "Notifications" ADD "ExpiresAt"  timestamp with time zone;
ALTER TABLE "Notifications" ADD "Priority"   character varying(20) NOT NULL DEFAULT 'Normal';
UPDATE "Notifications" SET "Priority"='Normal'
 WHERE "Priority" IS NULL OR "Priority" NOT IN ('Informational','Normal','Important','Critical');
CREATE INDEX "IX_Notifications_UserId_IsRead_Priority" ON "Notifications" ("UserId","IsRead","Priority");
INSERT INTO "__EFMigrationsHistory" VALUES ('20261002094649_...','9.0.3');
```

### Before / after comparison

| Measure | Before | After | Verdict |
| --- | --- | --- | --- |
| Notification rows | 585 | 585 | unchanged |
| Distinct recipients | 20 | 20 | ownership intact |
| Read / unread | 53 / 532 | 53 / 532 | unchanged |
| Soft-deleted | 0 | 0 | unchanged |
| **Content fingerprint** | `cf67d2b52c36bd1a2adfd6ebf87c8554` | `cf67d2b52c36bd1a2adfd6ebf87c8554` | **identical - no row mutated** |
| **User fingerprint** | `732342381c29d0c4794e6a09155c8072` | `732342381c29d0c4794e6a09155c8072` | **identical** |
| Columns | 19 | 22 | +3 expected |
| Indexes on `Notifications` | 5 | 6 | +1 expected |
| Migrations | 22 | 23 | +1 expected |
| Backfill | - | all 585 rows to `Normal` | correct catalogue value |

No unintended records were modified, no unintended indexes or constraints created, and
tenant relationships were untouched.

### Idempotency PASS

Re-running `dotnet ef database update` against the migrated clone reported:
"No migrations were applied. The database is already up to date." (exit 0)

### `Down` migration PASS

Executed against a **second disposable clone** (`sms-notif-down`):

- `Up` applied (exit 0)
- `Down` reported "Reverting migration '20261002094649_...'" (exit 0)
- Post-rollback: 585 rows, fingerprint `cf67d2b52c36bd1a2adfd6ebf87c8554` - **data fully
  intact**, back to 22 migrations, 0 new columns.

---

## 5. Security regression testing PASS

Run against a **live API** bound to the migrated clone, using two genuinely registered and
authenticated users with separate cookie sessions. **17 / 17 passed.**

| # | Test | Expected | Actual |
| --- | --- | --- | --- |
| 1 | User B reads **own** notification (positive control) | 200 | 200 PASS |
| 2 | **A reads B's notification (IDOR)** | **404** | **404** PASS |
| 3 | **A marks B's notification read (IDOR)** | **404** | **404** PASS |
| 4 | **A deletes B's notification (IDOR)** | **404** | **404** PASS |
| 5 | A lists with forged `?userId=<B>` | 200, no B rows | 200, **no leak** PASS |
| 6 | B marks **own** notification read (control) | 204 | 204 PASS |
| 7 | B's row is read in the database | `true` | `true` PASS |
| 8 | A marks read after B already read it | 404 | 404 PASS |
| 9 | A did not mutate B's row | `true` | `true` PASS |
| 10 | Anonymous `GET /api/v1/notifications` | 401 | 401 PASS |
| 11 | Anonymous unread-count | 401 | 401 PASS |
| 12 | **Anonymous SignalR `/hub/negotiate`** | **401** | **401** PASS |

The API returns **404, never 403**, for cross-user access, so notification ids cannot be
used to enumerate other users' data. Ownership is derived from the authenticated
principal at every handler (`GetForUserAsync` / `MarkAsReadForUserAsync` /
`DeleteForUserAsync`), and `GetMyNotificationsQuery` no longer accepts a client-supplied
`UserId` at all.

Tenant isolation is enforced by the `Notifications` global query filter **and** PostgreSQL
RLS; `SMS.IntegrationTests` (120/120) covers `CrossTenantIsolationTests`,
`RLSIsolationTests`, `TenantIsolationTests`, `TenantRowLevelSecurityTests` and
`LeastPrivilegeRolePrivilegeTests`.

### SignalR group hijacking - server side PASS

`NotificationHub` carries `[Authorize]`, is mapped with `.RequireAuthorization()`, and
`AddToGroupAsync` is called **exactly once**, from `OnConnectedAsync`, using
`Context.UserIdentifier`. A repository-wide search confirms **no client-controlled
subscription method survives** - `SubscribeToNotifications` exists only in comments,
documentation, and the regression test that asserts its absence.

---

## 6. Service worker, PWA and cache versioning PASS

| Requirement | Result |
| --- | --- |
| `/api/**` excluded from cache | PASS (allowlist-only design) |
| `/hub/**` excluded | PASS |
| `/uploads/**` excluded | PASS |
| Auth responses not cached | PASS (fall through to network) |
| WebSocket upgrade not intercepted | PASS (`request.mode === 'websocket'` returns early) |
| Cache names versioned | PASS (`sms-shell-<hash>`) |
| Old caches removed on activate | PASS (deletes every cache not equal to `CACHE_NAME`) |
| `skipWaiting` not called unprompted | PASS (deferred behind `SKIP_WAITING`) |

The worker is **allowlist-based, never denylist-based** - only `/assets/*` and known icons
are cacheable, so a new API route can never accidentally start leaking into a cache.

### Build A / Build B cache-version test PASS (mandatory test, performed)

| Build | Change | `CACHE_NAME` |
| --- | --- | --- |
| A | baseline | `sms-shell-1y5qjl4` |
| B | one hidden span in `NotFound.tsx` (lazy chunk) | `sms-shell-14blptj` |

The ids differ, so a new deployment starts a fresh cache and the `activate` handler
invalidates the previous release in one step. The version is derived from built
`index.html` **plus** the sorted emitted asset filenames, so a change confined to a
lazily-loaded chunk - which leaves `index.html` byte-identical - still changes the cache.
The probe was reverted; `NotFound.tsx` is byte-identical to the committed version.

### PWA manifest PASS

`id`, `name`, `short_name`, `start_url: "/"`, `scope: "/"`, `display: "standalone"`,
`theme_color: "#576426"`, `background_color: "#f5f7f0"`, two shortcuts. Icon dimensions
were measured from the actual PNG bytes:

```
icon-192.png              192x192
icon-512.png              512x512
icon-maskable-192.png     192x192   (purpose: maskable)
icon-maskable-512.png     512x512   (purpose: maskable)
apple-touch-icon-180.png  180x180
```

The previous manifest pointed a 192x192 and a 512x512 slot at a 595x420 `logo.png`, which
failed Chrome's installability check; that is corrected.

### nginx configuration PASS

Verified on production **before** deployment - the defect is real and present today:

```
/sw.js  ->  cache-control: public, immutable
           max-age=31536000       <- one year; pinned the browser to one worker forever
           CACHE_NAME = 'sms-cache-v1'   <- hard-coded, never versioned
/icons/*.png -> 404 (not yet deployed)
```

The new configs add exact-match `location = /sw.js`, `= /manifest.json` and `= /index.html`
blocks with `no-cache, must-revalidate`. In nginx an exact-match (`=`) location outranks a
regex location, so these correctly take precedence over the existing
`expires 1y; immutable` static-asset block. No existing security header (HSTS,
`X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`) was
weakened - all remain present on production responses.

---

## 7. Notification lifecycle test PASS (for the one wired business event)

An OMS request was driven through the real business flow - **not** by inserting a
notification row - against the live API:

```
POST /api/v1/oms/requests             -> 201 (draft created)
POST /api/v1/oms/requests/{id}/submit -> 200
```

Result: notification rows went **587 to 601** (14 new), each:

```
Request submitted: REQ-2026-000029 | type=OmsRequest | pri=Normal | unread=true
```

- Recipients resolved **server-side** to the approver roles -
  `Administrator, Coordinator, SystemAdministrator`
- The submitting student was **not** a recipient (`requester_is_recipient_count = 0`)
- All rows unread, correctly typed, correctly prioritised

This confirms the full pipeline works end-to-end: **business event -> persisted ->
correct recipients -> unread state**. It also confirms the same pipeline is *not* invoked
by any other business domain - see 10.1.

---

## 8. SignalR live notification test FAIL - no client subscribes

**Expected result (per the brief):** a notification appears in an open application
without a manual refresh, and the unread count changes.

**Actual result:** it cannot. **No web client connects to the hub.**

Evidence:

- `frontend/sms-web/package.json` has **no** `@microsoft/signalr` dependency
- `frontend/sms-web/package-lock.json` has **no** signalr entry
- A repo-wide search of every `.ts` / `.tsx` / `.js` / `.jsx` under `frontend/` and `e2e/`
  for `signalr`, `HubConnection`, `withUrl`, `ReceiveNotification`, `EventSource`,
  `WebSocket` returns **zero** matches in source (only built `dist/` vendor bundles and
  service-worker comments)

`Header.tsx` refreshes via React Query `refetchOnWindowFocus: true`, with a source comment
stating this is "the documented substitute for real background push on a LAN-only
deployment". The server side is complete and secure, but nothing subscribes, so
`SignalRNotificationRealtimePublisher` sends to empty groups.

The hub *authorization* tests pass (anonymous negotiate returns 401; groups derived from
claims; no hijackable subscribe method). It is the **delivery** half of the feature that is
absent.

---

## 9. Not verified / out of scope

The following require a real browser and physical devices and could not be executed from
this environment. They are **not** claimed as passing:

- Chromium desktop install prompt, standalone launch, app icon, uninstall/reinstall
- Android install-to-home-screen, standalone launch, service-worker update on device
- iOS/iPadOS "Add to Home Screen" guidance on a real device
- Live end-to-end service-worker *update* in an installed PWA (the cache-id change and the
  nginx headers were verified; the browser-side update handshake was not)
- Browser tab suspension / resume behaviour

---

## 10. Blockers - production deployment NOT performed

### 10.1 BLOCKER - business events outside OMS raise no notifications

The success criteria require "Important School Management System events generate
appropriate notifications", and section 7 of the brief lists course, unit, assignment,
lecture notes, accommodation, lecturer assignment, coordinator, receptionist and
administrator events.

Search of `src/SMS.Application/Features/` for the word `Notification`:

| Feature area | Files | Notification references |
| --- | --- | --- |
| Accommodation | 51 | **0** |
| Assignments | 20 | **0** |
| Units | 11 | **0** |
| Courses | 8 | **0** |
| Enrollments | 13 | **0** |

`new Notification(...)` appears in exactly two places - `BroadcastNotificationHandler`
and `SendNotificationToRoleHandler` - both reached only through the moderator/admin HTTP
endpoints. `INotificationDispatcher` is referenced **only** by its own definition, its DI
registration, its tests and the documentation. `RegistrationNotificationService` is not
registered in `DependencyInjection.cs` and is never injected - it is dead code.

The catalogue, DTOs and presentation layer already understand `Accommodation`,
`Assignment`, `Unit`, `Course`, `LectureNotes`, `Registration`, `AccountApproval`,
`Security`, `Certificate`, `Grade` and `Announcement`; only the business-handler wiring is
missing. `Documentation/Notifications-PWA/README.md` section 2.1 uses `AssignHouseHandler`
as its worked example - that handler does not notify.

### 10.2 BLOCKER - no SignalR client, so live push does not exist

See section 8. The feature described as "Live notifications work while the application is
open" is not implemented on the client. Notifications do appear on window focus, which is
useful, but it is not live delivery.

### 10.3 Documentation corrected

`README.md` section 1 previously asserted "Live updates while the app is open | Yes |
SignalR over the LAN hub". That was inaccurate and has been corrected, together with an
explicit table of which business events actually raise notifications.

---

## 11. Recommended next actions

1. **Wire the business events.** Inject `INotificationDispatcher` into the Accommodation,
   Assignment, Unit and Enrolment handlers and call it *after* each handler's own
   `SaveChangesAsync`. The dispatcher already de-duplicates recipients, normalises
   type/priority, sanitises `ActionUrl` and swallows faults so a notification failure can
   never roll back the business transition. Register `RegistrationNotificationService` or
   delete it.
2. **Add the SignalR client.** Add `@microsoft/signalr`, open a `HubConnection` in the
   authenticated `Layout` alongside `useServiceWorker`, and invalidate the
   `['header-notifications']` / `['header-unread-count']` query keys on
   `ReceiveNotification`. Note that the air-gapped build must be able to resolve the
   package - verify `npm ci` inside `Dockerfile.frontend` can reach the registry, or
   vendor the package.
3. **Then** re-run this validation and deploy.

## 12. Repository actions taken

- The validated Notifications/PWA changes were committed and pushed to
  `origin/coordinator-repair` (fast-forward; **no force-push**).
- The two pre-existing unrelated untracked files under `Documentation/` were left untouched.
- Production was **not** migrated and **not** deployed.
