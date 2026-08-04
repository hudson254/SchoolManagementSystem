import { describe, it, expect, beforeEach } from 'vitest';
import { storage } from './storage';

describe('storage utility (RISK-08 / RISK-19)', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('getAccessToken always returns null (tokens live in httpOnly cookies)', () => {
    expect(storage.getAccessToken()).toBeNull();
  });

  it('setAccessToken is a no-op (does not write to localStorage)', () => {
    storage.setAccessToken('fake-token');
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(localStorage.getItem('refresh_token')).toBeNull();
  });

  it('setTokens is a no-op (does not write to localStorage)', () => {
    storage.setTokens('access', 'refresh');
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(localStorage.getItem('refresh_token')).toBeNull();
  });

  it('setUser persists non-sensitive profile data', () => {
    const user = { id: 'u1', name: 'Test User', roles: ['Student'] };
    storage.setUser(user);
    expect(storage.getUser()).toEqual(user);
  });

  it('getUser returns null when no user is stored', () => {
    expect(storage.getUser()).toBeNull();
  });

  it('clearTokens removes the cached user profile', () => {
    storage.setUser({ id: 'u1' });
    storage.clearTokens();
    expect(storage.getUser()).toBeNull();
  });

  it('clear removes all localStorage entries', () => {
    storage.setUser({ id: 'u1' });
    localStorage.setItem('other', 'value');
    storage.clear();
    expect(localStorage.length).toBe(0);
  });
});
