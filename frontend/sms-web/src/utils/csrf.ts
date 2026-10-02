/**
 * Double-submit CSRF token access — the single source of truth for the browser
 * side of the RISK-10 CSRF protection.
 *
 * WHY THIS IS A SHARED MODULE
 * The server (`SMS.API/Middleware/CsrfProtectionMiddleware.cs`) issues a random
 * 32-byte token as a NON-httpOnly `XSRF-TOKEN` cookie on every response and
 * requires state-changing requests to echo it back in the `X-CSRF-TOKEN` header.
 * Axios (services/api.ts) and SignalR (services/notificationHub.ts) BOTH need it.
 *
 * When the SignalR client originally read the cookie inline, it was a second,
 * independent copy of the same logic. That drift risk is why the reader lives
 * here: one cookie name, one header name, one parser, one place to change.
 *
 * WHAT THIS DELIBERATELY DOES NOT DO
 * - It never touches the authentication cookies (`access_token` /
 *   `refresh_token`), which stay httpOnly and therefore unreadable from JS.
 * - It never manufactures, caches or logs a token. The value is read from the
 *   cookie the server just set, so a server-side rotation is picked up
 *   automatically.
 */

/** Cookie the API sets. Deliberately NOT httpOnly: the double-submit pattern
 *  requires JavaScript to read it and echo it back in a header. */
export const CSRF_COOKIE_NAME = 'XSRF-TOKEN';

/** Header the API validates the cookie against on state-changing requests. */
export const CSRF_HEADER_NAME = 'X-CSRF-TOKEN';

/**
 * HTTP methods that cannot change state and are therefore not CSRF-validated
 * server-side. Mirrors `SafeMethods` in CsrfProtectionMiddleware.
 */
const SAFE_METHODS = ['GET', 'HEAD', 'OPTIONS', 'TRACE'];

/** True when the method is one the server will CSRF-validate. */
export function isStateChangingMethod(method: string | undefined): boolean {
  return !SAFE_METHODS.includes((method || 'get').toUpperCase());
}

/**
 * Reads the current CSRF token from the non-httpOnly `XSRF-TOKEN` cookie.
 *
 * Returns null when the cookie is not present yet (e.g. the very first request
 * of a cold session, before any API response has set it). Callers must treat
 * null as "no token available" and NOT invent a value — sending a fabricated
 * or empty token would fail validation anyway, and hard-coding one would be a
 * security regression.
 */
export function getCsrfToken(): string | null {
  if (typeof document === 'undefined' || !document.cookie) return null;

  // Escape the cookie name so a stray regex metacharacter in a future rename
  // cannot change the meaning of this pattern.
  const escaped = CSRF_COOKIE_NAME.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const match = document.cookie.match(new RegExp(`(?:^|;\\s*)${escaped}=([^;]*)`));

  if (!match || !match[1]) return null;

  try {
    // The cookie is base64 and may contain '+', '/' and '=' which are
    // percent-encoded on the wire; the server compares the decoded value.
    return decodeURIComponent(match[1]);
  } catch {
    // A malformed escape sequence must not break the caller.
    return match[1];
  }
}