import { useState, useCallback } from 'react';
import { apiClient } from '../services/api';
import { normalizeError, isOffline, NormalizedError } from '../utils/errors';

interface UseApiState<T> {
  data: T | null;
  loading: boolean;
  error: NormalizedError | null;
}

interface UseApiOptions {
  retries?: number;
  retryDelayMs?: number;
}

const DEFAULT_RETRIES = 1;
const DEFAULT_RETRY_DELAY_MS = 1000;

/**
 * Enterprise-grade API hook with:
 * - Loading state
 * - Error normalization (user-friendly messages, never raw stack traces)
 * - Automatic retry with backoff for transient network/timeout errors
 * - Offline detection
 * - Reset capability
 */
export function useApi<T>(options?: UseApiOptions) {
  const [state, setState] = useState<UseApiState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  const retries = options?.retries ?? DEFAULT_RETRIES;
  const retryDelayMs = options?.retryDelayMs ?? DEFAULT_RETRY_DELAY_MS;

  const execute = useCallback(
    async (request: () => Promise<T>): Promise<T> => {
      setState((prev) => ({ ...prev, loading: true, error: null }));

      // Offline detection — fail fast with a friendly message
      if (isOffline()) {
        const offlineError = normalizeError(new Error('offline'));
        setState((prev) => ({ ...prev, loading: false, error: offlineError }));
        throw offlineError;
      }

      let lastError: NormalizedError | null = null;

      for (let attempt = 0; attempt <= retries; attempt++) {
        try {
          const data = await request();
          setState({ data, loading: false, error: null });
          return data;
        } catch (error) {
          const normalized = normalizeError(error);
          lastError = normalized;

          // Retry only on transient network/timeout errors
          const isTransient = normalized.isNetworkError || normalized.isTimeout;
          const isLastAttempt = attempt >= retries;

          if (!isTransient || isLastAttempt) {
            setState((prev) => ({ ...prev, loading: false, error: normalized }));
            throw normalized;
          }

          // Backoff before retry
          await new Promise((resolve) => setTimeout(resolve, retryDelayMs * (attempt + 1)));
        }
      }

      // Unreachable — defensive fallback
      const fallbackError = normalizeError(lastError);
      setState((prev) => ({ ...prev, loading: false, error: fallbackError }));
      throw fallbackError;
    },
    [retries, retryDelayMs]
  );

  const reset = useCallback(() => {
    setState({ data: null, loading: false, error: null });
  }, []);

  return { ...state, execute, reset };
}

export function useApiGet<T>(_url: string, _options?: RequestInit) {
  return useApi<T>();
}

export function useApiPost<T, D = any>(url: string) {
  const api = useApi<T>();
  const execute = useCallback(
    async (data: D) => {
      return api.execute(() => apiClient.post(url, data));
    },
    [api, url]
  );
  return { ...api, execute };
}

export function useApiPut<T, D = any>(url: string) {
  const api = useApi<T>();
  const execute = useCallback(
    async (data: D) => {
      return api.execute(() => apiClient.put(url, data));
    },
    [api, url]
  );
  return { ...api, execute };
}

export function useApiDelete<T>(url: string) {
  const api = useApi<T>();
  const execute = useCallback(
    async () => {
      return api.execute(() => apiClient.delete(url));
    },
    [api, url]
  );
  return { ...api, execute };
}
