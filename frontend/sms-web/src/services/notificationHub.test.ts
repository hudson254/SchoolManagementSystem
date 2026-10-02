import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  HUB_PATH,
  NOTIFICATION_QUERY_KEYS,
  REALTIME_SEVERITIES,
  RECEIVE_NOTIFICATION_EVENT,
  UNREAD_COUNT_QUERY_KEYS,
  buildHubHeaders,
  createNotificationHubClient,
  normalizePriority,
  refreshHubHeaders,
  resolveHubUrl,
  type RealtimeNotification,
  type SignalRConnection,
} from './notificationHub';

/**
 * A controllable SignalR double. It records handler registrations so a test can
 * assert the client never stacks duplicate handlers, and exposes the
 * reconnect/close callbacks a real connection would fire.
 */
class FakeConnection implements SignalRConnection {
  public state = 'Disconnected';
  public startCount = 0;
  public stopCount = 0;
  public readonly onCalls: string[] = [];
  public readonly offCalls: string[] = [];
  public lastHandler: ((payload: any) => void) | null = null;
  public startError: Error | null = null;
  public stopError: Error | null = null;

  private reconnecting?: (error?: Error) => void;
  private reconnected?: (connectionId?: string) => void;
  private closed?: (error?: Error) => void;

  async start(): Promise<void> {
    this.startCount += 1;
    if (this.startError) throw this.startError;
    this.state = 'Connected';
  }

  async stop(): Promise<void> {
    this.stopCount += 1;
    this.state = 'Disconnected';
    if (this.stopError) throw this.stopError;
  }

  on(event: string, handler: (payload: any) => void): void {
    this.onCalls.push(event);
    this.lastHandler = handler;
  }

  off(event: string, handler?: (payload: any) => void): void {
    this.offCalls.push(event);
    if (!handler || handler === this.lastHandler) this.lastHandler = null;
  }

  onreconnecting(fn: (error?: Error) => void): void {
    this.reconnecting = fn;
  }
  onreconnected(fn?: (connectionId?: string) => void): void {
    this.reconnected = fn;
  }
  onclose(fn?: (error?: Error) => void): void {
    this.closed = fn;
  }

  // ── Test drivers ────────────────────────────────────────────────────
  emit(payload: RealtimeNotification): void {
    this.lastHandler?.(payload);
  }
  simulateReconnecting(): void {
    this.state = 'Reconnecting';
    this.reconnecting?.();
  }
  simulateReconnected(): void {
    this.state = 'Connected';
    this.reconnected?.('conn-1');
  }
  simulateClose(error?: Error): void {
    this.state = 'Disconnected';
    this.closed?.(error);
  }
}

const sampleNotification: RealtimeNotification = {
  id: 'n-1',
  title: 'New Assignment',
  message: 'Essay 1 is due Friday.',
  type: 'Assignment',
  priority: 'Important',
  isRead: false,
  createdAt: new Date().toISOString(),
};

describe('notificationHub', () => {
  let fake: FakeConnection;

  beforeEach(() => {
    fake = new FakeConnection();
  });

  const build = (overrides = {}) =>
    createNotificationHubClient({
      createConnection: () => fake,
      ...overrides,
    });

  // ── Connection lifecycle ─────────────────────────────────────────────

  it('starts a connection and reports connected', async () => {
    const client = build();
    await client.start();

    expect(fake.startCount).toBe(1);
    expect(client.getState()).toBe('connected');
    expect(client.isActive()).toBe(true);
  });

  it('stops the connection and detaches the handler on logout', async () => {
    const client = build();
    await client.start();
    await client.stop();

    expect(fake.stopCount).toBe(1);
    expect(client.getState()).toBe('disconnected');
    expect(client.isActive()).toBe(false);
    // The handler must be removed, otherwise a stale socket could keep writing
    // into a torn-down tree.
    expect(fake.offCalls).toContain(RECEIVE_NOTIFICATION_EVENT);
    expect(fake.lastHandler).toBeNull();
  });

  it('never opens a second connection for the same session', async () => {
    const client = build();
    await client.start();
    await client.start();
    await client.start();

    expect(fake.startCount).toBe(1);
    expect(client.handlerCount()).toBe(1);
  });

  it('does not stack handlers when restarted after logout', async () => {
    const client = build();
    await client.start();
    await client.stop();
    await client.start();

    // Exactly one live registration at any time.
    expect(client.handlerCount()).toBe(1);
  });

  // ── ReceiveNotification ──────────────────────────────────────────────

  it('registers the server event name, not an invented one', async () => {
    const client = build();
    await client.start();

    expect(fake.onCalls).toContain(RECEIVE_NOTIFICATION_EVENT);
    expect(RECEIVE_NOTIFICATION_EVENT).toBe('ReceiveNotification');
  });

  it('invokes the notification callback when the server pushes', async () => {
    const onNotification = vi.fn();
    const client = build({ onNotification });
    await client.start();

    fake.emit(sampleNotification);

    expect(onNotification).toHaveBeenCalledTimes(1);
    expect(onNotification).toHaveBeenCalledWith(sampleNotification);
  });

  it('survives a throwing notification callback without breaking the socket', async () => {
    const onNotification = vi.fn(() => {
      throw new Error('render bug');
    });
    const client = build({ onNotification });
    await client.start();

    expect(() => fake.emit(sampleNotification)).not.toThrow();
    expect(client.getState()).toBe('connected');
  });

  // ── Reconnection ────────────────────────────────────────────────────

  it('reports reconnecting and then connected again', async () => {
    const client = build();
    await client.start();

    fake.simulateReconnecting();
    expect(client.getState()).toBe('reconnecting');

    fake.simulateReconnected();
    expect(client.getState()).toBe('connected');
  });

  it('requests a refresh on reconnect so missed pushes are re-read', async () => {
    const onReconnected = vi.fn();
    const client = build({ onReconnected });
    await client.start();

    fake.simulateReconnecting();
    fake.simulateReconnected();

    // Anything raised while offline exists only in the database, so the client
    // must re-read rather than assume it saw every push.
    expect(onReconnected).toHaveBeenCalledTimes(1);
  });

  it('does not create duplicate handlers after a reconnect', async () => {
    const onNotification = vi.fn();
    const client = build({ onNotification });
    await client.start();

    fake.simulateReconnecting();
    fake.simulateReconnected();
    fake.emit(sampleNotification);

    expect(onNotification).toHaveBeenCalledTimes(1);
    expect(client.handlerCount()).toBe(1);
  });

  // ── Failure fallback ────────────────────────────────────────────────

  it('reports error without throwing when the connection cannot start', async () => {
    fake.startError = new Error('negotiate failed');
    const onError = vi.fn();
    const client = build({ onError });

    await expect(client.start()).resolves.toBeUndefined();

    expect(client.getState()).toBe('error');
    expect(client.isActive()).toBe(false);
    expect(onError).toHaveBeenCalledTimes(1);
  });

  it('reports error when the socket closes with a failure', async () => {
    const onError = vi.fn();
    const client = build({ onError });
    await client.start();

    fake.simulateClose(new Error('transport closed'));

    expect(client.getState()).toBe('error');
    expect(onError).toHaveBeenCalledTimes(1);
  });

  it('stays usable after a start failure so the API fallback can serve', async () => {
    fake.startError = new Error('hub offline');
    const client = build();
    await client.start();

    // The app must keep working: notification APIs still function and window
    // focus still refreshes. The client simply reports it is not connected.
    expect(client.getState()).toBe('error');
    await expect(client.stop()).resolves.toBeUndefined();
  });

  it('stop resolves even when the underlying socket misbehaves', async () => {
    const client = build();
    await client.start();

    fake.stopError = new Error('already disposed');
    await expect(client.stop()).resolves.toBeUndefined();
  });

  // ── Authentication posture ───────────────────────────────────────────

  it('resolves a relative hub URL with no hard-coded host', () => {
    const url = resolveHubUrl();
    expect(url).toBe('/hub');
    expect(url.startsWith('/')).toBe(true);
    expect(url).not.toMatch(/https?:\/\//);
  });

  it('exposes no client-callable group-subscription method', () => {
    // The removed server-side SubscribeToNotifications(userId) was a
    // cross-user disclosure hole. Nothing in this client may reintroduce the
    // capability of naming another user's stream.
    const client = build() as unknown as Record<string, unknown>;
    expect(client.SubscribeToNotifications).toBeUndefined();
    expect(client.subscribe).toBeUndefined();
  });

  it('exposes only the four lifecycle methods and no token/user-id surface', () => {
    const client = build() as unknown as Record<string, unknown>;
    // No method may take a user id: group membership is server-derived.
    expect(Object.keys(client).sort()).toEqual([
      'getState',
      'handlerCount',
      'isActive',
      'start',
      'stop',
    ]);
  });

  // ── Query keys ───────────────────────────────────────────────────────

  it('uses the query keys the header already declares', () => {
    // These MUST match components/Layout/Header.tsx, otherwise invalidation
    // silently does nothing and live updates appear broken.
    expect(NOTIFICATION_QUERY_KEYS[0]).toBe('header-notifications');
    expect(UNREAD_COUNT_QUERY_KEYS[0]).toBe('header-unread-count');
  });

  // ── Severity gating ──────────────────────────────────────────────────

  it('treats only Important and Critical as worth a toast', () => {
    expect(REALTIME_SEVERITIES.has('Important')).toBe(true);
    expect(REALTIME_SEVERITIES.has('Critical')).toBe(true);
    expect(REALTIME_SEVERITIES.has('Normal')).toBe(false);
    expect(REALTIME_SEVERITIES.has('Informational')).toBe(false);
  });

  it('normalises an unknown priority to Normal so it fails quiet', () => {
    expect(normalizePriority('critical')).toBe('Critical');
    expect(normalizePriority('nonsense')).toBe('Normal');
    expect(normalizePriority(undefined)).toBe('Normal');
    expect(REALTIME_SEVERITIES.has(normalizePriority('nonsense'))).toBe(false);
  });
});

// ───────────────────────────────────────────────────────────────────────────
// CSRF handshake
//
// The production defect these guard against: `withUrl()` was given only
// `withCredentials`, so `POST /hub/negotiate` was rejected by
// CsrfProtectionMiddleware with 403 and no connection was ever established.
// ───────────────────────────────────────────────────────────────────────────
describe('notificationHub CSRF handshake', () => {
  const setCookie = (value: string) => {
    document.cookie = `XSRF-TOKEN=${value}`;
  };

  const clearCookie = () => {
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
  };

  beforeEach(() => {
    clearCookie();
  });

  afterEach(() => {
    clearCookie();
  });

  it('sends the existing XSRF-TOKEN cookie as X-CSRF-TOKEN', () => {
    setCookie('token-abc123');

    const headers = buildHubHeaders();

    expect(headers['X-CSRF-TOKEN']).toBe('token-abc123');
  });

  it('omits the header entirely when no CSRF cookie is present', () => {
    // Sending an empty or fabricated token would fail validation anyway, and
    // hard-coding one would be a security regression. The header must simply
    // be absent, and the client must degrade to the REST fallback.
    const headers = buildHubHeaders();

    expect(headers).not.toHaveProperty('X-CSRF-TOKEN');
    expect(Object.keys(headers)).toHaveLength(0);
  });

  it('reads the cookie lazily on every call, so a late-issued token is picked up', () => {
    // Regression guard for the build-time-snapshot bug: the cookie is issued by
    // the server's response, so it may appear AFTER the module loads. Because
    // withUrl() receives a FUNCTION, each negotiate - including every automatic
    // reconnect - re-reads the current value.
    expect(buildHubHeaders()).not.toHaveProperty('X-CSRF-TOKEN');

    setCookie('issued-later');

    expect(buildHubHeaders()['X-CSRF-TOKEN']).toBe('issued-later');
  });

  it('URL-decodes the cookie value the way the server compares it', () => {
    // The token is base64 and may contain '+', '/' and '=' which arrive
    // percent-encoded on the wire. The middleware compares the DECODED value,
    // so the client must decode too or the two sides would disagree.
    const raw = 'VG9rZW4rL3dpdGg9';
    setCookie(encodeURIComponent(raw));

    expect(buildHubHeaders()['X-CSRF-TOKEN']).toBe(raw);
  });

  it('never sends an Authorization header or any access token', () => {
    setCookie('token-abc123');

    const headers = buildHubHeaders();

    // Authentication is the httpOnly cookie the browser attaches by itself. No
    // bearer token may ever be handed to JavaScript.
    expect(headers).not.toHaveProperty('Authorization');
    expect(Object.keys(headers)).toEqual(['X-CSRF-TOKEN']);
  });

  it('never hard-codes a CSRF token', async () => {
    // Read through Vite's `?raw` import so the assertion needs no Node type
    // declarations (the app tsconfig deliberately excludes them).
    const source = (await import('./notificationHub.ts?raw')).default;

    // A hard-coded token would look like a literal base64 blob. The real token is
    // generated per session by CsrfProtectionMiddleware and only ever read from
    // the cookie, so no such literal may appear in this module.
    const literals = source.match(/['"][A-Za-z0-9+/]{32,}={0,2}['"]/g) ?? [];
    expect(literals).toEqual([]);
  });

  it('refreshes an existing header object in place rather than replacing it', () => {
    // SignalR holds a reference to the object passed to withUrl and re-reads it
    // on every negotiate. Replacing our local variable would leave the connection
    // using the stale object, so refreshHubHeaders must mutate the same target.
    document.cookie = 'XSRF-TOKEN=first-token';

    const headers = buildHubHeaders();
    expect(headers['X-CSRF-TOKEN']).toBe('first-token');

    document.cookie = 'XSRF-TOKEN=second-token';
    const returned = refreshHubHeaders(headers);

    // Same object identity...
    expect(returned).toBe(headers);
    // ...and now carrying the new token.
    expect(headers['X-CSRF-TOKEN']).toBe('second-token');
  });

  it('deletes the header when the cookie disappears, rather than sending a blank', () => {
    // An empty token would fail validation the same way, but a missing header is
    // unambiguous and never looks like a successfully-sent value.
    document.cookie = 'XSRF-TOKEN=some-token';
    const headers = buildHubHeaders();

    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
    refreshHubHeaders(headers);

    expect(headers).not.toHaveProperty('X-CSRF-TOKEN');
  });
});

// ───────────────────────────────────────────────────────────────────────────
// Canonical hub URL
//
// The production defect these guard against: nginx had only `location /hub/`,
// so `/hub` fell through to the SPA's `try_files $uri $uri/` and nginx answered
// `301 -> /hub/`. A WebSocket handshake cannot follow an HTTP redirect.
// ───────────────────────────────────────────────────────────────────────────
describe('notificationHub canonical URL', () => {
  it('uses /hub, matching app.MapHub<NotificationHub>("/hub")', () => {
    expect(resolveHubUrl()).toBe('/hub');
    expect(HUB_PATH).toBe('/hub');
  });

  it('never requests a trailing-slash form that nginx would redirect', () => {
    const url = resolveHubUrl();

    // A trailing slash is the exact shape that produced `301 -> /hub/`.
    expect(url.endsWith('/')).toBe(false);
  });

  it('is a relative, same-origin path with no hard-coded host', () => {
    const url = resolveHubUrl();

    // Same-origin is what makes the httpOnly auth cookie and the CSRF cookie
    // apply automatically without any CORS handling.
    expect(url.startsWith('/')).toBe(true);
    expect(url).not.toMatch(/https?:\/\//);
  });

  it('strips a trailing slash from a configured override', () => {
    // A stale VITE_HUB_URL=/hub/ must not be able to reintroduce the redirect.
    const originalEnv = (import.meta as any).env;
    (import.meta as any).env = { ...originalEnv, VITE_HUB_URL: '/hub/' };

    try {
      expect(resolveHubUrl()).toBe('/hub');
    } finally {
      (import.meta as any).env = originalEnv;
    }
  });
});