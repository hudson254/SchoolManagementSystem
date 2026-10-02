# Notifications and Progressive Web App

Architecture, behaviour, security model and operating procedures for the School
Management System's in-app notification system and its installable PWA client.

> **Read this first.** This deployment is **LAN-only and air-gapped**. There is no
> external push provider (no FCM, APNs, Web Push, OneSignal, Twilio, SMTP or SMS
> gateway) and none may be added. This document is explicit about which
> notification capabilities therefore exist and which do not, so that no operator
> or developer mistakes this system for something it is not.

---

## 1. What this system does and does not do

| Capability | Supported? | Notes |
| --- | --- | --- |
| Persistent in-app notifications | **Yes** | Stored in PostgreSQL `Notifications`, tenant-scoped. |
| Unread badge / unread count | **Yes** | Counted in the database. |
| Read / unread state, persisted | **Yes** | `IsRead` + `ReadAt`. |
| Notification history with paging | **Yes** | Server-side paging, page size clamped to 100. |
| Severity (Informational → Critical) | **Yes** | Rendered as text *and* colour. |
| Role / user targeting | **Yes** | Recipients resolved server-side, deduplicated. |
| SignalR hub, authenticated + claim-scoped | **Yes** | Hub requires authorization; groups come from token claims. |
| Live push into an open browser tab | **Yes (see 1.1)** | One authenticated SignalR connection per session; the client re-reads on `ReceiveNotification`. |
| Updates when the app regains focus | **Yes** | Client re-reads history on window focus. This is the fallback when the socket is down. |
| Standalone installed app (Android/Desktop) | **Yes** | Chromium install flow. |
| Standalone installed app (iOS/iPadOS) | **Yes, manually** | Share → Add to Home Screen. iOS has no install API. |
| **Background delivery to a CLOSED app** | **No** | Not achievable without an external push service. |
| **Operating-system push notifications** | **No** | Requires FCM/APNs/Web Push. Not available on an air-gapped LAN. |

### 1.1 Honest statement of the live-update path

**Live delivery (app open, socket up).** `useNotificationRealtime` opens **one**
`HubConnection` from the authenticated `Layout` — one per signed-in session, not
one per page. The server pushes `ReceiveNotification`; the client invalidates the
existing `['header-notifications']` and `['header-unread-count']` queries so the
UI re-reads authoritative server state. The payload is never inserted into a local
list, because the server has already persisted the row and the database is the
source of truth. Important/Critical items also raise a non-intrusive snackbar;
Normal items do not, so routine activity cannot become an interruption.

**Fallback (socket unavailable).** If the connection cannot be established —
negotiate fails, the hub is down, the LAN drops — the client reports state
`error`, logs a warning, and the application continues to work normally. The
notification centre and bell still read through the REST API, and
`refetchOnWindowFocus: true` remains in place. Reconnection uses bounded
`withAutomaticReconnect`; on reconnect the client re-reads notifications, because
anything raised while it was offline exists only in the database and was never
pushed.

**Do not claim OS-level background push to users.** A closed or suspended browser
process is suspended by the OS and cannot poll the LAN. The system compensates by
making everything durable and re-reading on next activation, but it cannot wake a
closed app, and it says so rather than pretending otherwise.

### 1.1a SignalR authentication posture

* The client authenticates with the **same httpOnly cookie** the REST API already
  uses (`withCredentials: true`). It sends **no** user id, **no** token and **no**
  password — no `accessTokenFactory`, so nothing secret is handed to JavaScript.
* The server remains the authority: `NotificationHub` carries `[Authorize]`, is
  mapped with `RequireAuthorization()`, and derives group membership solely from
  the validated principal's claims.
* The client exposes no group-subscription method, so the removed
  `SubscribeToNotifications(userId)` cross-user disclosure hole cannot be
  reintroduced from the browser. A test pins this.

### 1.2 Business events that raise notifications

All of the events below are wired. Each one dispatches **after** its own
`SaveChangesAsync`, so a notification can never describe a change that was then
rolled back, and a notification failure can never fail the business operation.

#### Event matrix

| Domain | Event | Recipient (resolved server-side) | Priority | Action URL |
| --- | --- | --- | --- | --- |
| Accommodation | House allocated (`AssignHouse`) | The occupant (student **or** lecturer) | Important | `/accommodation` |
| Accommodation | Room allocated (`AssignRoom`) | The student | Important | `/accommodation` |
| Accommodation | House reassigned (`ReassignHouse`) | The occupant | Important | `/accommodation` |
| Accommodation | House vacated (`VacateHouse`) | Every occupant whose assignment was closed, de-duplicated | Important | `/accommodation` |
| Assignment | Assignment published (`CreateAssignment`) | Students **enrolled in that unit** + the assigning lecturer | Important / Normal | `/assignments` |
| Assignment | Due date or status changed (`UpdateAssignment`) | Students enrolled in that unit, only on a real change | Important | `/assignments` |
| Assignment | Submission (`SubmitAssignment`) | The assignment's lecturer. **Never** the submitting student | Normal | `/assignments` |
| Assignment | Graded (`GradeAssignment`) | The student whose work it was | Important | `/assignments` |
| Unit | Unit renamed / withdrawn (`UpdateUnit`) | Students enrolled in **that** unit, only on a real change | Important | `/units` |
| Course | Course renamed / availability changed (`UpdateCourse`) | Students enrolled in **that** course, only on a real change | Important | `/courses` |
| Enrolment | Enrolled (`CreateEnrollment`) | The enrolled student | Important | `/courses` |
| Enrolment | Dropped (`DropEnrollment`) | The student | Important | `/courses` |
| Enrolment | Status changed (`UpdateEnrollmentStatus`) | The student, only on a real transition | Important | `/courses` |
| Registration | Approved / rejected | The applicant themselves, with the reason | Important | — |
| Request | OMS request submitted | Approver roles (pre-existing) | Normal | `/oms/requests` |

Scoping rules that the tests pin down:

* **No tenant-wide fan-out.** A notification never goes to "everyone". Recipients
  come from the academic relationship the event actually touched.
* **Sibling units are excluded.** `IEnrollmentRepository.GetEnrollmentsByUnitAsync`
  joins through the *course*, so it also returns students enrolled in a different
  unit of the same course. `BusinessEventNotifier` filters on the enrollment's own
  `UnitId`; without that filter one unit's assessment would leak to another unit.
* **Dropped enrollments are excluded** from unit/course fan-out.
* **Students and lecturers without a linked identity account produce nothing**,
  rather than an orphan notification addressed to a blank id.
* **No-op updates notify nobody.** Re-saving a unit/course/assignment form without
  changing anything, or setting an enrolment status to the value it already holds,
  produces no row, so a retried request cannot create duplicates.

### 1.3 Two layers, one write path

```
Business handler  ──after SaveChangesAsync──►  IBusinessEventNotifier
                                                   │  who is affected?
                                                   ▼
                                              INotificationDispatcher   ◄── the ONLY writer
                                                   │  type/priority, dedup,
                                                   │  action-URL sanitisation,
                                                   │  fault isolation
                                                   ▼
                                              CreateNotificationCommand → Notifications table
```

`INotificationDispatcher` was already the single write path but knew nothing about
the academic graph, so every call site would have had to re-derive "who is
affected" and each derivation would drift. `IBusinessEventNotifier` is the thin
semantic layer above it that owns that mapping in **one** place. It never writes a
row itself — it delegates to the dispatcher — so there is still exactly one code
path that can create a notification.

### 1.4 `RegistrationNotificationService` — removed

The previously-registered-nowhere `RegistrationNotificationService` was dead code:
not in `DependencyInjection.cs`, never injected, and therefore never invoked. It
also bypassed the dispatcher, calling MediatR's `CreateNotificationCommand`
directly, so it had **no** recipient de-duplication, **no** type/priority
normalisation, **no** action-URL sanitisation and **no** fault isolation.

It has been **deleted**, and the events it was supposed to cover are now raised by
`ApproveRegistrationCommandHandler` / `RejectRegistrationCommandHandler` through
the dispatcher. Registration outcomes therefore behave like every other
notification. See `tests/SMS.UnitTests/Notifications/BusinessEventNotifierTests.cs`
for the covering tests.

---

## 2. Notification architecture

### 2.1 Components

```
Business handler (e.g. AssignHouseHandler)
        │  AFTER its own SaveChangesAsync
        ▼
INotificationDispatcher          SMS.Application/Common/Interfaces
        │  resolves recipients, normalises type/priority,
        │  sanitises action URL, swallows + logs faults
        ▼
CreateNotificationCommand        MediatR CQRS (existing pattern)
        │
        ▼
INotificationRepository          SMS.Persistence
        │  persists; TenantId stamped by DbContext
        ▼
PostgreSQL "Notifications"       tenant query filter + RLS
        │
        ▼
INotificationRealtimePublisher   SMS.Notifications (optional collaborator)
        │
        ▼
NotificationHub (SignalR)        SMS.Notifications, [Authorize]
        │
        ▼
Browser: refetch /notifications/unread-count + ReceiveNotification
```

**Durability first, live second.** `CreateNotificationCommand` always commits the row
*before* any push is attempted. A push failure is logged and ignored, so "delivered
live" and "recorded in history" can never disagree, and no notification is lost
because a browser was closed.

### 2.2 Why the dispatcher exists

Business handlers must never raise a notification by talking to the repository or to
the MediatR command directly. `INotificationDispatcher` gives one place for:

* **Recipient resolution** — user, role-set, or self; always de-duplicated
  (a user in two roles receives exactly one notification).
* **Normalisation** — type and priority come from a fixed catalogue, so a caller
  cannot invent a value that breaks rendering or the unread-badge query.
* **Action-URL sanitisation** — see §5.2.
* **Fault isolation** — a notification failure can never abort a business
  operation that has already committed.

### 2.3 Data model

`Notifications` rows carry: `UserId` (owner), `Title`, `Message`, `Type`,
`Priority`, `ReferenceId`, `ActionUrl`, `ExpiresAt`, `IsRead`, `ReadAt`, plus the
`BaseEntity` audit/tenancy columns.

`TenantId` is stamped by `ApplicationDbContext.SaveChangesAsync` and is policed by
PostgreSQL RLS, so a notification is only ever visible inside the tenant that
produced it. See §6.

---

## 3. Notification types and priorities

`src/SMS.Domain/Notifications/NotificationTypes.cs` and `NotificationPriorities.cs`.

| Priority | Meaning | Default types |
| --- | --- | --- |
| `Informational` | Routine, no action implied | Enrollment, Course, Unit, AssignmentSubmission, Certificate |
| `Normal` | The user should be aware | Accommodation, PermissionRequest, Request, OmsRequest |
| `Important` | Action expected or a deadline attached | AssignmentIssue, AccountApproval, Announcement |
| `Critical` | Immediate attention | Security, Maintenance |

Unknown/blank values degrade to `Normal` rather than throwing, so a legacy or
hand-edited row can never break the UI or the unread-badge query.

### Supported categories

Academic — course/unit enrollment, assignment creation and submission, assignment
issues, lecture notes, grades, certificates, announcements.
Accommodation — allocation, reassignment, check-in/out, lane and house changes.
Workflow — permission and OMS requests, and their status transitions.
Identity — registration, account approval, password-reset and other security events.
System — maintenance and operational announcements.

### Avoiding spam

Notifications represent events that require **user awareness or action**. Do not
notify on every database operation. The dispatcher de-duplicates recipients and caps
a single fan-out at `NotificationDispatcher.MaxRecipients` (5000), and it emits
nothing for a blank title, so a mis-wired call site fails quietly rather than
flooding every user.

---

## 4. Real-time behaviour (while the app is open)

`NotificationHub` is mapped at `/hub` and **requires authorization**. A connection is
added to `user_{sub}` and to a `role_{role}` group derived **solely from the
validated token's claims**. There is no client-supplied user id anywhere in the hub.

> This endpoint was previously mapped with no authorization at all (and the host
> sets no `FallbackPolicy`), and the hub exposed
> `SubscribeToNotifications(userId)` — so any caller, including an anonymous one,
> could join any user's group and read their live notification stream. Both were
> removed; see `NotificationRegistrationTests`.

If the hub is unavailable the application still works completely: history, unread
counts and read state are served from PostgreSQL.

---

## 5. Security

### 5.1 Ownership and tenant isolation

Every read/write resolves the owner from the authenticated principal. A
notification belonging to another user is reported as **404, never 403**, so the
endpoints cannot be used to probe for the existence of another user's ids.

| Surface | Before | Now |
| --- | --- | --- |
| `GET /notifications/{id}` | `GetByIdAsync(id)` — any user could read any notification | `GetForUserAsync(id, caller)` |
| `POST /notifications/{id}/read` | `MarkAsReadAsync(id)` — any user could mutate any | `MarkAsReadForUserAsync(id, caller)` |
| `DELETE /notifications/{id}` | `DeleteAsync(byId)` — any user could delete any | `DeleteForUserAsync(id, caller)` (soft delete) |
| `GET /notifications` | Client-supplied `UserId` on the query | Principal only |
| `POST /notifications/read-all` | Client-supplied `UserId` on the command | Principal only |

Tenant isolation is layered: the global query filter on `ITenantAwareEntity`
(Notification implements it) hides other tenants' rows, PostgreSQL RLS is a second
independent boundary, and ownership is the third.

### 5.2 Action URLs are hints, never authorization

`ActionUrl` is validated to an **application-relative path only**
(`NotificationCatalog.NormalizeActionUrl`). It rejects absolute URLs,
protocol-relative `//host`, any scheme (`javascript:`, `data:`), backslashes (which
browsers normalise to `/`), traversal segments, control characters, fragments and
queries. It is sanitised on write **and** re-checked on read and again in the
browser.

Even when valid, following an action URL grants nothing: the destination page and
its API calls remain the authorization and tenant-isolation boundary.

### 5.3 Service worker safety

**The service worker never stores a response that could contain private data.**
Caching is allowlist-based: only `/assets/*`, `/icons/*` and `/logo.png` are
eligible. `/api/**`, `/hub/**`, `/uploads/**`, `/sw.js` and `/manifest.json` are
never cached or served from cache.

> The previous worker used cache-first for **every** GET, which cached notification
> lists, student records and auth responses into a cache shared across users on the
> same browser profile. One user could have been served another user's data, and a
> cached `/auth/me` could have resurrected a session after logout.

---

## 5a. Frontend dependency and the air-gapped build

**This section is the record of how `@microsoft/signalr` reaches a production
build on a LAN-only, air-gapped network.**

### 5a.1 What changed

`frontend/sms-web/package.json` gains exactly one dependency:

```json
"@microsoft/signalr": "^8.0.7"
```

and `package-lock.json` gains the corresponding entries, each with an `integrity`
SHA-512 hash. Pinned to 8.x deliberately: it is the version line that targets the
same modern browsers this PWA already supports, and it is the current maintained
release at the time of writing.

### 5a.2 How the production build obtains it

The production frontend image is built by `docker/Dockerfile.frontend`, which
copies `frontend/sms-web/package*.json` and runs:

```dockerfile
RUN npm ci
```

`npm ci` installs **strictly from the committed lockfile**. That is the existing,
already-approved controlled dependency mechanism for this repository — it is the
same command, in the same file, that already installs the other 430 packages.
`@microsoft/signalr` and its transitive dependencies are now pinned there with
integrity hashes exactly like the rest of the tree, so the build is reproducible
and a tampered tarball fails verification.

**No new mechanism was introduced, no CDN was added, and nothing is loaded from
an external URL at runtime.** The SignalR client is bundled by Vite into
`/assets/*.js` and served from the same nginx origin as the rest of the app.

### 5a.3 Verifying it resolves without internet access

The deployment host is air-gapped, so the registry cannot be assumed reachable.
Before deploying, confirm the build works with no egress:

```bash
# From the deployment host, with the WAN link down:
docker compose -f docker/docker-compose.prod.yml build frontend
```

If the host has a pre-seeded npm cache or an internal mirror, `npm ci` will resolve
from it. **If it does not**, the approved remedy is to prime the same lockfile on a
connected machine and transfer the artifacts, not to weaken the build:

```bash
# ON A CONNECTED MACHINE (produces a verifiable tarball set)
cd frontend/sms-web
npm ci
npm pack @microsoft/signalr@8.0.7        # or: npm cache verify / npm ci --offline
```

The tarball's SHA-512 must equal the `integrity` value recorded in
`package-lock.json` before it is transferred. The deployment host then installs
from that local copy:

```bash
npm ci --offline --cache /path/to/seeded/npm-cache
```

Because the lockfile pins both version and integrity, an offline install is
byte-for-byte the same artifact a connected build would produce. **Do not** relax
`npm ci` to `npm install`, delete the lockfile entry, or point the registry at an
external mirror — any of those trades an air-gap guarantee for convenience.

### 5a.4 Why the client cannot work without it

`@microsoft/signalr` is the official Microsoft client and implements the
negotiate + WebSocket handshake against the already-secured `NotificationHub`.
Writing that protocol by hand would mean re-implementing transport framing,
reconnection and message parsing — a large, security-sensitive surface — for no
benefit. The package is therefore a hard build-time dependency, and its
availability is a **deployment gate** (see §9.4).

---

## 6. Database migration

`20261002094649_AddNotificationPriorityActionUrlExpiry` adds `Priority`
(NOT NULL, default `'Normal'`), `ActionUrl`, `ExpiresAt` and a
`(UserId, IsRead, Priority)` index.

It is **purely additive**, idempotent, and backward compatible:

* no existing column is narrowed (narrowing `text` → `varchar` would fail outright
  if any historical row exceeded the new limit);
* `Priority` is added with a real default and then repaired with an idempotent
  `UPDATE` to the four catalogue values, so historical rows are visible to the
  "unresolved Important/Critical" check;
* `Down` simply drops the added columns.

Apply to a clone first, per §9.

---

## 7. PWA architecture

### 7.1 Manifest

`frontend/sms-web/public/manifest.json`. Correct `192x192`, `512x512`, maskable
variants and an Apple touch icon, explicit `id`, `scope`, `start_url`, `display:
standalone`, and `theme_color` matching `theme.ts`.

> The previous manifest declared the single `595x420` `logo.png` as **both** a
> `192x192` and a `512x512` icon. Neither was true, and Chrome validates the declared
> size against the real bitmap, so the app was **never installable**.

Icons are generated with `node scripts/generate-pwa-icons.mjs`, which uses only
Node's built-in `zlib` (no `canvas`/`sharp`, keeping the LAN-only and
no-new-dependencies rules). Maskable variants inset the logo into a padded square on
an opaque brand background so a launcher crop cannot clip it.

### 7.2 Service worker

`frontend/sms-web/public/sw.js`:

* **Navigations** — network-first with an offline shell fallback. A deployed
  frontend is picked up on the first load, not the second.
* **`/assets/*`** — cache-first, safe because Vite content-hashes filenames.
* **Everything else** — network, uncached (§5.3).
* **Cache versioning** — `CACHE_NAME` is a build id stamped at build time by the
  `sms-sw-version` Vite plugin, derived from a hash of the built `index.html`
  **plus the emitted asset filenames** (so a change confined to a lazily-loaded
  chunk still changes the version). `activate` deletes every cache that is not the
  current build, invalidating the previous release in one step.

> Previously the name was the constant `sms-cache-v1`, identical for every
> deployment, so a new release shared the old cache and `activate` had nothing to
> delete. Combined with nginx serving `sw.js` as `expires 1y; immutable`, the
> browser pinned the first worker it ever downloaded and **a deployed update could
> never reach an installed user**. Both are fixed: `docker/nginx.conf` and
> `docker/nginx-frontend.conf` now send `no-cache` for `/sw.js`, `/manifest.json`
> and `/index.html`.

### 7.3 Installation UX

`usePwaInstall` + `components/Pwa/InstallPrompt.tsx`, mounted in the authenticated
`Layout` (so it never appears on the login screen).

* Chromium: captures `beforeinstallprompt` and calls the real native dialog.
* iOS/iPadOS: **no programmatic install API exists**; the banner shows accurate
  Share → Add to Home Screen steps rather than a button that cannot work.
* Dismissal is persisted in `localStorage` and honoured across visits; the
  instructions remain reachable from the header so a user who declined once is not
  locked out.
* The banner disappears permanently once installed (detected via
  `display-mode: standalone` and `navigator.standalone`).
* Copy: *"Install the School Management System on your device for faster access and
  important system notifications."*

### 7.4 Update handling

`useServiceWorker` registers `/sw.js`, surfaces a waiting worker, and the
`PwaUpdateBanner` offers **Reload**, which posts `SKIP_WAITING` and reloads. The
worker deliberately does **not** call `skipWaiting()` during install, so a new
version never swaps out from under an active session unannounced.

---

## 8. Notification centre (UI)

* **Header bell** — unread badge (`max={99}`), panel sorted most-severe-first,
  severity shown as text, click marks read and follows the action target.
* **`/notifications`** — full history, All/Unread/Read filter, mark-one, mark-all,
  delete, bounded paging, per-row action button.
* **Refresh** — `refetchOnWindowFocus`, so a notification raised while the window was
  in the background appears without a manual reload. On a LAN-only deployment this
  is the practical substitute for push.

---

## 9. Deployment and testing

### 9.1 Before deploying

```bash
dotnet build SchoolManagementSystem.sln -c Release
dotnet test  tests/SMS.UnitTests/SMS.UnitTests.csproj -c Release
dotnet test  tests/SMS.ApiTests/SMS.ApiTests.csproj   -c Release
dotnet test  tests/SMS.IntegrationTests -c Release          # needs PostgreSQL
cd frontend/sms-web && npm run build && npx vitest run
```

Apply the migration to a **clone** of production first:

```bash
dotnet ef database update --project src/SMS.Persistence --startup-project src/SMS.API
```

### 9.2 Manual smoke test

1. Log in as a **Student**. Generate an accommodation allocation.
2. Confirm the notification appears in the bell and at `/notifications`.
3. Confirm it is unread (badge + "New" chip).
4. Open the action button; confirm you land on the Accommodation page.
5. Confirm the item is now read and the badge decremented.
6. Log in as a **Lecturer** in a second browser profile and generate a lecturer
   notification. Confirm the Student profile cannot see it.
7. `POST /api/v1/notifications/{id}/read` as the second user → expect **404**, never 403.
8. **Install** the PWA (Chromium) → confirm standalone launch and correct icon.
9. Close the browser, reopen the installed app, authenticate, confirm the
   notification centre is reachable.
10. Deploy a frontend change → confirm the **Reload** banner appears and the new
    build loads.
11. Repeat steps 8–10 with the LAN **disconnected from the internet** (the server
    itself still reachable). Confirm the shell loads and the API still works.

### 9.3 Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| No install prompt | Needs HTTPS, a valid manifest, a registered worker, and Chromium. Check `chrome://app-internals`. |
| Install prompt never appears on iOS | Expected — iOS has no install API; use Share → Add to Home Screen. |
| Stuck on an old build | Confirm `/sw.js` is served `no-cache`; check `CACHE_NAME` changed between builds. |
| Notifications show "Invalid Date" | API/client field mismatch; both `createdAt` and `createdDate` are published. |
| 404 on another user's notification | **Correct.** That is the ownership boundary working. |
| 401/403 on `/hub` | Expected for an unauthenticated client. |
| Badge not updating while app is closed | **By design.** No push service exists; it updates on next activation. |

---

## 10. Files

| Area | Path |
| --- | --- |
| Catalogue / priorities / validation | `src/SMS.Domain/Notifications/` |
| Entity | `src/SMS.Domain/Entities/Notification.cs` |
| Repository | `src/SMS.Persistence/Repositories/NotificationRepository.cs` |
| Dispatcher (write path) | `src/SMS.Application/Services/NotificationDispatcher.cs` |
| Handlers + mapper | `src/SMS.Application/Features/Notifications/` |
| API | `src/SMS.API/Controllers/v1/NotificationController.cs` |
| Hub / publisher | `src/SMS.Notifications/` |
| Migration | `src/SMS.Persistence/Migrations/20261002094649_AddNotificationPriorityActionUrlExpiry.cs` |
| Manifest / worker / icons | `frontend/sms-web/public/` |
| Install + update hooks | `frontend/sms-web/src/hooks/usePwaInstall.ts`, `useServiceWorker.ts` |
| Install / update UI | `frontend/sms-web/src/components/Pwa/InstallPrompt.tsx` |
| Notification UI | `frontend/sms-web/src/pages/Notifications.tsx`, `components/Layout/Header.tsx` |
| Presentation helpers | `frontend/sms-web/src/utils/notifications.ts` |
| Tests | `tests/SMS.UnitTests/Notifications/`, `frontend/sms-web/src/**/*.test.tsx` |