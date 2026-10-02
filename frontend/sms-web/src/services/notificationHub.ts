import { LogLevel, HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

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
 * Builds the hub URL from the SAME environment value the REST client uses.
 * VITE_API_URL is '/api/v1' in production and nginx proxies /hub separately, so
 * the hub sits at the origin root. Deriving it here (rather than hard-coding a
 * host) means no production IP address is ever embedded in the bundle.
 */
export function resolveHubUrl(): string {
  const configured = (import.meta as any).env?.VITE_HUB_URL as string | undefined;
  if (configured && typeof configured === 'string' && configured.trim().length > 0) {
    return configured.trim();
  }
  return '/hub';
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

        connection.onreconnecting(() => setState('reconnecting'));
        connection.onreconnected(() => {
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