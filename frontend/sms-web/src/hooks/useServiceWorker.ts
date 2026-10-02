/**
 * Service worker registration and update lifecycle.
 *
 * WHY THIS EXISTS
 * Registration happened but its result was discarded, so:
 *  - a failing registration was only ever visible as a console error;
 *  - an UPDATE that had downloaded and was WAITING was never surfaced, so users
 *    stayed on the old frontend indefinitely;
 *  - skipWaiting() was called inside the worker's install handler, which swaps the
 *    worker out from under a running page without warning.
 *
 * This hook makes the update explicit: a new worker parks in `waiting`, the UI
 * offers a "Reload" action, and only then is `skipWaiting` sent. That is what
 * stops users getting stuck on an old frontend after a deployment.
 *
 * The worker is registered in every environment (not only production). In
 * development Vite serves modules that a stale cached worker would break, and
 * the previous production-only guard meant the PWA behaviour could never be
 * exercised locally. Failures are reported as state, never thrown.
 */

import { useCallback, useEffect, useState } from 'react';

export interface ServiceWorkerState {
  /** True once a worker is registered and controlling this page. */
  isRegistered: boolean;
  /** True when support exists (ServiceWorkerContainer and a secure context). */
  isSupported: boolean;
  /** A newer worker has downloaded and is waiting to take over. */
  updateAvailable: boolean;
  /** Last registration error message, if any. */
  error: string | null;
  /** Activates the waiting worker and reloads so the new assets are used. */
  applyUpdate: () => Promise<void>;
}

/** True when this environment can host a service worker at all. */
function detectSupport(): boolean {
  return typeof navigator !== 'undefined' && 'serviceWorker' in navigator;
}

export function useServiceWorker(): ServiceWorkerState {
  const [isRegistered, setIsRegistered] = useState(false);
  const [isSupported, setIsSupported] = useState(detectSupport);
  const [updateAvailable, setUpdateAvailable] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const applyUpdate = useCallback(async () => {
    if (!detectSupport()) return;

    try {
      const registration = await navigator.serviceWorker.getRegistration();
      const waiting = registration?.waiting;
      if (!waiting) return;

      // Ask the waiting worker to activate, then reload so the new hashed asset
      // URLs referenced by the new index.html are actually fetched.
      await new Promise<void>((resolve) => {
        const onControllerChange = () => resolve();
        navigator.serviceWorker.addEventListener('controllerchange', onControllerChange, { once: true });
        waiting.postMessage({ type: 'SKIP_WAITING' });
        // Fallback in case controllerchange never fires.
        setTimeout(resolve, 2000);
      });

      window.location.reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to apply update');
    }
  }, []);

  useEffect(() => {
    if (!detectSupport()) {
      setIsSupported(false);
      return;
    }
    setIsSupported(true);

    let cancelled = false;

    const register = async () => {
      try {
        const registration = await navigator.serviceWorker.register('/sw.js', { scope: '/' });

        if (cancelled) return;

        setIsRegistered(true);
        setError(null);

        // A worker that is already waiting when we register means a newer version
        // is sitting on disk from a previous visit.
        if (registration.waiting && navigator.serviceWorker.controller) {
          setUpdateAvailable(true);
        }

        registration.addEventListener('updatefound', () => {
          const installing = registration.installing;
          if (!installing) return;

          installing.addEventListener('statechange', () => {
            // "installed" with an existing controller means this is an UPDATE
            // (a first install has no controller yet and needs no prompt).
            if (installing.state === 'installed' && navigator.serviceWorker.controller) {
              setUpdateAvailable(true);
            }
          });
        });

        // Another tab applied the update: offer the reload here too.
        let refreshing = false;
        const onControllerChange = () => {
          if (refreshing) return;
          refreshing = true;
          window.location.reload();
        };
        navigator.serviceWorker.addEventListener('controllerchange', onControllerChange);

        // Periodic check so a long-lived tab eventually notices a deployment.
        const interval = window.setInterval(() => {
          registration.update().catch(() => undefined);
        }, 60 * 60 * 1000); // hourly

        return () => window.clearInterval(interval);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : 'Service worker registration failed');
      }
    };

    let cleanup: (() => void) | undefined;
    register().then((fn) => {
      cleanup = fn;
    });

    return () => {
      cancelled = true;
      cleanup?.();
    };
  }, []);

  return { isRegistered, isSupported, updateAvailable, error, applyUpdate };
}