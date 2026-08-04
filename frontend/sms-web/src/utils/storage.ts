// RISK-08: Authentication tokens are NO LONGER stored in browser storage.
// They are stored in httpOnly, SameSite cookies set by the backend
// (AuthController). JavaScript cannot read them, which eliminates the
// localStorage XSS exfiltration vector.
//
// This module now only persists non-sensitive user profile data (the same
// data the /auth/me endpoint returns) for instant UI rendering. The source
// of truth for the authenticated user is always the backend.

const USER_KEY = 'user';

export const storage = {
  // Tokens are handled entirely by httpOnly cookies — nothing to read here.
  getAccessToken: (): string | null => {
    return null;
  },

  setAccessToken: (_token: string): void => {
    // No-op — tokens live in httpOnly cookies.
  },

  getRefreshToken: (): string | null => {
    return null;
  },

  setRefreshToken: (_token: string): void => {
    // No-op — tokens live in httpOnly cookies.
  },

  setTokens: (_accessToken: string, _refreshToken: string): void => {
    // No-op — tokens live in httpOnly cookies.
  },

  clearTokens: (): void => {
    localStorage.removeItem(USER_KEY);
  },

  getUser: (): any | null => {
    const user = localStorage.getItem(USER_KEY);
    return user ? JSON.parse(user) : null;
  },

  setUser: (user: any): void => {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  clear: (): void => {
    localStorage.clear();
  },
};
