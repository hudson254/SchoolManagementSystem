# Final SignalR Repair and Nginx Hardening — Deployment Report

**System:** School Management System (SMS)
**Host:** `sms-server` (192.168.110.161), `/opt/sms/app`
**Report date:** 2026-10-02
**Scope:** repair of the two browser SignalR defects and the nginx stale-configuration defect left open by the `997e44c` deployment.

---

## 1. Baseline

| Item | Value |
|---|---|
| Previous production SHA | `997e44cf06b36dc88ebaab5ff9179ff0f20674d8` |
| Branch | `coordinator-repair` |
| Local / GitHub / production SHA at start | **all identical** at `997e44c` |

The deployed baseline was left untouched. All three fixes were validated and
committed as a new commit; production deployment is recorded in §11.

---

## 2. What was wrong — verified, not assumed

Each defect was reproduced against the live system **before** any code was changed.

### Defect 1 — CSRF blocked SignalR negotiation

`notificationHub.ts` called `withUrl(url, { withCredentials: true })` with no
headers. `POST /hub/negotiate` is a state-changing request on a cookie-authenticated
session, so `CsrfProtectionMiddleware` requires the double-submit token:

```
POST /hub/negotiate  -> 403 {"error":"CSRF validation failed: missing token"}
```

### Defect 2 — `/hub` returned `301`

This was **not** the API redirecting, and **not** a missing `/hub/` block. Measured
directly against production:

```
GET /hub        (via nginx)     -> 301  location: https://127.0.0.1/hub/
GET /hub?id=x   (via nginx)     -> 301  location: https://127.0.0.1/hub/?id=x
GET /hub        (direct to API) -> 401   <- endpoint matched, auth required
GET /hub/       (direct to API) -> 401   <- endpoint matched, auth required
```

The API serves `/hub`, `/hub/` and `/hub/negotiate` correctly. The redirect was
produced by **nginx**, in two compounding ways:

1. `location /hub/` does **not** match the path `/hub`, so the request fell through
   to the SPA `location /` block.
2. That block runs `try_files $uri $uri/`. The `$uri/` form makes nginx treat the
   path as a directory and answer `301` with a trailing slash.

Confirmed by isolation: inside `sms-web`, `/hub` → 301 and `/uploads` → 301 (both
are prefix locations), while `/hubfoo` → 200 (no location match, falls through).

A WebSocket handshake cannot follow an HTTP redirect, so the socket never opened
even once the CSRF header was added.

### Defect 3 — nginx bind-mounted as a single file

`docker-compose.prod.yml` mounted `./nginx.conf:/etc/nginx/nginx.conf:ro`. A
single-file bind mount is resolved to **one inode** at container creation.
`git checkout` *replaces* the file (new inode), so the container kept serving the
previous configuration — the real stale-config condition recorded in the previous
deployment report.

---
## 3. CSRF

### Existing mechanism (reused, not duplicated)

| Aspect | Value |
|---|---|
| Pattern | Double-submit cookie (RISK-10) |
| Cookie | `XSRF-TOKEN`, **non-httpOnly** (JS must read it), `SameSite=Lax` |
| Header | `X-CSRF-TOKEN` |
| Enforcement | state-changing methods on `access_token`-cookie sessions |
| Comparison | `CryptographicOperations.FixedTimeEquals` (constant-time) |

The Axios client already attached the header for REST. The SignalR client did not.
Rather than add a second mechanism, the reader was extracted to
**`frontend/sms-web/src/utils/csrf.ts`** and is now shared by `api.ts` and
`notificationHub.ts`, so the cookie name, header name and parsing can never drift.

### Repair

`notificationHub.ts` now sends `X-CSRF-TOKEN` from that existing cookie.

**A correction to the original plan, found during implementation.** The brief
warned not to assume a custom header applies to every transport request. Both
concerns proved real:

- `IHttpConnectionOptions.headers` in **@microsoft/signalr 8.0.7 is a plain object
  and cannot be a getter function.** The first implementation used a function and
  failed to compile (`TS2769`). It was corrected.
- Browsers cannot set headers on a WebSocket handshake, and SignalR's own type
  documentation states headers do not apply to WebSockets/SSE.

That second point is **not** a gap, and this is the key transport analysis:

| Request | Method | CSRF-validated? | Header needed? |
|---|---|---|---|
| `/hub/negotiate` | POST | **Yes** | **Yes — now sent** |
| WebSocket upgrade `/hub?id=…` | GET | No (GET is a safe method) | No — cookie suffices |
| Server-Sent Events stream | GET | No | No |

So negotiation (the request that was actually failing) now carries the header, and
the WebSocket upgrade is authenticated purely by the httpOnly cookie the browser
attaches automatically for a same-origin URL.

**Token freshness.** SignalR spreads `this._options.headers` into every negotiate
(`HttpConnection._getNegotiationResponse`), so a single long-lived object is re-read
on each negotiation. The client therefore keeps one header object and **refreshes it
in place** before starting and before every reconnect. A value captured once at
build time would leave the header permanently absent on a cold session, where the
cookie is only issued by the server's first response.

### Negative test

`CsrfMiddleware_DoesNotExemptTheHubNegotiationEndpoint` asserts the middleware
source contains neither `/hub` nor `negotiate`. **The hub was not exempted.**
CSRF protection is unchanged and still rejects an un-headered negotiate.

---

## 4. Hub routing

### Canonical URL

**`/hub`** — no trailing slash. This matches `app.MapHub<NotificationHub>("/hub")`
exactly, so backend, nginx and client now agree on one canonical endpoint.

| Component | Before | After |
|---|---|---|
| `Program.cs` | `MapHub<NotificationHub>("/hub").RequireAuthorization()` | unchanged |
| `resolveHubUrl()` | `/hub` | `/hub` (+ trailing slash stripped from any override) |
| edge nginx | `location /hub/` → **301** for `/hub` | `location ^~ /hub` |
| frontend nginx | `location /hub/` → **301** for `/hub` | `location ^~ /hub` |

`^~` matches `/hub`, `/hub/`, `/hub/negotiate` and `/hub?id=…`, and the upstream is
given **no URI part** so the original path and query string pass through unchanged.
This is a genuine fix, not a client-side URL guess, and not a hidden redirect.

### nginx WebSocket configuration

- `proxy_http_version 1.1`
- `proxy_set_header Upgrade $http_upgrade`
- `proxy_set_header Connection $connection_upgrade` — via a new `map`, so a plain
  negotiate POST is no longer falsely advertised as an upgrade (the old config sent
  a hard-coded `Connection: upgrade` on every hub request)
- `proxy_buffering off`
- `proxy_read_timeout 3600s` / `proxy_send_timeout 3600s` — a notification socket is
  idle by design; the previous 300s would sever healthy connections
- **Scoped to the hub only.** No other location gained WebSocket behaviour;
  `Nginx_DoesNotScopeWebSocketProxyingBeyondTheHub` enforces this.

### Configuration validation

Both configurations were validated with the real `nginx:1.27-alpine` binary:

```
docker/nginx/nginx.conf        -> syntax is ok / test is successful
docker/nginx-frontend.conf     -> syntax is ok / test is successful
```

The only warning is the pre-existing `listen ... http2` deprecation, unrelated to
this work and present before these changes.
---

## 5. Security — nothing weakened

Every protection from `997e44c` is intact. No exemption, bypass or relaxation was
introduced.

| Control | Status |
|---|---|
| `[Authorize]` on the hub | intact |
| `RequireAuthorization()` on the endpoint | intact |
| Anonymous `/hub/negotiate` → 401 | unchanged |
| Claim-derived `Context.UserIdentifier` | unchanged |
| No client-supplied user id / group subscription | unchanged |
| Notification ownership + 404 for cross-user | unchanged |
| Tenant isolation + RLS | unchanged (120/120 integration tests pass) |
| CSRF middleware present and enforced | intact — **not** disabled, **not** exempted |
| Service-worker API/hub/uploads exclusions | unchanged |

No `accessTokenFactory`, no access token in JavaScript, no hard-coded CSRF token,
and no token value is ever logged.

---

## 6. Nginx bind-mount fix

| | Before | After |
|---|---|---|
| Mount | `./nginx.conf:/etc/nginx/nginx.conf:ro` | `./nginx:/etc/nginx/sms:ro` |
| Start | image default | `nginx -c /etc/nginx/sms/nginx.conf` |
| Health | none | `nginx -t` |

A **directory** mount re-resolves the file on every read, so `git checkout` can no
longer leave nginx on an obsolete inode.

**Proven, not assumed.** A container was started with the directory mount, then the
mounted file was rewritten in place (new inode). Without any restart or recreate, the
running container immediately saw the new content and `nginx -t` succeeded against
it. That is the exact failure mode that broke the previous deployment.

Applied consistently to `docker-compose.prod.yml`, `docker-compose.yml` and
`docker-compose.dev.yml`.

### Deployment verification step

**`scripts/verify-nginx-config.sh`** (new, wired into `scripts/deploy.sh`) performs
the six required steps and fails fast:

1. host file checksum
2. in-container checksum — **mismatch is reported as `STALE CONFIGURATION` and
   nothing is reloaded**, printing the exact recreate command
3. `nginx -t` — before touching the live process
4. `nginx -s reload`
5. process health after reload
6. externally observable behaviour: `/hub` must **not** return 301/302,
   `/hub/negotiate` must be reachable, and `/sw.js` must not be `immutable`

---

## 7. PWA and service worker — unaffected

Verified in the built output:

| Check | Result |
|---|---|
| `CACHE_NAME` stamped | `sms-shell-foa7q8` (content-derived, changes per build) |
| `/sw.js` cache header | `no-cache, no-store, must-revalidate` — not `public, immutable` |
| `/api/` excluded from worker caching | intact |
| `/hub/` excluded | intact |
| `/uploads/` excluded | intact |
| WebSocket interception | none — `request.mode === 'websocket'` returns early |
| SignalR bundled locally | yes — `vendor-CkjxHP5c.js`; **no CDN** |

The SignalR repair was solved in the client and the proxy; no service-worker caching
rule was altered.

---
## 8. Testing

### Passed

| Suite | Result |
|---|---|
| Solution build (`Release`) | **0 errors**, 75 warnings (pre-existing) |
| `SMS.UnitTests` | **952 / 952** passed |
| `SMS.ApiTests` | **222 / 223** (1 pre-existing failure) |
| `SMS.IntegrationTests` (RLS / tenant) | **120 / 120** passed |
| Frontend build | **succeeded** (1m 20s) |
| Frontend `tsc --noEmit` | **0 errors** |
| Notification tests | **63 / 63** passed |
| PWA tests | **11 / 11** passed |
| `nginx -t` (edge + frontend) | **both successful** |

### New tests added

**Frontend — `notificationHub.test.ts` (+11) and `csrf.test.ts` (new, 12)**

- CSRF: header is sent from the existing cookie; omitted when absent; re-read
  lazily so a late-issued token is picked up; URL-decoded to match the server
  comparison; **no `Authorization` header**; **no hard-coded token**; refreshed
  **in place** (object identity preserved); **deleted** rather than blanked when
  the cookie disappears
- URL: exactly `/hub`; never trailing-slash; relative/same-origin; a
  `VITE_HUB_URL=/hub/` override is normalised
- Lifecycle, `ReceiveNotification` invalidation of `['header-notifications']` and
  `['header-unread-count']`, reconnect refresh, logout stop, and REST fallback on
  failure were already covered and still pass.

**Backend — `NotificationHubRoutingTests.cs` (new, 27)**

- hub mapped at `/hub` **with** `RequireAuthorization()`; never at `/hub/`
- both nginx configs route the hub via `^~`, contain **no redirect**, forward the
  upgrade with the `$connection_upgrade` map, keep ≥3600s timeouts, and scope
  WebSocket proxying to the hub alone
- service worker still revalidated and never `immutable`
- compose files mount the **directory**, not the file, and start nginx with `-c`
- CSRF middleware does **not** exempt the hub, exempts only anonymous auth paths,
  and keeps its constant-time comparison

**Regression proof.** The routing test was proven to actually catch the defect: with
`location ^~ /hub` temporarily reverted to `location /hub/`,
`Nginx_RoutesTheHubWithoutRequiringATrailingSlash` failed; it passed again on
restore.

### Pre-existing failures (not regressions)

| Test | Status |
|---|---|
| `AccommodationReportDateApiTests.OccupancyHistory_BoundaryDates_…` | **pre-existing**, unrelated, unchanged by this work |
| `Register.test.tsx` (strong password) | **pre-existing** 5s timeout under parallel load; passes in isolation |

### Environment limitations

- Under full-suite parallel load on this workstation, 7 tests timed out at 5000 ms
  (`AccommodationReports`, `OmsOrderDetail`, `OmsOrders`, `RequestsList`,
  `Register`×3). **All pass in isolation** (verified individually). This is machine
  contention — the run took 620s with 5894s of import time — not a code regression.
- These same files were already documented as flaky under parallel load.

### Not tested here

- Physical device PWA installation (unchanged self-signed TLS limitation remains
  documented; TLS validation was **not** weakened to work around it)
- Long-duration reconnect soak

---

## 9. Files changed

| File | Change |
|---|---|
| `frontend/sms-web/src/utils/csrf.ts` | **new** — shared double-submit reader |
| `frontend/sms-web/src/utils/csrf.test.ts` | **new** — 12 tests |
| `frontend/sms-web/src/services/notificationHub.ts` | CSRF header + canonical URL + refresh-on-reconnect |
| `frontend/sms-web/src/services/notificationHub.test.ts` | +11 tests |
| `frontend/sms-web/src/services/api.ts` | consume the shared helper (no behaviour change) |
| `docker/nginx/nginx.conf` | `^~ /hub`, `map`, no redirect (renamed from `docker/nginx.conf`) |
| `docker/nginx-frontend.conf` | same |
| `docker/docker-compose.prod.yml` / `.yml` / `.dev.yml` | directory mount + `-c` + healthcheck |
| `scripts/verify-nginx-config.sh` | **new** — 6-step verification |
| `scripts/deploy.sh` | runs the verification |
| `tests/.../NotificationHubRoutingTests.cs` | **new** — 27 tests |

---

## 10. Security model preserved — explicit statement

- `/hub/negotiate` was **not** exempted from authentication.
- CSRF was **not** disabled and the middleware was **not** removed.
- `/hub` was **not** made anonymous.
- **No** user id is accepted from the JavaScript client.
- Authentication remains the httpOnly cookie; no token is exposed to JavaScript.
- No secret is hard-coded or logged.

---

## 11. Deployment record

| Field | Value |
|---|---|
| Previous production SHA | `997e44cf06b36dc88ebaab5ff9179ff0f20674d8` |
| New production SHA | _(pending deployment)_ |
| Deployment timestamp | _(pending deployment)_ |
| Database migration required | **No** — no schema change; no migration was manufactured |
| Database backup | Not required (no migration involved) |

---

## 12. Outstanding limitation

The **browser end-to-end acceptance test** is the only remaining step:

```
Authenticated browser -> CSRF-protected negotiate -> direct /hub -> WSS
  -> authenticated connection -> business event -> persisted notification
  -> ReceiveNotification -> React Query invalidation -> visible notification
```

Server-side SignalR behaviour, group isolation, CSRF enforcement and the routing
configuration are all verified. What is **not** yet proven from this workstation is
the live browser handshake against production, and that must not be claimed until it
is observed.