import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { InstallPrompt, PwaUpdateBanner } from './InstallPrompt';

const DISMISSED_KEY = 'sms.pwa.installDismissed';

/**
 * Drives `beforeinstallprompt`, the only way a Chromium browser signals that the
 * app is installable. The event is not constructible in jsdom, so a plain Event is
 * given the two extra members the hook uses.
 */
class FakeBeforeInstallPrompt extends Event {
  prompt = vi.fn().mockResolvedValue(undefined);
  userChoice = Promise.resolve({ outcome: 'accepted' as const, platform: 'web' });

  constructor() {
    super('beforeinstallprompt');
  }
}

/** Makes matchMedia report standalone or not, per the queried condition. */
const setStandalone = (matches: boolean) => {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    matches: query.includes('standalone') ? matches : false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })) as unknown as typeof window.matchMedia;
};

const setUserAgent = (ua: string) => {
  Object.defineProperty(window.navigator, 'userAgent', { value: ua, configurable: true });
};

const fireInstallPrompt = async () => {
  await act(async () => {
    window.dispatchEvent(new FakeBeforeInstallPrompt());
  });
};

describe('InstallPrompt', () => {
  beforeEach(() => {
    window.localStorage.clear();
    setStandalone(false);
    setUserAgent('Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/120 Safari/537.36');
    (window.navigator as { maxTouchPoints?: number }).maxTouchPoints = 0;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders nothing when the app is already installed', () => {
    // An installed user must not keep being nagged to install it again.
    setStandalone(true);
    render(<InstallPrompt />);
    expect(screen.queryByText('Install School Management System')).toBeNull();
  });

  it('explains the benefit once an install prompt is captured', async () => {
    render(<InstallPrompt />);
    await fireInstallPrompt();

    expect(screen.getByText('Install School Management System')).toBeInTheDocument();
    expect(
      screen.getByText(/faster access and important system notifications/i)
    ).toBeInTheDocument();
  });

  it('offers an enabled Install button on Chromium', async () => {
    render(<InstallPrompt />);
    await fireInstallPrompt();

    // Anchored exactly: /install/i would also match the "Installation instructions"
    // icon button and resolve to multiple elements.
    expect(screen.getByRole('button', { name: 'Install' })).toBeEnabled();
  });

  it('invokes the native prompt when Install is pressed', async () => {
    const event = new FakeBeforeInstallPrompt();
    render(<InstallPrompt />);
    await fireInstallPrompt();
    // Dispatch the captured instance so the assertion can inspect it.
    await act(async () => {
      window.dispatchEvent(event);
    });

    await userEvent.click(screen.getByRole('button', { name: 'Install' }));

    expect(event.prompt).toHaveBeenCalledTimes(1);
  });

  it('hides after dismissal and remembers the choice across mounts', async () => {
    render(<InstallPrompt />);
    await fireInstallPrompt();

    await userEvent.click(screen.getByRole('button', { name: /dismiss install prompt/i }));

    expect(screen.queryByText('Install School Management System')).toBeNull();
    expect(window.localStorage.getItem(DISMISSED_KEY)).toBe('true');

    // Remounting must not resurrect the prompt: respecting the dismissal is what
    // stops the banner nagging a user who already said no.
    render(<InstallPrompt />);
    expect(screen.queryByText('Install School Management System')).toBeNull();
  });

  it('does not render before Chromium offers an install prompt', () => {
    // A prompt the user cannot act on is worse than no prompt.
    render(<InstallPrompt />);
    expect(screen.queryByText('Install School Management System')).toBeNull();
  });

  it('shows iOS manual instructions because iOS has no install API', async () => {
    setUserAgent('Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1');
    render(<InstallPrompt />);

    expect(screen.getByText('Install School Management System')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Install' }));

    expect(screen.getByText(/Add to Home Screen/i)).toBeInTheDocument();
    expect(screen.getByText(/does not offer a programmatic install prompt/i)).toBeInTheDocument();
  });

  it('detects iPadOS reporting a desktop Mac user agent', () => {
    // iPadOS 13+ Safari reports "Macintosh" with a touch screen; a UA-only check
    // would misclassify an iPad as a desktop Mac and show the wrong instructions.
    setUserAgent('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 Safari/604.1');
    (window.navigator as { maxTouchPoints?: number }).maxTouchPoints = 5;

    render(<InstallPrompt />);
    expect(screen.getByText('Install School Management System')).toBeInTheDocument();
    expect(screen.getByText(/Install on iPhone \/ iPad/i)).toBeInTheDocument();
  });

  it('hides once the browser reports the app was installed', async () => {
    render(<InstallPrompt />);
    await fireInstallPrompt();
    expect(screen.getByText('Install School Management System')).toBeInTheDocument();

    await act(async () => {
      window.dispatchEvent(new Event('appinstalled'));
    });

    expect(screen.queryByText('Install School Management System')).toBeNull();
  });
});

describe('PwaUpdateBanner', () => {
  it('renders nothing when no update is waiting', () => {
    const { container } = render(
      <PwaUpdateBanner updateAvailable={false} onApplyUpdate={async () => {}} />
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('offers a reload when an update is available', async () => {
    // Without this an installed user would keep running the previous build forever:
    // the new worker downloads but nobody tells them and the old one will not step
    // aside.
    const onApplyUpdate = vi.fn().mockResolvedValue(undefined);
    render(<PwaUpdateBanner updateAvailable onApplyUpdate={onApplyUpdate} />);

    expect(
      screen.getByText(/new version of the School Management System is available/i)
    ).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /reload/i }));
    expect(onApplyUpdate).toHaveBeenCalledTimes(1);
  });
});