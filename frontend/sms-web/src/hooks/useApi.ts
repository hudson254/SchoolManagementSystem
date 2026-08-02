import { useState, useCallback } from 'react';
import { apiClient } from '../services/api';

interface UseApiState<T> {
  data: T | null;
  loading: boolean;
  error: Error | null;
}

export function useApi<T>() {
  const [state, setState] = useState<UseApiState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  const execute = useCallback(
    async (request: () => Promise<T>): Promise<T> => {
      setState((prev) => ({ ...prev, loading: true, error: null }));
      try {
        const data = await request();
        setState({ data, loading: false, error: null });
        return data;
      } catch (error) {
        const errorObj = error as Error;
        setState((prev) => ({ ...prev, loading: false, error: errorObj }));
        throw errorObj;
      }
    },
    []
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
