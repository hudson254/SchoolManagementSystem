import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import {
  CSRF_COOKIE_NAME,
  CSRF_HEADER_NAME,
  getCsrfToken,
  isStateChangingMethod,
} from './csrf';

/**
 * The double-submit CSRF reader shared by the REST client and the SignalR
 * client. It must stay consistent with
 * `src/SMS.API/Middleware/CsrfProtectionMiddleware.cs`.
 */
describe('csrf', () => {
  const clearCookies = () => {
    for (const part of document.cookie.split(';')) {
      const name = part.split('=')[0]?.trim();
      if (name) {
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
      }
    }
  };

  beforeEach(clearCookies);
  afterEach(clearCookies);

  it('exposes the exact names the server middleware validates', () => {
    // CsrfProtectionMiddleware: CsrfCookieName = "XSRF-TOKEN",
    //                            CsrfHeaderName = "X-CSRF-TOKEN".
    expect(CSRF_COOKIE_NAME).toBe('XSRF-TOKEN');
    expect(CSRF_HEADER_NAME).toBe('X-CSRF-TOKEN');
  });

  it('reads the token from the cookie', () => {
    document.cookie = 'XSRF-TOKEN=abc123';

    expect(getCsrfToken()).toBe('abc123');
  });

  it('finds the cookie when it is not the first entry', () => {
    document.cookie = 'other=1';
    document.cookie = 'XSRF-TOKEN=abc123';

    expect(getCsrfToken()).toBe('abc123');
  });

  it('does not match a cookie whose name merely ends with the token name', () => {
    // `NOT-XSRF-TOKEN` must not be mistaken for `XSRF-TOKEN`.
    document.cookie = 'NOT-XSRF-TOKEN=wrong';

    expect(getCsrfToken()).toBeNull();
  });

  it('URL-decodes the value so it matches the server-side comparison', () => {
    document.cookie = `XSRF-TOKEN=${encodeURIComponent('VG9rZW4rL3dpdGg9')}`;

    expect(getCsrfToken()).toBe('VG9rZW4rL3dpdGg9');
  });

  it('returns null rather than throwing when the cookie is absent', () => {
    expect(getCsrfToken()).toBeNull();
  });

  it('returns null rather than throwing on a malformed escape sequence', () => {
    document.cookie = 'XSRF-TOKEN=%E0%A4%A';

    // A malformed value must not take down the caller; the request simply
    // proceeds without a header and the server decides.
    expect(() => getCsrfToken()).not.toThrow();
  });

  it('treats only state-changing methods as CSRF-validated', () => {
    // Mirrors SafeMethods in CsrfProtectionMiddleware.
    expect(isStateChangingMethod('GET')).toBe(false);
    expect(isStateChangingMethod('HEAD')).toBe(false);
    expect(isStateChangingMethod('OPTIONS')).toBe(false);
    expect(isStateChangingMethod('TRACE')).toBe(false);

    expect(isStateChangingMethod('POST')).toBe(true);
    expect(isStateChangingMethod('PUT')).toBe(true);
    expect(isStateChangingMethod('PATCH')).toBe(true);
    expect(isStateChangingMethod('DELETE')).toBe(true);
  });

  it('is case-insensitive and defaults an absent method to GET', () => {
    expect(isStateChangingMethod('post')).toBe(true);
    expect(isStateChangingMethod('get')).toBe(false);
    // Axios leaves `method` undefined on some configs; the default must be the
    // safe one so a read never starts failing with 403.
    expect(isStateChangingMethod(undefined)).toBe(false);
  });
});