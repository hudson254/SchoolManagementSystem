import { LogLevel, HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { CSRF_HEADER_NAME, getCsrfToken } from '../utils/csrf';

/**
 * Single authenticated SignalR connection for the notification hub.
 *
 * WHY ONE CONNECTION, NOT ONE PER PAGE
 * The hub groups every connection by the authenticated principal's claims. One
 * connection per session means one websocket, one negotiate, and one membership
 * set. A connection per page would multiply sockets, multiply reconnects, and
 * multiply duplicate event handlers for no benefit.
 *
 * WHY NO USER ID IS SENT
 * The client never transmits a user id, token, or password. Authentication is the
 * SAME httpOnly cookie the rest of the app already uses (`withCredentials`), so
 * the browser attaches it to the negotiate + WebSocket handshake automatically
 * and the server derives identity from the validated principal.
 * `accessTokenFactory` therefore returns an empty string: sending a token from JS
 * would move a secret into a place where any XSS could read it, and it is not
 * needed. The old `SubscribeToNotifications(userId)` mechanism, which let any
 * client join any user's group, has no counterpart here.
 *
 * FAILURE BEHAVIOUR
 * SignalR is an ENHANCEMENT, never a dependency. Every failure path here resolves
 * rather than throws, leaves `state` as 'error', and lets the caller carry on -
 * the notification centre still works through the REST API, and the header's
 * `refetchOnWindowFocus` remains the fallback.
 */

/** Must match `NotificationService.ReceiveNotificationMethod` on the server. */
export const RECEIVE_NOTIFICATION_EVENT = 'ReceiveNotification';

/** Query keys the header bell and notification centre already use. */
export const NOTIFICATION_QUERY_KEYS = ['header-notifications'] as const;
export const UNREAD_COUNT_QUERY_KEYS = ['header-unread-count'] as const;

/**
 * Severities worth interrupting the user for. Deliberately a small allowlist:
 * a "Normal" background event must never become a disruptive toast, or routine
 * activity would train users to dismiss notifications.
 */
export const REALTIME_SEVERITIES: ReadonlySet<string> = new Set(['Important', 'Critical']);

/**
 * Coerces a server priority to a known value.
 *
 * Mirrors `utils/notifications.normalizePriority` but lives here so the realtime
 * path has no dependency on presentation code. Unknown values degrade to
 * 'Normal', which is NOT in REALTIME_SEVERITIES - so an unrecognised severity
 * fails quiet rather than escalating.
 */
export function normalizePriority(value: unknown): string {
  const known = ['Informational', 'Normal', 'Important', 'Critical'];
  if (typeof value === 'string') {
    const match = known.find((p) => p.toLowerCase() === value.trim().toLowerCase());
    if (match) return match;
  }
  return 'Normal';
}

export type NotificationHubState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'error';

/** The payload the server pushes. Shape matches `NotificationDto`. */
export interface RealtimeNotification {
  id: string;
  title: string;
  message: string;
  type: string;
  priority: string;
  isRead: boolean;
  createdAt: string;
  actionUrl?: string | null;
}

/** Narrowly-typed view of the parts of HubConnection this module uses. */
export interface SignalRConnection {
  start(): Promise<void>;
  stop(): Promise<void>;
  on(event: string, handler: (payload: any) => void): void;
  off(event: string, handler?: (payload: any) => void): void;
  onreconnecting(fn: (error?: Error) => void): void;
  onreconnected(fn?: (connectionId?: string) => void): void;
  onclose(fn?: (error?: Error) => void): void;
  get state(): string;
}

/**
 * THE CANONICAL SIGNALR HUB PATH.
 *
 * This must match `app.MapHub<NotificationHub>("/hub")` in
 * `src/SMS.API/Program.cs` EXACTLY, with no trailing slash. `/hub` and `/hub/`
 * are two different HTTP paths and the proxy in front of the API treats them
 * differently: with only a `location /hub/` block, a request for `/hub` falls
 * through to the SPA's `try_files $uri $uri/`, which nginx resolves as a
 * directory and answers with `301 -> /hub/`. A WebSocket handshake cannot
 * follow an HTTP redirect, so the socket never opened. Both nginx configs now
 * route `/hub` and `/hub/` to the API directly and nothing redirects.
 */
export const HUB_PATH = '/hub';

/**
 * Builds the hub URL from the SAME environment value the REST client uses.
 * VITE_API_URL is '/api/v1' in production and nginx proxies /hub separately, so
 * the hub sits at the origin root. Deriving it here (rather than hard-coding a
 * host) means no production IP address is ever embedded in the bundle.
 *
 * An operator-supplied override is normalised to the canonical, non-redirecting
 * form: a trailing slash is stripped so a stale `VITE_HUB_URL=/hub/` cannot
 * reintroduce the 301 defect.
 */
export function resolveHubUrl(): string {
  const configured = (import.meta as any).env?.VITE_HUB_URL as string | undefined;
  if (configured && typeof configured === 'string' && configured.trim().length > 0) {
    return normalizeHubUrl(configured.trim());
  }
  return HUB_PATH;
}

/**
 * Strips a trailing slash so every caller - the default, an env override, or a
 * test - resolves to the identical canonical path.
 */
function normalizeHubUrl(url: string): string {
  return url.length > 1 && url.endsWith('/') ? url.replace(/\/+$/, '') || '/' : url;
}

/**
 * The headers sent with the SignalR HTTP requests.
 *
 * WHY THIS IS NEEDED
 * `CsrfProtectionMiddleware` validates state-changing requests that are
 * authenticated by the `access_token` cookie, and `POST /hub/negotiate` is a
 * state-changing request on a cookie-authenticated session. The negotiation
 * therefore has to echo the double-submit token in `X-CSRF-TOKEN`, exactly as
 * the Axios interceptor already does for REST calls. Without it the server
 * answers `403 {"error":"CSRF validation failed: missing token"}` and no
 * connection is ever established.
 *
 * The hub is deliberately NOT exempted from CSRF on the server: negotiate is a
 * cookie-authenticated POST, and an exemption would re-open the cross-origin
 * attack the middleware exists to stop.
 *
 * WHY THE HEADER OBJECT IS REFRESHED, NOT SNAPSHOTTED
 * `IHttpConnectionOptions.headers` is a plain `MessageHeaders` object in
 * @microsoft/signalr 8.x - it cannot be a getter function. SignalR spreads
 * `this._options.headers` into every negotiate request
 * (`HttpConnection._getNegotiationResponse`), so the SAME object reference is
 * re-read on each negotiation, including each automatic reconnect.
 *
 * The client therefore keeps one long-lived object and refreshes its contents
 * before starting and before every reconnect. That matters because the CSRF
 * cookie is issued by the server's response: on a cold session it may not exist
 * when the connection is first built, and a value captured once at build time
 * would leave the header permanently absent and fail every later negotiate.
 *
 * WHAT SIGNALR DOES AND DOES NOT SEND THIS HEADER ON
 * Browsers cannot set custom headers on a WebSocket handshake, and SignalR's
 * own documentation says so. That is not a gap here:
 *   - `POST /hub/negotiate` is a state-changing request, so it IS CSRF-validated
 *     and DOES need this header. It is sent.
 *   - The WebSocket upgrade is a GET. `CsrfProtectionMiddleware` only validates
 *     GET/HEAD/OPTIONS/TRACE-exempt methods, so the handshake needs no CSRF
 *     header at all - only the httpOnly auth cookie, which the browser attaches
 *     automatically for a same-origin URL.
 *
 * WHAT IS NOT HERE
 * No `Authorization` header and no access token. Authentication remains the
 * httpOnly cookie the browser attaches by itself; no secret is ever handed to
 * JavaScript. The token value is never logged.
 */
export function buildHubHeaders(): Record<string, string> {
  const token = getCsrfToken();
  return token ? { [CSRF_HEADER_NAME]: token } : {};
}

/**
 * Updates a previously-built header object IN PLACE with the current CSRF
 * token. Mutating rather than replacing matters: SignalR holds a reference to
 * the original object, so replacing our local variable would leave the
 * connection using the stale one.
 *
 * An absent token DELETES the header rather than sending an empty value, so a
 * request that genuinely cannot be validated fails loudly with the server's 403
 * instead of being silently sent with a blank token.
 */
export function refreshHubHeaders(target: Record<string, string>): Record<string, string> {
  delete target[CSRF_HEADER_NAME];

  const token = getCsrfToken();
  if (token) {
    target[CSRF_HEADER_NAME] = token;
  }

  return target;
}

export interface NotificationHubOptions {
  /** Injected in tests; production uses the real @microsoft/signalr builder. */
  createConnection?: () => SignalRConnection;
  onNotification?: (payload: RealtimeNotification) => void;
  /** Called when the connection (re)establishes, to refresh server state. */
  onReconnected?: () => void;
  /** Called when the connection drops permanently. */
  onError?: (error: Error) => void;
}

export interface NotificationHubClient {
  start(): Promise<void>;
  stop(): Promise<void>;
  /** Idempotent: a second call on a live/starting client is a no-op. */
  isActive(): boolean;
  getState(): NotificationHubState;
  /** Test seam: number of live `on` registrations. Must never exceed 1. */
  handlerCount(): number;
}

export function createNotificationHubClient(
  options: NotificationHubOptions = {}
): NotificationHubClient {
  const { onNotification, onReconnected, onError } = options;

  let connection: SignalRConnection | null = null;
  let state: NotificationHubState = 'disconnected';
  let handler: ((payload: any) => void) | null = null;
  // Guards against duplicate work when start() races with a StrictMode double
  // effect or a re-render. Without it two sockets can be created.
  let starting: Promise<void> | null = null;
  // One long-lived header object shared with SignalR. It is refreshed in place
  // before every start and reconnect (see refreshHubHeaders) rather than being
  // rebuilt, because SignalR keeps a reference to this exact object.
  let hubHeaders: Record<string, string> = buildHubHeaders();

  const setState = (next: NotificationHubState) => {
    state = next;
  };

  const build = (): SignalRConnection => {
    if (options.createConnection) return options.createConnection();

    return new HubConnectionBuilder()
      .withUrl(resolveHubUrl(), {
        // The auth cookie is httpOnly: the browser attaches it automatically
        // because the URL is same-origin. No token is manufactured here.
        withCredentials: true,
        // `X-CSRF-TOKEN` for the cookie-authenticated POST /hub/negotiate.
        // This object is refreshed in place before every start and reconnect,
        // so a token issued after the connection was built is still picked up.
        // The WebSocket upgrade needs no CSRF header: it is a GET, which the
        // server does not CSRF-validate, and the browser attaches the auth
        // cookie to it automatically.
        headers: hubHeaders,
      })
      // Deliberately no accessTokenFactory: identity comes from the cookie, and
      // exposing a token to JS would widen the blast radius of any XSS.
      .configureLogging(LogLevel.Warning)
      // Bounded retry: brief outages self-heal, a real outage gives up quickly
      // and lets the API fallback take over rather than hammering the server.
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();
  };

  const fail = (err: unknown) => {
    const error = err instanceof Error ? err : new Error(String(err));
    setState('error');
    try {
      onError?.(error);
    } catch {
      // A reporting failure must never escape into React.
    }
  };

  const start = async (): Promise<void> => {
    // Idempotent: never open a second socket for the same session.
    if (starting || (connection && state === 'connected')) return;

    setState('connecting');
    starting = (async () => {
      try {
        // Pick up the current CSRF token immediately before the connection is
        // built, so the very first negotiate carries a valid value even on a
        // cold session where the cookie only appeared after page load.
        refreshHubHeaders(hubHeaders);
        connection = build();

        // Detach first so a re-created connection can never stack handlers.
        if (connection && handler) {
          try {
            connection.off(RECEIVE_NOTIFICATION_EVENT, handler);
          } catch {
            /* best effort */
          }
        }

        handler = (payload: RealtimeNotification) => {
          try {
            onNotification?.(payload);
          } catch {
            // Never let a handler bug break the socket.
          }
        };
        connection.on(RECEIVE_NOTIFICATION_EVENT, handler);

        connection.onreconnecting(() => {
          // Automatic reconnect re-runs negotiation, and negotiation is the
          // CSRF-validated POST. Refresh the token first so the reconnect is not
          // rejected with 403 and left to retry until the backoff gives up.
          refreshHubHeaders(hubHeaders);
          setState('reconnecting');
        });
        connection.onreconnected(() => {
          // Defensive: if the reconnect completed without an `onreconnecting`
          // event (for example a fast transport-level retry), the header is
          // still refreshed here so the next cycle starts from the current token.
          refreshHubHeaders(hubHeaders);
          setState('connected');
          // Anything that arrived while disconnected is only in the database, so
          // the caller must re-read rather than trust a missed push.
          try {
            onReconnected?.();
          } catch {
            /* ignore */
          }
        });
        connection.onclose((err) => {
          if (err) fail(err);
          else setState('disconnected');
        });

        await connection.start();
        setState('connected');
      } catch (err) {
        fail(err);
      } finally {
        starting = null;
      }
    })();

    await starting;
  };

  const stop = async (): Promise<void> => {
    const active = connection;
    connection = null;

    if (active && handler) {
      try {
        active.off(RECEIVE_NOTIFICATION_EVENT, handler);
      } catch {
        /* best effort */
      }
    }
    handler = null;
    starting = null;
    setState('disconnected');

    if (!active) return;
    try {
      await active.stop();
    } catch {
      // Logging out must always succeed locally even if the socket misbehaves.
    }
  };

  return {
    start,
    stop,
    isActive: () =>
      !!connection &&
      (state === 'connected' || state === 'connecting' || state === 'reconnecting'),
    getState: () => state,
    handlerCount: () => (handler ? 1 : 0),
  };
}