/**
 * Normalizes API list responses to arrays.
 *
 * Some endpoints in the School Management System return a bare array
 * (e.g. `/assessment/types`) while others return a paginated envelope
 * (e.g. `{ items: [...] }`). Components that naively call `.map()` on
 * the raw payload crash at render time with the app's generic
 * ErrorBoundary message. This helper guarantees an array for both
 * shapes, so a malformed or empty response can never take down a page.
 */
export function asList<T>(value: unknown): T[] {
  if (Array.isArray(value)) {
    return value as T[];
  }
  if (value !== null && typeof value === 'object' && Array.isArray((value as { items?: unknown }).items)) {
    return (value as { items: T[] }).items;
  }
  return [];
}