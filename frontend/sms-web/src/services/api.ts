import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { normalizeError } from '../utils/errors';

const API_URL = import.meta.env.VITE_API_URL || '/api/v1';
const API_TIMEOUT = parseInt(import.meta.env.VITE_API_TIMEOUT || '30000');

// Paths that must never trigger a cookie-refresh attempt.
// Login/register establish a session; refresh-token is the refresh endpoint
// itself; password-reset/verify-email are pre-session flows.
const AUTH_REFRESH_SKIP_PATHS = [
  '/auth/login',
  '/auth/register',
  '/auth/refresh-token',
  '/auth/forgot-password',
  '/auth/reset-password',
  '/auth/verify-email',
  '/auth/resend-verification',
];

// Reads the non-httpOnly XSRF-TOKEN cookie set by CsrfProtectionMiddleware
// and returns its value (for the X-CSRF-TOKEN header on state-changing
// requests), or null if the cookie is not present yet.
const getCsrfToken = (): string | null => {
  const match = document.cookie.match(/(?:^|;\s*)XSRF-TOKEN=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
};

// ────────────────────────────────────────────────────────────────────────────
// Single-flight refresh lock (module-level state)
//
// When multiple requests fail with 401 simultaneously, only one actual
// refresh call is dispatched.  The others queue and are either retried
// (with fresh cookies) or rejected once the refresh completes.  This
// prevents a refresh-storm that would hit the rate limiter.
// ────────────────────────────────────────────────────────────────────────────
interface RefreshQueueItem {
  resolve: (value: AxiosResponse | PromiseLike<AxiosResponse>) => void;
  reject: (reason: unknown) => void;
  config: AxiosRequestConfig;
}
let isRefreshing = false;
let failedQueue: RefreshQueueItem[] = [];
let currentRefreshPromise: Promise<string | null> | null = null;

function processQueue(error: unknown, token: string | null = null): void {
  const queue = [...failedQueue];
  failedQueue = [];
  isRefreshing = false;
  currentRefreshPromise = null;

  for (const item of queue) {
    if (error) {
      item.reject(error);
    } else if (token) {
      // Mark as retried so the response interceptor does not re-enter
      // the refresh flow for this request.
      item.config._retry = true;
      // Replay the request.  The new httpOnly cookies are sent
      // automatically by the browser.
      item.resolve(
        axios.create({
          baseURL: API_URL,
          timeout: API_TIMEOUT,
          withCredentials: true,
          headers: { 'Content-Type': 'application/json' },
        })(item.config)
      );
    }
  }
}

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_URL,
      timeout: API_TIMEOUT,
      withCredentials: true, // RISK-08: send/receive httpOnly auth cookies
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor — attach the CSRF token to state-changing
    // requests. The XSRF-TOKEN cookie is set by the backend on every
    // response; the double-submit cookie pattern requires echoing it
    // back in the X-CSRF-TOKEN header.
    this.client.interceptors.request.use(
      (config) => {
        const method = (config.method || 'get').toUpperCase();
        const isStateChanging = !['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(method);

        if (isStateChanging && !config.headers['X-CSRF-TOKEN']) {
          const csrfToken = getCsrfToken();
          if (csrfToken) {
            config.headers['X-CSRF-TOKEN'] = csrfToken;
          }
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor — on a 401 (expired access token), attempt a
    // silent cookie-based refresh. The backend rotates the refresh token
    // and sets new httpOnly cookies. If refresh fails, redirect to login.
    //
    // CRITICAL SAFEGUARDS against refresh-storms:
    //  1. Never refresh for auth endpoints (login, refresh-token, etc.)
    //  2. Single-flight lock — only one actual refresh at a time
    //  3. No infinite retry — _retry flag prevents re-entering the flow
    //  4. Rate-limit safe — no storm of simultaneous refresh calls
    this.client.interceptors.response.use(
      (response: AxiosResponse) => response,
      async (error) => {
        const originalRequest = error.config;

        // Only handle 401 responses; skip if no response, already retried,
        // or the request URL targets an auth endpoint that must not
        // trigger a refresh cycle.
        if (
          !error.response ||
          error.response.status !== 401 ||
          originalRequest._retry ||
          !originalRequest.url ||
          AUTH_REFRESH_SKIP_PATHS.some((p) => originalRequest.url.includes(p))
        ) {
          return Promise.reject(normalizeError(error));
        }

        // ── Single-flight refresh lock ──────────────────────────────
        // If a refresh is already in progress, queue this request to be
        // retried (or rejected) once the refresh completes.
        if (isRefreshing && currentRefreshPromise) {
          return new Promise<AxiosResponse>((resolve, reject) => {
            failedQueue.push({ resolve, reject, config: originalRequest });
          });
        }

        // Start a new refresh — this is the first 401 we have seen.
        isRefreshing = true;
        currentRefreshPromise = this.refreshAuthToken();

        try {
          const accessToken = await currentRefreshPromise;
          if (accessToken) {
            // Refresh succeeded — queue this request and process the
            // entire queue so every waiting request is retried.
            return new Promise<AxiosResponse>((resolve, reject) => {
              failedQueue.push({ resolve, reject, config: originalRequest });
              processQueue(null, accessToken);
            });
          }
        } catch (refreshError) {
          // Refresh failed — reject all queued requests and redirect.
          processQueue(refreshError, null);
          window.location.href = '/login';
          return Promise.reject(normalizeError(refreshError));
        }

        // Fallback (should not normally be reached).
        processQueue(null, null);
        return Promise.reject(normalizeError(error));
      }
    );
  }

  /**
   * Call the refresh-token endpoint. The backend reads the httpOnly
   * refresh_token cookie, rotates it, and sets fresh access_token /
   * refresh_token cookies. No request body is required.
   *
   * This endpoint is in AUTH_REFRESH_SKIP_PATHS so the response
   * interceptor will never attempt to refresh a refresh.
   */
  private async refreshAuthToken(): Promise<string> {
    const response = await this.client.post('/auth/refresh-token');
    return response.data?.accessToken || '';
  }

  public get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    return this.client.get<T>(url, config).then((res) => res.data);
  }

  public post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.client.post<T>(url, data, config).then((res) => res.data);
  }

  public put<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.client.put<T>(url, data, config).then((res) => res.data);
  }

  public patch<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.client.patch<T>(url, data, config).then((res) => res.data);
  }

  public delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    return this.client.delete<T>(url, config).then((res) => res.data);
  }

  public upload<T = any>(url: string, file: File, onProgress?: (progress: number) => void): Promise<T> {
    const formData = new FormData();
    formData.append('file', file);

    return this.client.post<T>(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (onProgress && progressEvent.total) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(progress);
        }
      },
    }).then((res) => res.data);
  }
}

export const apiClient = new ApiClient();

// Convenience methods
export const api = {
  get: <T = any>(url: string, config?: AxiosRequestConfig) => apiClient.get<T>(url, config),
  post: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => apiClient.post<T>(url, data, config),
  put: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => apiClient.put<T>(url, data, config),
  patch: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => apiClient.patch<T>(url, data, config),
  delete: <T = any>(url: string, config?: AxiosRequestConfig) => apiClient.delete<T>(url, config),
  upload: <T = any>(url: string, file: File, onProgress?: (progress: number) => void) =>
    apiClient.upload<T>(url, file, onProgress),
};
