/* eslint-env serviceworker */
/**
 * School Management System - service worker.
 *
 * ============================ SECURITY MODEL =============================
 * This worker NEVER stores a response that could contain private data.
 *
 * The previous version used cache-first for every GET request. That included
 * /api/v1/** , so notification lists, student records, grades and auth responses
 * were written into a cache that is shared by every consumer of the origin and is
 * not partitioned by user. Two concrete failures followed:
 *   - a user who signed out and another who signed in on the same browser could be
 *     served the previous user's cached notification/student payloads;
 *   - after logout a cached /auth/me response could resurrect a stale session.
 *
 * The rules below are therefore allowlist-based, never denylist-based: a request
 * is cached ONLY if it matches a known-public static asset pattern. Anything else
 * - including every /api/ path, the SignalR hub, uploads and auth endpoints -
 * falls through to the network untouched. Adding a new API route can therefore
 * never accidentally start leaking it into a cache.
 *
 * ============================= CACHE STRATEGY =============================
 * - Navigations: network-first with an offline shell fallback. Network-first is
 *   required so a deployed frontend is picked up immediately rather than after a
 *   second visit; the shell fallback exists only so a cold offline launch shows the
 *   app rather than the browser error page.
 * - Hashed build assets (/assets/*): cache-first. Vite emits content-hashed
 *   filenames, so a changed file is a different URL and can never be stale.
 * - /sw.js and /manifest.json: always network, never cached. nginx previously sent
 *   sw.js with `expires 1y; immutable`, which pinned the browser to one worker
 *   forever; that header has been removed and the worker no longer caches itself.
 * - CACHE_NAME embeds a build id, so a new deployment starts a fresh cache and the
 *   activate step deletes the previous one, invalidating the old release in one step.
 *
 * ============================= LAN-ONLY NOTE ===============================
 * No external CDN and no analytics. Offline support means "the app shell loads and
 * static assets are served locally"; it does NOT mean the API is reachable without
 * the server. There is no push service, so the service worker cannot deliver
 * notifications to a closed application - see Documentation/Notifications-PWA.
 */

// The build replaces the token below with `const CACHE_NAME = 'sms-shell-<hash>';`
// (see the sms-sw-version plugin in vite.config.ts). It MUST NOT be preceded by a
// `//` comment marker: the emitted declaration is a statement, and commenting it out
// leaves CACHE_NAME undefined, which throws the moment the worker handles a fetch.
__SW_VERSION__

const SHELL_URL = '/index.html';

/**
 * True when the request is a same-origin static asset we are willing to store.
 * Explicitly false for /api/, /hub/, /uploads/ and anything non-GET.
 */
function isCacheableStaticAsset(url) {
  if (url.origin !== self.location.origin) return false;

  // Never store anything dynamic or private.
  if (url.pathname.startsWith('/api/')) return false;
  if (url.pathname.startsWith('/hub/')) return false;
  if (url.pathname.startsWith('/uploads/')) return false;
  if (url.pathname === '/sw.js' || url.pathname === '/manifest.json') return false;

  // Vite emits content-hashed build output here.
  if (url.pathname.startsWith('/assets/')) return true;

  // Icons and the logo are public, immutable build output.
  if (url.pathname.startsWith('/icons/')) return true;
  if (url.pathname === '/logo.png') return true;

  return false;
}

self.addEventListener('install', (event) => {
  // Precache only the shell: public HTML containing no user data.
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.add(new Request(SHELL_URL, { cache: 'reload' })))
  );
  // skipWaiting() is NOT called here. It is deferred until the page explicitly asks
  // via the SKIP_WAITING message, so a newly deployed version never swaps the
  // worker out from under a user mid-session without them being told.
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      // Drop every cache that is not this build's. This is what invalidates the
      // previous deployment in a single step.
      const keys = await caches.keys();
      await Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)));

      await self.clients.claim();
    })()
  );
});

// Lets the page ask us to activate immediately (used by the update prompt).
self.addEventListener('message', (event) => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
});

self.addEventListener('fetch', (event) => {
  const request = event.request;

  // Only GET is ever considered; state-changing verbs must always hit the network.
  if (request.method !== 'GET') return;

  const url = new URL(request.url);

  // SignalR negotiation/transport and anything cross-origin: straight to network.
  // Explicitly not handled so the WebSocket upgrade is never intercepted.
  if (request.mode === 'websocket') return;
  if (url.origin !== self.location.origin) return;

  // ---- Navigations: network-first, offline shell fallback ----
  if (request.mode === 'navigate') {
    event.respondWith(
      (async () => {
        try {
          // Always revalidate. This is how a user escapes an old frontend build
          // immediately after a deployment rather than on a later visit.
          const response = await fetch(request);
          if (response && response.ok) {
            const cache = await caches.open(CACHE_NAME);
            await cache.put(SHELL_URL, response.clone());
          }
          return response;
        } catch (err) {
          const cached = await caches.match(SHELL_URL);
          if (cached) return cached;
          throw err;
        }
      })()
    );
    return;
  }

  // ---- Everything else: cache ONLY allowlisted public static assets ----
  // Anything not matched here (all of /api/, /hub/, /uploads/, auth) simply falls
  // through to the browser's default network handling: never cached, never read.
  if (!isCacheableStaticAsset(url)) return;

  event.respondWith(
    (async () => {
      const cached = await caches.match(request);

      // Hashed assets: cache-first is safe because a content change changes the URL.
      if (cached) {
        // Refresh in the background so a corrected asset lands on the next load
        // without making the current load wait for the network.
        event.waitUntil(
          fetch(request)
            .then((response) => {
              if (response && response.ok) {
                return caches.open(CACHE_NAME).then((cache) => cache.put(request, response));
              }
              return undefined;
            })
            .catch(() => undefined)
        );
        return cached;
      }

      const response = await fetch(request);
      if (response && response.ok) {
        const cache = await caches.open(CACHE_NAME);
        await cache.put(request, response.clone());
      }
      return response;
    })()
  );
});
