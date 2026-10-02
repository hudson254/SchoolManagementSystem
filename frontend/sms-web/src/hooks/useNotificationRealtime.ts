import { useCallback, useEffect, useMemo, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { enqueueSnackbar } from 'notistack';
import {
  NOTIFICATION_QUERY_KEYS,
  REALTIME_SEVERITIES,
  UNREAD_COUNT_QUERY_KEYS,
  createNotificationHubClient,
  normalizePriority,
} from '../services/notificationHub';

export interface UseNotificationRealtimeResult {
  /** Live connection state, for diagnostics and tests. */
  state: string;
}

/**
 * Opens ONE authenticated SignalR connection for the signed-in session.
 *
 * MOUNTED ONCE, IN THE AUTHENTICATED LAYOUT. The hub derives the caller's
 * identity from the validated cookie, so this hook is mounted only when a user is
 * authenticated and is torn down on logout - which also stops the socket.
 *
 * WHAT HAPPENS WHEN A NOTIFICATION ARRIVES
 * The server is the source of truth and has ALREADY persisted the row before it
 * pushes. The client therefore does not insert anything into a local list: it
 * invalidates the existing queries, which re-read the authoritative state. That
 * is what prevents the client from ever showing a notification the database does
 * not have, or showing it twice.
 *
 * `refetchOnWindowFocus` in the header is deliberately left in place: it is the
 * documented fallback whenever this socket is unavailable.
 */
export function useNotificationRealtime(enabled: boolean): UseNotificationRealtimeResult {
  const queryClient = useQueryClient();

  // Latest-value refs keep the effect from re-running (and therefore from
  // re-opening the socket) on every render.
  const clientRef = useRef<ReturnType<typeof createNotificationHubClient> | null>(null);
  const stateRef = useRef<string>('disconnected');

  const handleIncoming = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: NOTIFICATION_QUERY_KEYS });
    void queryClient.invalidateQueries({ queryKey: UNREAD_COUNT_QUERY_KEYS });
    // The full notification centre page uses a third key; refresh it too when
    // mounted so a user with the page open sees the new item.
    void queryClient.invalidateQueries({ queryKey: ['notifications'] });
  }, [queryClient]);

  const handleImportant = useCallback(
    (payload: { title: string; message?: string; priority?: string }) => {
      // Only IMPORTANT/CRITICAL get an intrusive hint. A Normal notice would
      // otherwise turn every background event into an interruption.
      if (!REALTIME_SEVERITIES.has(normalizePriority(payload?.priority))) return;

      enqueueSnackbar(`${payload.title}${payload.message ? ` — ${payload.message}` : ''}`, {
        variant: 'warning',
        autoHideDuration: 8000,
        preventDuplicate: true,
      });
    },
    []
  );

  const handleError = useCallback((error: Error) => {
    // Surfaced, never thrown: a dead socket must not break the application.
    // eslint-disable-next-line no-console
    console.warn('Notification live channel unavailable; using API fallback.', error);
  }, []);

  useEffect(() => {
    if (!enabled) return;

    const client = createNotificationHubClient({
      onNotification: (payload) => {
        handleIncoming();
        handleImportant(payload);
      },
      onReconnected: () => {
        // A notification raised while we were offline is in the database but was
        // never pushed, so re-read on reconnect.
        handleIncoming();
        stateRef.current = 'connected';
      },
      onError: handleError,
    });

    clientRef.current = client;
    void client.start().then(() => {
      stateRef.current = client.getState();
    });

    return () => {
      // Logout / unmount: detach the handler and close the socket.
      void client.stop();
      clientRef.current = null;
      stateRef.current = 'disconnected';
    };
  }, [enabled, handleIncoming, handleImportant, handleError]);

  // Returned as a live read so a caller observing state is not stale; the value
  // itself is only used by tests and diagnostics.
  return useMemo(() => ({ state: stateRef.current }), []);
}