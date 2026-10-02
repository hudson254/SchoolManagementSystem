import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useNotificationRealtime } from './useNotificationRealtime';
import * as hub from '../services/notificationHub';

const enqueueSnackbar = vi.fn();

vi.mock('notistack', () => ({
  enqueueSnackbar: (...args: unknown[]) => enqueueSnackbar(...args),
}));

const startMock = vi.fn();
const stopMock = vi.fn();

let capturedOptions: hub.NotificationHubOptions;

vi.mock('../services/notificationHub', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../services/notificationHub')>();
  return {
    ...actual,
    createNotificationHubClient: (options: hub.NotificationHubOptions) => {
      capturedOptions = options;
      return {
        start: startMock,
        stop: stopMock,
        isActive: () => true,
        getState: () => 'connected' as const,
        handlerCount: () => 1,
      };
    },
  };
});

function Harness({ enabled }: { enabled: boolean }) {
  useNotificationRealtime(enabled);
  return <div data-testid="harness" />;
}

function renderHook(enabled: boolean) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

  const utils = render(
    <QueryClientProvider client={queryClient}>
      <Harness enabled={enabled} />
    </QueryClientProvider>
  );
  return { ...utils, invalidateSpy, queryClient };
}

const important: hub.RealtimeNotification = {
  id: 'n-1',
  title: 'Accommodation Allocated',
  message: 'You have been allocated house A.',
  type: 'Accommodation',
  priority: 'Important',
  isRead: false,
  createdAt: new Date().toISOString(),
};

/**
 * Extracts the query keys passed to `invalidateQueries`.
 * Typed loosely because the spy is created against a concrete QueryClient but
 * read generically here; the assertion only cares about the key values.
 */
const invalidatedKeys = (spy: {
  mock: { calls: unknown[][] };
}): unknown[] =>
  spy.mock.calls.map((call) => (call[0] as { queryKey: unknown }).queryKey);

describe('useNotificationRealtime', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    startMock.mockResolvedValue(undefined);
    stopMock.mockResolvedValue(undefined);
    capturedOptions = undefined as unknown as hub.NotificationHubOptions;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('starts the connection when authenticated', async () => {
    renderHook(true);
    await waitFor(() => expect(startMock).toHaveBeenCalledTimes(1));
  });

  it('does not connect while unauthenticated', async () => {
    renderHook(false);
    await waitFor(() => expect(startMock).not.toHaveBeenCalled());
  });

  it('stops the connection on unmount (logout)', async () => {
    const { unmount } = renderHook(true);
    await waitFor(() => expect(startMock).toHaveBeenCalled());

    unmount();
    await waitFor(() => expect(stopMock).toHaveBeenCalled());
  });

  it('invalidates the notification and unread-count queries on ReceiveNotification', async () => {
    const { invalidateSpy } = renderHook(true);
    await waitFor(() => expect(capturedOptions).toBeTruthy());

    await act(async () => {
      capturedOptions.onNotification?.(important);
    });

    const keys = invalidatedKeys(invalidateSpy);
    expect(keys).toContainEqual(['header-notifications']);
    expect(keys).toContainEqual(['header-unread-count']);
  });

  it('refreshes the same queries after a reconnect, so missed pushes are re-read', async () => {
    const { invalidateSpy } = renderHook(true);
    await waitFor(() => expect(capturedOptions).toBeTruthy());

    await act(async () => {
      capturedOptions.onReconnected?.();
    });

    const keys = invalidatedKeys(invalidateSpy);
    expect(keys).toContainEqual(['header-notifications']);
    expect(keys).toContainEqual(['header-unread-count']);
  });

  it('does not create a client-side notification record from the push payload', async () => {
    // The server already persisted the row; the client only re-reads. If it
    // inserted locally it could show an item the database does not have.
    const setQueryDataSpy = vi.spyOn(QueryClient.prototype, 'setQueryData');
    const { invalidateSpy } = renderHook(true);
    await waitFor(() => expect(capturedOptions).toBeTruthy());

    await act(async () => {
      capturedOptions.onNotification?.(important);
    });

    expect(setQueryDataSpy).not.toHaveBeenCalled();
    expect(invalidateSpy).toHaveBeenCalled();
  });

  it('toasts an Important notification but not a Normal one', async () => {
    renderHook(true);
    await waitFor(() => expect(capturedOptions).toBeTruthy());

    await act(async () => {
      capturedOptions.onNotification?.({ ...important, priority: 'Important' });
    });
    expect(enqueueSnackbar).toHaveBeenCalledTimes(1);

    await act(async () => {
      capturedOptions.onNotification?.({ ...important, id: 'n-2', priority: 'Normal' });
    });
    // Still 1: a routine background event must not interrupt the user.
    expect(enqueueSnackbar).toHaveBeenCalledTimes(1);
  });

  it('toasts a Critical notification', async () => {
    renderHook(true);
    await waitFor(() => expect(capturedOptions).toBeTruthy());

    await act(async () => {
      capturedOptions.onNotification?.({ ...important, priority: 'Critical' });
    });

    expect(enqueueSnackbar).toHaveBeenCalledTimes(1);
  });

  it('surfaces a hub failure without throwing, so the app keeps working', async () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    renderHook(true);
    await waitFor(() => expect(capturedOptions).toBeTruthy());

    expect(() => capturedOptions.onError?.(new Error('transport closed'))).not.toThrow();
    expect(warnSpy).toHaveBeenCalled();
  });

  it('does not connect twice when enabled flips false then true again', async () => {
    const queryClient = new QueryClient();
    const { rerender, unmount } = render(
      <QueryClientProvider client={queryClient}>
        <Harness enabled={true} />
      </QueryClientProvider>
    );
    await waitFor(() => expect(startMock).toHaveBeenCalledTimes(1));

    // Logout: the hook is disabled, so the effect cleanup runs and stops the
    // socket rather than leaving an unauthorized connection alive.
    rerender(
      <QueryClientProvider client={queryClient}>
        <Harness enabled={false} />
      </QueryClientProvider>
    );
    await waitFor(() => expect(stopMock).toHaveBeenCalledTimes(1));

    unmount();
    expect(startMock).toHaveBeenCalledTimes(1);
  });
});