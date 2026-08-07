import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { normalizeError } from '../utils/errors';

const API_URL = import.meta.env.VITE_API_URL || '/api';
const API_TIMEOUT = parseInt(import.meta.env.VITE_API_TIMEOUT || '30000');

// Reads the non-httpOnly XSRF-TOKEN cookie set by CsrfProtectionMiddleware
// and returns its value (for the X-CSRF-TOKEN header on state-changing
// requests), or null if the cookie is not present yet.
const getCsrfToken = (): string | null => {
  const match = document.cookie.match(/(?:^|;\s*)XSRF-TOKEN=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
};

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
    // All errors are normalized to user-friendly messages before rejection.
    this.client.interceptors.response.use(
      (response: AxiosResponse) => response,
      async (error) => {
        const originalRequest = error.config;

        // Avoid infinite retry loops on the refresh endpoint itself.
        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true;

          try {
            // The refresh endpoint reads the refresh_token cookie set by the
            // backend; no body is needed. It returns the new access token so
            // we can replay the original request (the cookie is already set).
            const accessToken = await this.refreshAuthToken();
            if (accessToken) {
              return this.client(originalRequest);
            }
          } catch (refreshError) {
            // Token refresh failed — clear user session and go to login.
            window.location.href = '/login';
            return Promise.reject(normalizeError(refreshError));
          }
        }

        // Normalize the error to a user-friendly, safe message.
        // Never exposes stack traces, SQL, file paths, or internal details.
        return Promise.reject(normalizeError(error));
      }
    );
  }

  private async refreshAuthToken(): Promise<string> {
    // With httpOnly cookies, the refresh-token call simply needs to hit the
    // endpoint with credentials; the backend reads the refresh_token cookie,
    // validates it, rotates it, and sets fresh cookies.
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
