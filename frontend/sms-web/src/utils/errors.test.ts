import { describe, it, expect, vi, afterEach } from 'vitest';
import { normalizeError, getFieldErrors, isOffline } from './errors';

describe('error normalization utility (Phase 7)', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('normalizes a validation error to a user-friendly message', () => {
    const error = {
      success: false,
      code: 'VALIDATION_ERROR',
      message: 'One or more validation failures have occurred.',
      statusCode: 400,
      errors: { Email: ['Email is required.'] },
    };

    const normalized = normalizeError(error);
    expect(normalized.code).toBe('VALIDATION_ERROR');
    expect(normalized.message).toContain('correct the highlighted fields');
    expect(normalized.isNetworkError).toBe(false);
    expect(normalized.errors).toEqual({ Email: ['Email is required.'] });
  });

  it('maps known error codes to friendly messages', () => {
    const error = { success: false, code: 'DATABASE_UNAVAILABLE' };
    const normalized = normalizeError(error);
    expect(normalized.message).toContain('database is currently unavailable');
  });

  it('falls back to a generic message for unknown errors', () => {
    const normalized = normalizeError(new Error('raw stack trace: at SMS.API.Controllers...'));
    expect(normalized.message).toContain('Something went wrong');
    expect(normalized.message).not.toContain('at SMS.API');
  });

  it('detects 401 as session expired', () => {
    const normalized = normalizeError({ status: 401 });
    expect(normalized.code).toBe('SESSION_EXPIRED');
    expect(normalized.message).toContain('session has expired');
  });

  it('detects 403 as access denied', () => {
    const normalized = normalizeError({ status: 403 });
    expect(normalized.code).toBe('ACCESS_DENIED');
    expect(normalized.message).toContain('permission');
  });

  it('detects 404 as not found', () => {
    const normalized = normalizeError({ status: 404 });
    expect(normalized.code).toBe('NOT_FOUND');
    expect(normalized.message).toContain('not found');
  });

  it('detects timeout errors', () => {
    const normalized = normalizeError({ code: 'ECONNABORTED' });
    expect(normalized.isTimeout).toBe(true);
    expect(normalized.message).toContain('timed out');
  });

  it('detects network errors', () => {
    const normalized = normalizeError({ code: 'ERR_NETWORK' });
    expect(normalized.isNetworkError).toBe(true);
    expect(normalized.message).toContain('network error');
  });

  it('detects offline state', () => {
    // Simulate offline
    Object.defineProperty(navigator, 'onLine', { configurable: true, value: false });
    const normalized = normalizeError(new Error('offline'));
    expect(normalized.isNetworkError).toBe(true);
    expect(normalized.code).toBe('NETWORK_OFFLINE');
    expect(normalized.message).toContain('offline');
  });

  it('getFieldErrors extracts validation errors', () => {
    const error = {
      success: false,
      code: 'VALIDATION_ERROR',
      errors: { Name: ['Name is required.'], Age: ['Age must be positive.'] },
    };
    const fieldErrors = getFieldErrors(error);
    expect(fieldErrors).toEqual({
      Name: ['Name is required.'],
      Age: ['Age must be positive.'],
    });
  });

  it('getFieldErrors returns undefined when no field errors exist', () => {
    expect(getFieldErrors(new Error('boom'))).toBeUndefined();
  });

  it('isOffline returns current navigator state', () => {
    Object.defineProperty(navigator, 'onLine', { configurable: true, value: true });
    expect(isOffline()).toBe(false);
  });
});
