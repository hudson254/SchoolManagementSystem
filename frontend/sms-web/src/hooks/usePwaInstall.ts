/**
 * Progressive Web App install state.
 *
 * WHY THIS EXISTS
 * The application had a manifest and a service worker but nothing ever read
 * `beforeinstallprompt`, so no user was ever offered installation.
 *
 * SCOPE AND HONESTY (this deployment is LAN-only and air-gapped)
 * - Chromium browsers (Chrome/Edge/Android) expose `beforeinstallprompt` and this
 *   hook will call `prompt()` on it. That is a genuine one-click install.
 * - iOS/iPadOS Safari does NOT implement `beforeinstallprompt` at all. There is no
 *   API to trigger installation, so pretending otherwise would be a lie. The hook
 *   reports `platform: 'ios'` and the UI shows manual Share -> "Add to Home Screen"
 *   instructions instead.
 * - There is no Web Push here. Notification badges and the notification centre come
 *   from the API while the app is open; nothing can wake a closed app. See
 *   Documentation/Notifications-PWA.
 */

import { useCallback, useEffect, useState } from 'react';

/** Minimal shape of the non-standard BeforeInstallPromptEvent. */
interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>;
}

/** Persisted dismissal, so the prompt is not shown on every single visit. */
const DISMISSED_KEY = 'sms.pwa.installDismissed';

export type InstallPlatform = 'chromium' | 'ios' | 'other';
export type InstallStatus =
  | 'unavailable'    // browser cannot install at all / not a PWA-capable context
  | 'not-installable' // installable in principle, but no programmatic prompt exists
  | 'available'       // a beforeinstallprompt event is captured and can be triggered
  | 'installed';      // already running as an installed app

export interface UsePwaInstallResult {
  /** True when running inside the installed standalone app. */
  isInstalled: boolean;
  /** True when a captured beforeinstallprompt can be triggered right now. */
  canInstall: boolean;
  /** True when the user dismissed the prompt (this or a previous visit). */
  isDismissed: boolean;
  /** Which browser family we are on, so the UI can give correct guidance. */
  platform: InstallPlatform;
  /** Coarse install state, for choosing what to render. */
  status: InstallStatus;
  /** Triggers the native install dialog. Resolves to true when accepted. */
  install: () => Promise<boolean>;
  /** Hides the prompt and remembers the choice. */
  dismiss: () => void;
  /** Clears a stored dismissal (used by the "how do I install this?" help entry). */
  resetDismissal: () => void;
}

/** Reads the stored dismissal flag, tolerating disabled/absent localStorage. */
function readDismissed(): boolean {
  try {
    return window.localStorage.getItem(DISMISSED_KEY) === 'true';
  } catch {
    // Private browsing or a blocked storage partition: treat as not dismissed so the
    // user still gets offered the app rather than silently never seeing it.
    return false;
  }
}

/**
 * Detects iOS/iPadOS. Safari on iPadOS 13+ reports as MacOS with a touch-capable
 * device, so a user-agent-only check would misclassify iPad as a desktop Mac and
 * show the wrong (Chromium) instructions.
 */
function detectPlatform(): InstallPlatform {
  if (typeof navigator === 'undefined') return 'other';

  const ua = navigator.userAgent || '';
  const isIOS = /iPad|iPhone|iPod/.test(ua);
  const isIPadOSDesktopMode = /Macintosh/.test(ua)
    && typeof navigator.maxTouchPoints === 'number'
    && navigator.maxTouchPoints > 1;

  if (isIOS || isIPadOSDesktopMode) return 'ios';
  if (/Chrome|Chromium|Edg|OPR|SamsungBrowser/.test(ua)) return 'chromium';
  return 'other';
}

/**
 * True when the app is already installed. Two independent signals, because neither
 * is universally supported: the `display-mode: standalone` media query (Chromium and
 * iOS Safari 16.4+) and the legacy `navigator.standalone` flag (older iOS Safari).
 */
function detectStandalone(): boolean {
  if (typeof window === 'undefined') return false;

  const hasStandaloneDisplay = typeof window.matchMedia === 'function'
    && (window.matchMedia('(display-mode: standalone)').matches
      || window.matchMedia('(display-mode: fullscreen)').matches
      || window.matchMedia('(display-mode: minimal-ui)').matches);

  const iosStandalone = (navigator as Navigator & { standalone?: boolean }).standalone === true;

  return hasStandaloneDisplay || iosStandalone;
}

export function usePwaInstall(): UsePwaInstallResult {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const [isInstalled, setIsInstalled] = useState<boolean>(detectStandalone);
  const [isDismissed, setIsDismissed] = useState<boolean>(readDismissed);
  const platform = detectPlatform();

  // Capture the install prompt. Chromium fires this once the PWA is deemed
  // installable; holding the event is the only way to re-trigger it later.
  useEffect(() => {
    const onBeforeInstallPrompt = (event: Event) => {
      // Suppress the browser's mini-infobar so the app decides when/whether to ask.
      event.preventDefault();
      setDeferredPrompt(event as BeforeInstallPromptEvent);
    };

    // Fires when the user installs from the browser UI instead of our dialog.
    const onInstalled = () => {
      setDeferredPrompt(null);
      setIsInstalled(true);
    };

    window.addEventListener('beforeinstallprompt', onBeforeInstallPrompt);
    window.addEventListener('appinstalled', onInstalled);

    return () => {
      window.removeEventListener('beforeinstallprompt', onBeforeInstallPrompt);
      window.removeEventListener('appinstalled', onInstalled);
    };
  }, []);

  // Track standalone state, so switching to an installed window (or launching it)
  // updates the flag without needing a reload.
  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return;

    const query = window.matchMedia('(display-mode: standalone)');
    const onChange = (event: MediaQueryListEvent) => setIsInstalled(event.matches);

    // Safari < 16.4 has no addEventListener on MediaQueryList.
    if (typeof query.addEventListener === 'function') {
      query.addEventListener('change', onChange);
      return () => query.removeEventListener('change', onChange);
    }

    query.addListener(onChange);
    return () => query.removeListener(onChange);
  }, []);

  const install = useCallback(async (): Promise<boolean> => {
    if (!deferredPrompt) return false;

    try {
      await deferredPrompt.prompt();
      const choice = await deferredPrompt.userChoice;

      // The event is single-use by specification: it may not be prompted twice.
      setDeferredPrompt(null);

      if (choice.outcome === 'accepted') {
        setIsInstalled(true);
        setIsDismissed(false);
        try {
          window.localStorage.removeItem(DISMISSED_KEY);
        } catch {
          // Storage unavailable; in-memory state is already updated.
        }
        return true;
      }

      setIsDismissed(true);
      return false;
    } catch {
      // A rejected prompt() must never break the UI.
      setDeferredPrompt(null);
      return false;
    }
  }, [deferredPrompt]);

  const dismiss = useCallback(() => {
    setIsDismissed(true);
    setDeferredPrompt(null);
    try {
      window.localStorage.setItem(DISMISSED_KEY, 'true');
    } catch {
      // Storage unavailable; the in-memory flag still hides it for this session.
    }
  }, []);

  const resetDismissal = useCallback(() => {
    setIsDismissed(false);
    try {
      window.localStorage.removeItem(DISMISSED_KEY);
    } catch {
      // No-op.
    }
  }, []);

  let status: InstallStatus;
  if (isInstalled) {
    status = 'installed';
  } else if (deferredPrompt) {
    status = 'available';
  } else if (platform === 'ios') {
    // iOS is always installable manually via Share -> Add to Home Screen, but there
    // is no programmatic prompt, so the UI must show instructions instead.
    status = 'not-installable';
  } else {
    status = 'unavailable';
  }

  return {
    isInstalled,
    canInstall: deferredPrompt !== null,
    isDismissed,
    platform,
    status,
    install,
    dismiss,
    resetDismissal,
  };
}